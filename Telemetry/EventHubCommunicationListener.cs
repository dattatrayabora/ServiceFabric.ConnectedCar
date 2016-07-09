// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventHubCommunicationListener.cs" company="Microsoft"> 
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
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using IoTProcessorManagement.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Telemetry.Interfaces;

namespace Telemetry
{
    /// <summary>
    /// Event Hub Communiation Listener
    /// </summary>
    public class EventHubCommunicationListener : ICommunicationListener
    {
        /// <summary>
        /// The state manager
        /// </summary>
        private IReliableStateManager stateManager;

        /// <summary>
        /// The state dictionary
        /// </summary>
        private IReliableDictionary<string, string> stateDictionary;

        /// <summary>
        /// The event hub name
        /// </summary>
        private string eventHubName;

        /// <summary>
        /// The event hub connection string
        /// </summary>
        private string eventHubConnectionString;

        /// <summary>
        /// The consumer group
        /// </summary>
        private string consumerGroup;

        /// <summary>
        /// The event hub consumer group
        /// </summary>
        private EventHubConsumerGroup eventHubConsumerGroup;

        /// <summary>
        /// The event data handler
        /// </summary>
        private IDataHandler eventDataHandler;

        /// <summary>
        /// The event hub client
        /// </summary>
        private EventHubClient eventHubClient;

        /// <summary>
        /// The event processor factory
        /// </summary>
        private EventProcessorFactory eventProcessorFactory;

        /// <summary>
        /// The m_ namespace
        /// </summary>
        private string m_Namespace;

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        /// <value>
        /// The namespace.
        /// </value>
        private string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_Namespace))
                {
                    string[] elements = this.eventHubConnectionString.Split(';');

                    foreach (string elem in elements)
                    {
                        if (elem.StartsWith("HostName="))
                        {
                            var uri = "http://" + elem.Split('=')[1];
                            this.m_Namespace = new Uri(uri).Host;
                        }
                    }
                }
                return this.m_Namespace;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubCommunicationListener" /> class.
        /// </summary>
        /// <param name="initParams">The initialize parameters.</param>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="stateDictionary">The state dictionary.</param>
        /// <param name="eventDataHandler">The event data handler.</param>
        /// <exception cref="System.ArgumentNullException">Connection String
        /// or
        /// Event Hub name</exception>
        public EventHubCommunicationListener(
            ServiceInitializationParameters initParams,
            IReliableStateManager stateManager,
            IReliableDictionary<string, string> stateDictionary,
            IDataHandler eventDataHandler)
        {
            var configurationPackage = initParams.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var configSettings = configurationPackage.Settings.Sections["ConfigurationSettings"];
            this.stateManager = stateManager;
            this.stateDictionary = stateDictionary;
            this.eventHubName = configSettings.Parameters[Constants.EventHubNameStringKey].Value;
            this.eventHubConnectionString = configSettings.Parameters[Constants.EventHubConnectionStringKey].Value;
            this.consumerGroup = configSettings.Parameters[Constants.ConsumerGroupNameKey].Value;
            this.eventDataHandler = eventDataHandler;

            if (string.IsNullOrWhiteSpace(this.eventHubConnectionString))
            {
                ServiceEventSource.Current.Message("Invalid Connection String");
                throw new ArgumentNullException("Connection String");
            }

            if (string.IsNullOrWhiteSpace(this.eventHubName))
            {
                ServiceEventSource.Current.Message("Invalid Event Hub Name");
                throw new ArgumentNullException("Event Hub name");
            }

            ServiceEventSource.Current.Message("Initializing Event Hub Listener");
        }

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Task</returns>
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.eventHubClient = EventHubClient.CreateFromConnectionString(this.eventHubConnectionString, this.eventHubName);
                this.eventHubConsumerGroup = string.IsNullOrWhiteSpace(this.consumerGroup)
                    ? this.eventHubClient.GetDefaultConsumerGroup()
                    : this.eventHubClient.GetConsumerGroup(this.consumerGroup);
                var runtimeInfo = await this.eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
                this.eventProcessorFactory = new EventProcessorFactory(this.eventDataHandler);
                CheckPointManager checkPointManager = new CheckPointManager();
                ServiceEventSource.Current.Message("Initializing event hub listener");

                foreach (string pid in runtimeInfo.PartitionIds)
                {
                    StateManagerLease lease =
                        await
                            StateManagerLease.GetOrCreateAsync(
                                this.stateManager,
                                this.stateDictionary,
                                this.Namespace,
                                this.consumerGroup,
                                this.eventHubName,
                                pid);


                    await this.eventHubConsumerGroup.RegisterProcessorFactoryAsync(
                        lease,
                        checkPointManager,
                        this.eventProcessorFactory
                        );
                }
            }
            catch (Exception exception)
            {
                ServiceEventSource.Current.Message(exception.ToString());
            }
            
            return string.Concat(this.eventHubName, " @ ", this.Namespace);
        }

        /// <summary>
        /// Closes the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Task</returns>
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            await eventHubConsumerGroup.CloseAsync();
            await this.eventHubClient.CloseAsync();
        }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public void Abort()
        {
            // Ignore
        }
    }
}