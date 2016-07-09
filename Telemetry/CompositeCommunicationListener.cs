// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeCommunicationListener.cs" company="Microsoft"> 
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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    /// <summary>
    /// a composite listener is an implementation of ICommunicationListener
    /// surfaced to Service Fabric as one listener but can be a # of 
    /// listeners grouped together. Supports adding listeners even after OpenAsync()
    /// has been called for the listener
    /// </summary>
    public class CompositeCommunicationListener : ICommunicationListener
    {
        private Dictionary<string, ICommunicationListener> listeners = new Dictionary<string, ICommunicationListener>();
        private AutoResetEvent listenerLock = new AutoResetEvent(true);


        public Func<CompositeCommunicationListener, Dictionary<string, string>, string> OnCreateListeningAddress { get; set; }

        public async Task ClearAll()
        {
            foreach (string key in this.listeners.Keys)
            {
                await this.RemoveListenerAsync(key);
            }
        }

        public void Abort()
        {
            try
            {
                this.listenerLock.WaitOne();

                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.listeners)
                {
                    this._AbortListener(kvp.Value);
                }
            }
            finally
            {
                this.listenerLock.Set();
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.listenerLock.WaitOne();

                List<Task> tasks = new List<Task>();
                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.listeners)
                {
                    tasks.Add(this._CloseListener(kvp.Value, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                this.listenerLock.Set();
            }
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult("Composite" + Guid.NewGuid());
        }

        public async Task AddListenerAsync(string Name, ICommunicationListener listener)
        {
            try
            {
                if (null == listener)
                {
                    throw new ArgumentNullException("listener");
                }

                if (this.listeners.ContainsKey(Name))
                {
                    throw new InvalidOperationException(string.Format("Listener with the name {0} already exists", Name));
                }


                this.listenerLock.WaitOne();

                this.listeners.Add(Name, listener);

                await this._OpenListener(listener, CancellationToken.None);
            }
            finally
            {
                this.listenerLock.Set();
            }
        }

        public async Task RemoveListenerAsync(string Name)
        {
            ICommunicationListener listener = null;

            try
            {
                if (!this.listeners.ContainsKey(Name))
                {
                    throw new InvalidOperationException(string.Format("Listener with the name {0} does not exists", Name));
                }

                listener = this.listeners[Name];


                this.listenerLock.WaitOne();
                await this._CloseListener(listener, CancellationToken.None);
            }
            catch (AggregateException aex)
            {
                AggregateException ae = aex.Flatten();

                // force abkrted
                if (null != listener)
                {
                    try
                    {
                        listener.Abort();
                    }
                    catch
                    {
                        /*no op*/
                    }
                }
            }
            finally
            {
                this.listeners.Remove(Name);
                this.listenerLock.Set();
            }
        }

        private async Task<string> _OpenListener(
            ICommunicationListener listener,
            CancellationToken canceltoken)
        {
            string sAddress = await listener.OpenAsync(canceltoken);
            return sAddress;
        }


        private async Task _CloseListener(
            ICommunicationListener listener,
            CancellationToken cancelToken)
        {
            await listener.CloseAsync(cancelToken);
        }

        private void _AbortListener(
            ICommunicationListener listener)
        {
            listener.Abort();
        }
    }
}