// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VehicleController.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClientGateway.Controllers
{
    using System;
    using System.Data.SqlClient;
    using System.Fabric;
    using System.Threading.Tasks;
    using System.Web.Http;

    using global::ClientGateway.Model;

    using Microsoft.ServiceFabric.Data.Collections;

    [RoutePrefix("api/vehicles")]
    public class VehicleController : ApiController
    {
        [HttpGet]
        [Route("{vin}/lock")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> DoorLock(string vin)
        {
            var commandResponseModel = new CommandResponseModel();
            try
            {
                commandResponseModel.CommandId = Guid.NewGuid().ToString();

                // Get the Queue
                var queue = await ClientGateway.ReliableStateManager.GetOrAddAsync<IReliableQueue<Command>>("command");
                using (var tx = ClientGateway.ReliableStateManager.CreateTransaction())
                {
                    var command = new Command()
                    {
                        CommandType = CommandType.DoorLock,
                        Vin = vin,
                        CommandId = commandResponseModel.CommandId
                    };
                    await queue.EnqueueAsync(tx, command);
                    await this.InsertCommand(command);
                    await tx.CommitAsync();
                }

                commandResponseModel.Status = "Queued";
            }
            catch (Exception exception)
            {
                commandResponseModel.Status = "Error";
            }

            return Json(commandResponseModel);
        }

        /// <summary>
        /// Inserts the command.
        /// </summary>
        /// <param name="command">The command.</param>
        private async Task InsertCommand(Command command)
        {
            var connectionString = ServiceConfiguration.GetConfiguration("DBConnectionString");
            using (
                var conn =
                    new SqlConnection(connectionString)
                )
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO [dbo].[Command]
                       ([CommandId]
                       ,[CommandTypeId]
                       ,[CommandStatus])
                    VALUES         
                        ('" + command.CommandId + "'," + (int)command.CommandType + ",'Queued')";

                conn.Open();
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}