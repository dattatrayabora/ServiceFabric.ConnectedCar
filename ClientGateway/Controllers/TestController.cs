// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestController.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ClientGateway.Controllers
{
    using System.Collections.Generic;
    using System.Web.Http;

    [RoutePrefix("api/test")]
    public class TestController : ApiController
    {
        // GET api/values
        //[HttpGet]
        //public IEnumerable<string> Sample()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET api/values/5
        [HttpGet]
        [Route("{vin}/test")]
        [AllowAnonymous]
        public string Sample(string vin)
        {
            return "Hello World!!" + vin;
        }

        public IHttpActionResult Get()
        {
            return this.Ok("Hello Worldzz!");
        }

        //// POST api/values
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5
        //public void Delete(int id)
        //{
        //}
    }
}