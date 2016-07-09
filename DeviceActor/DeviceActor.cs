// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeviceActor.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DeviceActor
{
    using System;
    using System.Data.SqlClient;
    using System.Fabric;

    using global::DeviceActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using System.Threading.Tasks;
    using ConnectedCar.Core;

    /// <summary>
    /// Represents the Actor For Device
    /// </summary>
    /// <remarks>
    /// Each ActorID maps to an instance of this class.
    /// The IProjName  interface (in a separate DLL that client code can
    /// reference) defines the operations exposed by ProjName objects.
    /// </remarks>
    internal class DeviceActor : StatefulActor<DeviceActorState>, IDeviceActor
    {
        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        /// <value>
        /// The database connection string.
        /// </value>
        private string DbConnectionString { get; set; }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        /// <returns>The Task</returns>
        protected override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                // This is the first time this actor has ever been activated.
                // Set the actor's initial state values.
                this.Initialize();
                this.State = new DeviceActorState
                {
                    DeviceState = "Connected"
                };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }

        // <summary>
        /// This method is called whenever an actor state is loaded.
        /// </summary>
        /// <returns>The Task</returns>
        protected override Task OnLoadStateAsync()
        {
            this.Initialize();
            return base.OnLoadStateAsync();
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The Task</returns>
        public async Task ProcessMessage(TelemetryMessage message)
        {
            if (message != null)
            {
                try
                {
                    if (message.Properties.ContainsKey(Constants.CorrelationId) && !string.IsNullOrWhiteSpace(message.Properties[Constants.CorrelationId].ToString()))
                    {
                        var correlationId = message.Properties[Constants.CorrelationId].ToString();
                        ActorEventSource.Current.ActorMessage(this, "Received Command Response For Command Id {0} and Message Id {1}", correlationId, message.MessageId);

                        // Process the Message
                        await this.UpdateCommand(correlationId);
                        await this.InsertTelemetry(message);
                        // Save the State
                    }
                    else
                    {
                        ActorEventSource.Current.ActorMessage(this, "Received Telemetry Message with Id {0}", message.MessageId);

                        // Save the telemetry message
                        await this.InsertTelemetry(message);
                    }
                }
                catch (Exception exception)
                {
                    ActorEventSource.Current.ActorMessage(this, exception.ToString());
                }
            }
        }

        /// <summary>
        /// Updates the command.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <returns>The Task</returns>
        private async Task UpdateCommand(string commandId)
        {
            using (var conn = new SqlConnection(this.DbConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE [dbo].[Command] SET CommandStatus = 'Recieved' where
                        [CommandId] = '" + commandId + "'";

                conn.Open();
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task InsertTelemetry(TelemetryMessage message)
        {
            using (var conn = new SqlConnection(this.DbConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO [dbo].[Telemetry](MessageId,Body) values(@Id,@Body)";
                cmd.Parameters.AddWithValue("@Id", message.MessageId);
                cmd.Parameters.AddWithValue("@Body", message.Body);
                conn.Open();
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            var configurationPackage = this.ActorService.ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            this.DbConnectionString = configurationPackage.Settings.Sections["ConfigurationSettings"].Parameters["DBConnectionString"].Value;
            if (string.IsNullOrWhiteSpace(this.DbConnectionString))
            {
                throw new Exception("DB Connection String shouldn't be empty");
            }
        }
    }
}
