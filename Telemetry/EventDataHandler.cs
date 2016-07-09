// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventDataHandler.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Telemetry.Interfaces;
    using ConnectedCar.Core;
    using DeviceActor.Interfaces;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.ServiceFabric.Actors;

    public class EventDataHandler : IDataHandler
    {
        /// <summary>
        /// The device actor service name
        /// </summary>
        private const string deviceActorServiceName = "fabric:/ConnectedCar/DeviceActorService";

        /// <summary>
        /// The actors
        /// </summary>
        private IDictionary<string, IDeviceActor> actors = new Dictionary<string, IDeviceActor>();

        /// <summary>
        /// The actor lock
        /// </summary>
        private object actorLock = new object();

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// The Task
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task ProcessMessage(EventData data)
        {
            if (!data.Properties.ContainsKey(Constants.DeviceIdKey))
            {
                throw new Exception("Device Id is missing");
            }

            var deviceId = data.Properties[Constants.DeviceIdKey].ToString();
            var actor = this.GetActor(deviceId);
            var telemetryMessage = new TelemetryMessage()
            {
                Body = data.GetBytes(),
                Properties = data.Properties,
                DeviceId = deviceId
            };

            if (data.Properties.ContainsKey(Constants.MessageIdKey))
            {
                telemetryMessage.MessageId = data.Properties[Constants.MessageIdKey].ToString();
            }

            await actor.ProcessMessage(telemetryMessage);
        }

        private IDeviceActor GetActor(string deviceId)
        {
            if (this.actors.ContainsKey(deviceId))
            {
                return this.actors[deviceId];
            }

            lock (this.actorLock)
            {
                if (this.actors.ContainsKey(deviceId))
                {
                    return this.actors[deviceId];
                }

                ActorId id = new ActorId(deviceId);
                var actor = ActorProxy.Create<IDeviceActor>(id, new Uri(deviceActorServiceName));
                actors.Add(deviceId, actor);
                return actor;
            }
        }
    }
}