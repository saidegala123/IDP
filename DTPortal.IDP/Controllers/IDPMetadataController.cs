using DTPortal.Core.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Newtonsoft.Json;
using DTPortal.Core.Domain.Services.Communication;
using Newtonsoft.Json.Linq;

namespace DTPortal.IDP.Controllers
{
    public class IDPMetadataController: Controller
    {
        private readonly ILogger<JwksController> _logger;
        private readonly IConfigurationService _configurationService;

        public IDPMetadataController(ILogger<JwksController> logger,
            IConfigurationService configurationService)
        {
            _logger = logger;
            _configurationService = configurationService;
        }

        [Route("OpenIDConfiguration")]
        [HttpGet]
        public async Task<IActionResult> OpenIDConfiguration()
        {
            JObject response = await _configurationService.
                GetConfigurationAsync<JObject>("IDP_Configuration");
            if (null == response)
            {
                _logger.LogError("Unable to Get Jwks_Config");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            var jwksString = response.GetValue("openidconnect").ToString();
            return Ok(jwksString);
        }
    }
}
