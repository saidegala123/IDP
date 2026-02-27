using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserConsentController : ControllerBase
    {
        private readonly IUserProfilesConsentService _userProfilesConsentService;
        public UserConsentController(
            IUserProfilesConsentService userProfilesConsentService)
        {
            _userProfilesConsentService = userProfilesConsentService;
        }

        [Route("GetUserConsents")]
        [HttpGet]
        public async Task<IActionResult> GetUserConsents(string suid)
        {
            var res = await _userProfilesConsentService.
                GetUserProfilesConsentbySuidAsync(suid);
            APIResponse response = new APIResponse();
            response.Success = res.Success;
            response.Message = res.Message;
            response.Result = res.Resource;
            return Ok(response);
        }

        [Route("GetUserConsentsByClientName")]
        [HttpGet]
        public async Task<IActionResult> GetUserConsentsByClientName
            (string suid, string applicationName)
        {
            var res = await _userProfilesConsentService.
                GetUserProfilesConsentByClientNameAsync(suid,applicationName);
            APIResponse response = new APIResponse();
            response.Success = res.Success;
            response.Message = res.Message;
            response.Result = res.Resource;
            return Ok(response);
        }

        [Route("GetUserConsentsByProfile")]
        [HttpGet]
        public async Task<IActionResult> GetUserConsentsByProfile
            (string suid, string applicationName, string profile)
        {
            var res = await _userProfilesConsentService.
                GetUserProfilesConsentByProfileAsync(suid, applicationName, profile);
            APIResponse response = new APIResponse();
            response.Success = res.Success;
            response.Message = res.Message;
            response.Result = res.Resource;
            return Ok(response);
        }

        [Route("RevokeUserConsentsByProfile")]
        [HttpGet]
        public async Task<IActionResult> RevokeUserConsentsByProfile
            (string suid, string applicationName, string profile)
        {
            var res = await _userProfilesConsentService.
                RevokeUserProfilesConsentByProfileAsync(suid, applicationName, profile);
            APIResponse response = new APIResponse();
            response.Success = res.Success;
            response.Message = res.Message;
            response.Result = res.Resource;
            return Ok(response);
        }
    }
}
