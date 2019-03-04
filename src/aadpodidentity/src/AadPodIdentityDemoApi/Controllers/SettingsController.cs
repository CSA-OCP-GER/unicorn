using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AadPodIdentityDemoApi.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AadPodIdentityDemoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsOptions _options;

        public SettingsController(IOptions<SettingsOptions> options)
        {
            _options = options.Value;
        }

        [HttpGet]
        [ProducesResponseType(typeof(SettingsOptions), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSettings()
        {
            await Task.Delay(10);
            return Ok(_options);
        }
    }
}