


namespace FrontEnd
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class FrontEnd : StatelessService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private static readonly Uri clientServiceUri = new Uri(@"fabric:/ConnectedCar/ClientGateway");
        private ServicePartitionResolver servicePartitionResolver;

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            this.servicePartitionResolver = ServicePartitionResolver.GetDefault();
            return new[] { new ServiceInstanceListener((initParams) => new HttpCommunicationListener(initParams, this.ProcessInputRequest)) };
        }

        private async Task ProcessInputRequest(HttpListenerContext context, CancellationToken cancelRequest)
        {
            String output = null;

            try
            {
                string vin = context.Request.QueryString["vin"];

                // The partitioning scheme of the processing service is a range of integers from 0 - 25.
                // This generates a partition key within that range by converting the first letter of the input name
                // into its numerica position in the alphabet.
                char firstLetterOfLastName = vin.First();
                int partitionKey = Char.ToUpper(firstLetterOfLastName) - 'A';

                // Get the Service Partition
                ResolvedServicePartition partition = await this.servicePartitionResolver.ResolveAsync(clientServiceUri, partitionKey, cancelRequest);
                ResolvedServiceEndpoint ep = partition.GetEndpoint();

                JObject addresses = JObject.Parse(ep.Address);
                string primaryReplicaAddress = addresses["Endpoints"]["Gateway"].Value<string>();

                string url = primaryReplicaAddress + "api/vehicles/" + vin + "/lock";

                UriBuilder primaryReplicaUriBuilder = new UriBuilder(url);

                string result = await this.httpClient.GetStringAsync(primaryReplicaUriBuilder.Uri);

                output = String.Format(
                    "Result: {0}. Partition key: '{1}' generated from the first letter '{2}' of input value '{3}'.",
                    result,
                    partitionKey,
                    firstLetterOfLastName,
                    vin);
            }
            catch (Exception ex)
            {
                output = ex.Message;
            }

            using (HttpListenerResponse response = context.Response)
            {
                if (output != null)
                {
                    byte[] outBytes = Encoding.UTF8.GetBytes(output);
                    response.OutputStream.Write(outBytes, 0, outBytes.Length);
                }
            }
        }
    }
}
