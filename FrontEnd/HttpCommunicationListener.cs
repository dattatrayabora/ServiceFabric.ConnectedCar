// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpCommunicationListener.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace FrontEnd
{
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    /// <summary>
    /// Http Communication Listener
    /// </summary>
    public class HttpCommunicationListener : ICommunicationListener
    {
        private readonly string publishUri;
        private readonly HttpListener httpListener;
        private readonly Func<HttpListenerContext, CancellationToken, Task> processRequest;
        private readonly CancellationTokenSource processRequestsCancellation = new CancellationTokenSource();

        public HttpCommunicationListener(ServiceInitializationParameters initParams, Func<HttpListenerContext, CancellationToken, Task> processRequest)
        {
            // Service instance's URL is the node's IP & desired port
            EndpointResourceDescription inputEndpoint = initParams.CodePackageActivationContext.GetEndpoint("WebApiServiceEndpoint");

            // This is the public-facing URL that HTTP clients.
            string uriPrefix = String.Format("{0}://+:{1}/connectedcar/", inputEndpoint.Protocol, inputEndpoint.Port);

            string uriPublished = uriPrefix.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            this.publishUri = uriPublished;
            this.processRequest = processRequest;
            this.httpListener = new HttpListener();
            this.httpListener.Prefixes.Add(uriPrefix);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.processRequestsCancellation.Cancel();
            this.httpListener.Close();
            return Task.FromResult(true);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            this.httpListener.Start();

            Task openTask = this.ProcessRequestsAsync(this.processRequestsCancellation.Token);

            return Task.FromResult(this.publishUri);
        }

        private async Task ProcessRequestsAsync(CancellationToken processRequests)
        {
            while (!processRequests.IsCancellationRequested)
            {
                HttpListenerContext request = await this.httpListener.GetContextAsync();

                // The ContinueWith forces rethrowing the exception if the task fails.
                Task requestTask = this.processRequest(request, this.processRequestsCancellation.Token)
                    .ContinueWith(async t => await t /* Rethrow unhandled exception */, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public void Abort()
        {
            this.processRequestsCancellation.Cancel();
            this.httpListener.Abort();
        }
    }
}