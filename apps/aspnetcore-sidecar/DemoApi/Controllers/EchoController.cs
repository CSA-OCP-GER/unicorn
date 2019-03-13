using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EchoController : ControllerBase
    {
        [HttpGet]
        public List<object> EchoHeaders()
        {
            return Request.Headers.Select(h => new { Title = h.Key, Values = h.Value.ToString() }).ToList<object>();
        }
    }
}