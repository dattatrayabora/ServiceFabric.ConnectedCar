// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDeviceActor.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DeviceActor.Interfaces
{
    using System.Threading.Tasks;
    using ConnectedCar.Core;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// This interface represents the actions a client app can perform on an actor.
    /// It MUST derive from IActor and all methods MUST return a Task.
    /// </summary>
    public interface IDeviceActor : IActor
    {
        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">Telemetry message.</param>
        /// <returns>
        /// The Task
        /// </returns>
        Task ProcessMessage(TelemetryMessage message);
    }
}
