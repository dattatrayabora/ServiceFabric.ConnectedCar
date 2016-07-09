// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft"> 
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
    /// <summary>
    /// Common Constants
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// The event hub connection string key
        /// </summary>
        public const string EventHubConnectionStringKey = "EventHubConnectionString";

        /// <summary>
        /// The event hub name string key
        /// </summary>
        public const string EventHubNameStringKey = "EventHubName";

        /// <summary>
        /// The consumer group name key
        /// </summary>
        public const string ConsumerGroupNameKey = "ConsumerGroup";

        /// <summary>
        /// The device identifier key
        /// </summary>
        public const string DeviceIdKey = "DeviceId";

        /// <summary>
        /// The message identifier key
        /// </summary>
        public const string MessageIdKey = "MessageId";

        /// <summary>
        /// The definition dictionary
        /// </summary>
        public const string DefDictionary = "Defs";
    }
}