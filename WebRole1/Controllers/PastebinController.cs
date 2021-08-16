using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebRole1.Data;

namespace WebRole1.Controllers
{
    public class PastebinController : ApiController
    {
        // GET api/url/someurl
        public async Task<string> GetAsync(string shortUrlStub)
        {
            //Return Original Url from the Short Url
            string originalUrl = await PastebinDatabase.Instance.QueryPastebinItemAsync(shortUrlStub);
            return originalUrl != null ? originalUrl : "";
        }

        // POST api/url
        public async Task<string> PostAsync([FromBody] string PasteData)
        {
            //Create a Short Url from the Original Url
            string shortStubUrl = await PastebinDatabase.Instance.AddItemToContainerAsync(PasteData);
            return shortStubUrl != null ? shortStubUrl : "";
        }
    }
}