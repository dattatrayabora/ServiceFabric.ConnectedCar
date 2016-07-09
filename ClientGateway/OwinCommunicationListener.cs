// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OwinCommunicationListener.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClientGateway
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin.Hosting;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly string appRoot;
        private readonly IOwinAppBuilder startup;
        private string listeningAddress;
        private string publishAddress;

        /// <summary>
        ///     OWIN server handle.
        /// </summary>
        private IDisposable serverHandle;

        /// <summary>
        /// The service initialization parameters
        /// </summary>
        private ServiceInitializationParameters serviceInitializationParameters;

        public OwinCommunicationListener(ServiceInitializationParameters serviceInitializationParameters,string appRoot, IOwinAppBuilder startup)
        {
            this.serviceInitializationParameters = serviceInitializationParameters;
            this.startup = startup;
            this.appRoot = appRoot;
            ServiceConfiguration.InitializeServiceParameters(serviceInitializationParameters);
        }

        public void Initialize()
        {
            ServiceEventSource.Current.Message("Initialize");

            EndpointResourceDescription serviceEndpoint = this.serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            int port = serviceEndpoint.Port;

            if (this.serviceInitializationParameters is StatefulServiceInitializationParameters)
            {
                StatefulServiceInitializationParameters statefulInitParams = (StatefulServiceInitializationParameters)this.serviceInitializationParameters;

                this.listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}/{2}/{3}",
                    port,
                    statefulInitParams.PartitionId.ToString().Replace("-", ""),
                    statefulInitParams.ReplicaId,
                    this.appRoot);
            }
            else if (this.serviceInitializationParameters is StatelessServiceInitializationParameters)
            {
                this.listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(this.appRoot)
                        ? string.Empty
                        : this.appRoot.TrimEnd('/') + '/');
            }
            else
            {
                throw new InvalidOperationException();
            }

            //this.listeningAddress = String.Format(
            //   CultureInfo.InvariantCulture,
            //   "http://+:{0}/{1}",
            //   port,
            //   String.IsNullOrWhiteSpace(this.appRoot)
            //       ? String.Empty
            //       : this.appRoot.TrimEnd('/') + '/');

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Opening on {0}", this.publishAddress);

            try
            {
                this.Initialize();
                ServiceEventSource.Current.Message("Starting web server on {0}", this.publishAddress);

                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Configuration(appBuilder));

                return Task.FromResult(this.publishAddress);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Close");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            ServiceEventSource.Current.Message("Abort");

            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this.serverHandle != null)
            {
                try
                {
                    this.serverHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}