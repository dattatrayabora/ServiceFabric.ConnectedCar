// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventProcessor.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Telemetry.Interfaces;

namespace IoTProcessorManagement.Common
{
    public class EventProcessor : IEventProcessor
    {
        /// <summary>
        /// The data handler
        /// </summary>
        private readonly IDataHandler dataHandler;

        /// <summary>
        /// The checkpoint stop watch
        /// </summary>
        private Stopwatch checkpointStopWatch;

        public EventProcessor(IDataHandler dataHandler)
        {
            this.dataHandler = dataHandler;
        }

        public Task OpenAsync(PartitionContext context)
        {
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult(true);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                foreach (var eventData in messages)
                {
                    try
                    {
                        await this.dataHandler.ProcessMessage(eventData).ConfigureAwait(false);
                    }
                    catch(Exception exception)
                    {
                        // TODO: Exception
                    }
                }

                //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
                if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
                {
                    await context.CheckpointAsync();
                    lock (this)
                    {
                        this.checkpointStopWatch.Reset();
                    }
                }
            }
            catch (Exception)
            {
                //// TODO: Log Exception   
            }
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }
    }
}