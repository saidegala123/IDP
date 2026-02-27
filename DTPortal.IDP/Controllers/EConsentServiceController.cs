using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EConsentServiceController : ControllerBase
    {
        private readonly IEConsentService _eConsentService;

        public EConsentServiceController(IEConsentService eConsentService)
        {
            _eConsentService = eConsentService;
        }


        [Route("GetClientProfiles/{clientId}")]
        [EnableCors("AllowedOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetClientProfiles(string clientId)
        {

            APIResponse response = new APIResponse();
            var res = await _eConsentService.GetClientProfiles(clientId);
            response.Success = res.Success;
            response.Result = res.Resource;
            response.Message = res.Message;
            return Ok(response);
        }

        [Route("GetClientPurposes/{clientId}")]
        [EnableCors("AllowedOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetClientPurposes(string clientId)
        {
            APIResponse response = new APIResponse();
            var res = await _eConsentService.GetClientPurposes(clientId);
            response.Success = res.Success;
            response.Result = res.Resource;
            response.Message = res.Message;
            return Ok(response);
        }
    }
}
