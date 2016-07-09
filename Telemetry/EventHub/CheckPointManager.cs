// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CheckPointManager.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace IoTProcessorManagement.Common
{
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// CheckPoint Manager
    /// </summary>
    public class CheckPointManager : ICheckpointManager
    {
        /// <summary>
        /// Checkpoints the asynchronous.
        /// </summary>
        /// <param name="lease">The lease.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="sequenceNumber">The sequence number.</param>
        /// <returns>The Task</returns>
        public async Task CheckpointAsync(Lease lease, string offset, long sequenceNumber)
        {
            StateManagerLease stateManagerLease = lease as StateManagerLease;
            stateManagerLease.Offset = offset;
            stateManagerLease.SequenceNumber = sequenceNumber;
            await stateManagerLease.SaveAsync();
        }
    }
}