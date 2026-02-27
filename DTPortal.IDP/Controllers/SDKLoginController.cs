using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using Google.Apis.Logging;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SDKLoginController : ControllerBase
    {
        private readonly ILogger<SDKLoginController> _logger;
        private readonly ISDKAuthenticationService _sdkAuthenticationService;
        public SDKLoginController(ILogger<SDKLoginController> logger,
            ISDKAuthenticationService sDKAuthenticationService)
        {
            _logger = logger;
            _sdkAuthenticationService = sDKAuthenticationService;
        }

        [Route("VerifyUser")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<IActionResult> VerifyUser(VerifyUserRequest requestObj)
        {
            var result=await _sdkAuthenticationService.VerifyUser(requestObj);
            return Ok(result);
        }

        [Route("VerifyUserAuthData")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<IActionResult> VerifyUserAuthData
            (VerifyUserAuthDataRequest requestObj)
        {
            var result = await _sdkAuthenticationService.VerifyUserAuthData(requestObj);
            return Ok(result);
        }

        [Route("GetVerifierUrl")]
        [EnableCors("AllowedOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetVerifierUrl()
        {
            var result = await _sdkAuthenticationService.GetVerificationUrl();
            return Ok(result);
        }

        [Route("IsUserVerifiedQrCode")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<IActionResult> IsUserVerifiedQrCode
            (VerifyQrRequest verifyQrCodeRequest)
        {
            var result = await _sdkAuthenticationService
                .IsUserVerifiedQrCode(verifyQrCodeRequest);
            return Ok(result);
        }
    }
}
