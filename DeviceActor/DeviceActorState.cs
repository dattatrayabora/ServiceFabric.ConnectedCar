// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeviceActorState.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DeviceActor
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// State for the Device Actor
    /// </summary>
    [DataContract]
    public class DeviceActorState
    {
        /// <summary>
        /// The device state
        /// </summary>
        [DataMember]
        public string DeviceState;

        /// <summary>
        /// The last command identifier processed
        /// </summary>
        [DataMember]
        public Guid LastCommandId;
    }
}