using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Telemetry
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class Telemetry : StatefulService
    {
        /// <summary>
        /// The composite listener
        /// </summary>
        private CompositeCommunicationListener compositeListener;

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service replica.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            this.compositeListener = new CompositeCommunicationListener();
            var serviceReplicaListeners = new List<ServiceReplicaListener>
            {
                new ServiceReplicaListener(initParams => compositeListener, "EventHubs")
            };

            return serviceReplicaListeners;
        }

        /// <summary>
        /// This is the main entry point for your service's partition replica. 
        /// RunAsync executes when the primary replica for this partition has write status.
        /// </summary>
        /// <param name="cancelServicePartitionReplica">Canceled when Service Fabric terminates this partition's replica.</param>
        protected override async Task RunAsync(CancellationToken cancelServicePartitionReplica)
        {
            try
            {
                var dataHandler = new EventDataHandler();

                var leaseStateDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(Constants.DefDictionary);

                var eventHubCommunicationListener = new EventHubCommunicationListener(this.ServiceInitializationParameters, this.StateManager, leaseStateDictionary, dataHandler);

                await this.compositeListener.AddListenerAsync("IOTHub", eventHubCommunicationListener);

                while (!cancelServicePartitionReplica.IsCancellationRequested)
                {
                    await Task.Delay(2000);
                }
            }
            catch (Exception exception)
            {
                ServiceEventSource.Current.Message(exception.Message);
            }
        }
    }
}
