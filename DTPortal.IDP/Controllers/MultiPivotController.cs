using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.Domain.Services;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using DTPortal.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MultiPivotController : Controller
    {
        private readonly ILogger<MultiPivotController> _logger;
        private readonly ICategoryService _categoryService;
        private readonly IConfiguration Configuration;
        private readonly OIDCConstants OIDCConstants;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly IMessageLocalizer _messageLocalizer;
        public MultiPivotController(
            ILogger<MultiPivotController> logger,
            ICategoryService categoryService,
            IConfiguration configuration,
            IGlobalConfiguration globalConfiguration,
            IMessageLocalizer messageLocalizer)
        {
            _logger = logger;
            Configuration = configuration;
            _globalConfiguration = globalConfiguration;
            _categoryService = categoryService;
            var errorConfiguration = _globalConfiguration.
                GetErrorConfiguration();
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            OIDCConstants = errorConfiguration.OIDCConstants;
            if (null == OIDCConstants)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            _messageLocalizer = messageLocalizer;
        }
        [Route("GetCategoryList")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryList()
        {
            // Check the value of authorization header
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                DTOs.ErrorResponseDTO errResponse = new()
                {
                    error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken),
                    error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidAuthZHeader)
                };
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                DTOs.ErrorResponseDTO errResponse = new()
                {
                    error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken),
                    error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidAuthZHeader)
                };
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Bearer token type.Recieved {authHeaderVal.Scheme} type");
                DTOs.ErrorResponseDTO errResponse = new()
                {
                    error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken),
                    error_description = _messageLocalizer.GetMessage(OIDCConstants.UnsupportedAuthSchm)
                };
                return Unauthorized(errResponse);
            }

            APIResponse response = new APIResponse();
            try
            {
                var response1 = await _categoryService.GetCategoryListAsync();
                if (response1 == null)
                {
                    response.Success = false;
                    response.Message = "Internal Error";
                    return Ok(response);
                }
                response.Success = response1.Success;
                response.Message = response1.Message;
                response.Result = response1.Resource;
                return Ok(response);
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "Internal Error";
                return Ok(response);
            }
        }

        [Route("GetCategoryNameAndIdList")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryNameAndIdList()
        {
            var result = await _categoryService.GetCategoryNameAndIdListAsync();
            var apiResponse = new APIResponse()
            {
                Success = result.Success,
                Message = result.Message,
                Result = result.Resource
            };
            return Ok(apiResponse);
        }
    }
}
