using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.DTOs;
using DTPortal.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DTPortal.IDP.Controllers
{
    public class CredentialController : Controller
    {
        private readonly ICredentialService _credentialService;
        private readonly IConfiguration _configuration;
        private readonly IMessageLocalizer _messageLocalizer;
        private readonly OIDCConstants OIDCConstants;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly ILogger<CredentialController> _logger;
        public CredentialController(ICredentialService credentialService,
            IGlobalConfiguration globalConfiguration,
            IConfiguration configuration,ILogger<CredentialController> logger,
            IMessageLocalizer messageLocalizer)
        {
            _credentialService = credentialService;
            _logger= logger;
            _globalConfiguration = globalConfiguration;
            _configuration = configuration;
            _messageLocalizer = messageLocalizer;

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
        }

        [HttpGet]
        public async Task<IActionResult> GetCredentialList()
        {
            var response = await _credentialService.GetCredentialList();

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetActiveCredentialList()
        {
            var authHeader = Request.Headers[_configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }
            var response = await _credentialService.
                GetActiveCredentialList(authHeaderVal.Parameter);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialListByOrgUid(string orgUid)
        {
            var response = await _credentialService.GetCredentialListByOrgId(orgUid);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialById(int Id)
        {
            var response = await _credentialService.GetCredentialById(Id);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialByUid(string Id)
        {
            var response = await _credentialService.GetCredentialByUid(Id);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialOfferByUid(string Id)
        {
            var authHeader = Request.Headers[_configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = "Invalid Token";
                errResponse.error_description = "Invalid Token";
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = "Invalid Token";
                errResponse.error_description = "Invalid Token";
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = "Invalid Token";
                errResponse.error_description = "Invalid Token";
                return Unauthorized(errResponse);
            }
            var response = await _credentialService.GetCredentialOfferByUid(Id,authHeaderVal.Parameter);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpPost]
        public async Task<IActionResult> CreateCredential([FromBody] CredentialDTO credentialDto)
        {

            var response = await _credentialService.CreateCredentialAsync(credentialDto);

            APIResponse apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateCredential([FromBody] CredentialDTO credentialDto)
        {
            var response = await _credentialService.UpdateCredential(credentialDto);
            APIResponse apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = null
            };
            return Ok(apiResponse);
        }
        [HttpPost]
        public async Task<IActionResult> TestCredential
            ([FromBody] TestCredentialRequest testCredentialRequest)
        {
            var response = await _credentialService.
                TestCredential(testCredentialRequest.UserId, testCredentialRequest.CredentialId);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
        [HttpGet]
        public async Task<IActionResult> ActivateCredential(string credentialId)
        {
            var response = await _credentialService.ActivateCredential(credentialId);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialDetails(string credentialId)
        {
            var response = await _credentialService.GetCredentialDetails(credentialId);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetCredentialNameIdList(string credentialId)
        {
            var response = await _credentialService.GetCredentialNameIdListAsync(credentialId);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetVerifiableCredentialList(string orgId)
        {
            var response = await _credentialService.GetVerifiableCredentialList(orgId);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
        [HttpGet]
        public IActionResult GetAuthSchemeList()
        {
            var authSchemeList = _configuration.GetSection("auth_schemes_supported").Get<string[]>();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (var authScheme in authSchemeList)
            {
                dict[authScheme] = authScheme;
            }
            return Ok(new APIResponse()
            {
                Success = true,
                Message = "Get Auth Scheme List Success",
                Result = dict
            });
        }
    }
}
