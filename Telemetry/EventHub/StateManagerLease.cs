// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateManagerLease.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace IoTProcessorManagement.Common
{
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Newtonsoft.Json;

    /// <summary>
    /// Manages Event Hub Leases (where sequence/partition) are maintained. 
    /// each instance represents a cursor maintained by the client.
    /// this lease type uses Reliable State (of Service Fabric) to for storage.
    /// </summary>
    internal class StateManagerLease : Lease
    {
        public static readonly string DefaultEntryNameFormat = "_LEASE-{0}-{1}-{2}-{3}";
        private IReliableStateManager m_StateManager;
        private IReliableDictionary<string, string> m_StateDictionary;
        private string m_EntryName;

        public StateManagerLease()
        {
        }

        private StateManagerLease(
            IReliableStateManager StateManager,
            IReliableDictionary<string, string> StateDictionary,
            string EntryName,
            string partitionId)
        {
            this.m_StateManager = StateManager;
            this.m_StateDictionary = StateDictionary;
            this.m_EntryName = EntryName;
            this.PartitionId = partitionId;
        }

        public static string GetDefaultLeaseEntryName(string ServiceBusNamespace, string ConsumerGroupName, string EventHubName, string PartitionId)
        {
            return string.Format(DefaultEntryNameFormat, ServiceBusNamespace, ConsumerGroupName, EventHubName, PartitionId);
        }

        public static Task<StateManagerLease> GetOrCreateAsync(
            IReliableStateManager StateManager,
            IReliableDictionary<string, string> StateDictionary,
            string ServiceBusNamespace,
            string ConsumerGroupName,
            string EventHubName,
            string PartitionId)
        {
            string defaultEntryName = GetDefaultLeaseEntryName(ServiceBusNamespace, ConsumerGroupName, EventHubName, PartitionId);
            return GetOrCreateAsync(StateManager, StateDictionary, defaultEntryName, PartitionId);
        }

        /// <summary>
        /// Gets the or create asynchronous.
        /// </summary>
        /// <param name="StateManager">The state manager.</param>
        /// <param name="StateDictionary">The state dictionary.</param>
        /// <param name="EntryName">Name of the entry.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns>The Task</returns>
        public static async Task<StateManagerLease> GetOrCreateAsync(
            IReliableStateManager StateManager,
            IReliableDictionary<string, string> StateDictionary,
            string EntryName,
            string partitionId)
        {
            using (ITransaction tx = StateManager.CreateTransaction())
            {
                StateManagerLease lease;
                // if something has been saved before load it
                ConditionalResult<string> cResults = await StateDictionary.TryGetValueAsync(tx, EntryName);
                if (cResults.HasValue)
                {
                    lease = FromJsonString(cResults.Value);
                }
                else
                {
                    // if not create new
                    lease = new StateManagerLease(StateManager, StateDictionary, EntryName, partitionId);
                }
                await tx.CommitAsync();
                return lease;
            }
        }

        /// <summary>
        /// Determines whether this instance is expired.
        /// </summary>
        /// <returns>Is Expired</returns>
        public override bool IsExpired()
        {
            return false; // Service fabric lease does not expire
        }

        /// <summary>
        /// Saves the asynchronous.
        /// </summary>
        /// <returns>The Task</returns>
        public async Task SaveAsync()
        {
            using (ITransaction tx = this.m_StateManager.CreateTransaction())
            {
                // brute force save
                string json = this.ToJsonString();
                await this.m_StateDictionary.AddOrUpdateAsync(tx, this.m_EntryName, json, (key, val) => { return json; });
                await tx.CommitAsync();
            }
        }

        /// <summary>
        /// Froms the json string.
        /// </summary>
        /// <param name="sJson">The json.</param>
        /// <returns>Lease</returns>
        private static StateManagerLease FromJsonString(string sJson)
        {
            return (StateManagerLease)JsonConvert.DeserializeObject<StateManagerLease>(sJson);
        }

        /// <summary>
        /// To the json string.
        /// </summary>
        /// <returns>To Json</returns>
        private string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}