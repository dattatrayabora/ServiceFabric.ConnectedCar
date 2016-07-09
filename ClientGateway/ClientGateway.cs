namespace ClientGateway
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;
    using global::ClientGateway.Model;
    using Microsoft.Azure.Devices;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class ClientGateway : StatefulService
    {
        private IoTHubContext iotHubContext;
        public static IReliableStateManager ReliableStateManager { get; set; }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            var serviceReplicaList = new List<ServiceReplicaListener>
            {
                new ServiceReplicaListener(initParams => new OwinCommunicationListener(initParams, "", new Startup()), "Gateway")
            };
            return serviceReplicaList;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var iotHubConnectionString = ServiceConfiguration.GetConfiguration("EventHubConnectionString");
            this.iotHubContext = new IoTHubContext();
            this.iotHubContext.Initialize(iotHubConnectionString);
            ReliableStateManager = this.StateManager;
            // TODO: Replace the following with your own logic.
            var myQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<Command>>("command");

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    bool isSuccess = true;
                    var queueItem = await myQueue.TryDequeueAsync(tx);
                    if (queueItem.HasValue)
                    {
                        try
                        {
                            var deviceId = "T5720022";
                            var message = new Message()
                            {
                                MessageId = queueItem.Value.CommandId
                            };

                            await this.iotHubContext.ServiceClient.SendAsync(deviceId, message).ConfigureAwait(false);
                            await this.UpdateCommand(queueItem.Value, "Sent");
                        }
                        catch (Exception exception)
                        {
                            isSuccess = false;
                            ServiceEventSource.Current.Message(exception.ToString());
                        }

                        if (!isSuccess)
                        {
                            await this.UpdateCommand(queueItem.Value, "Error");
                        }
                    }

                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <value>
        /// The database connection string.
        /// </value>
        public string DBConnectionString
        {
            get
            {
                return ServiceConfiguration.GetConfiguration("DBConnectionString");
            }
        }

        /// <summary>
        /// Updates the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        private async Task UpdateCommand(Command command, string status)
        {
            try
            {
                using (
                var conn =
                    new SqlConnection(this.DBConnectionString)
                )
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                    UPDATE [dbo].[Command] SET CommandStatus = '" + status + @"' where
                        [CommandId] = '" + command.CommandId + "'";

                    conn.Open();
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch(Exception exception)
            {
                ServiceEventSource.Current.Message(exception.ToString());
            }
        }
    }
}
