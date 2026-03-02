using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.DTOs;
using DTPortal.Core.Utilities;
using DTPortal.IDP.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DTPortal.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CredentialVerifiersController : ControllerBase
    {
        private readonly ICredentialVerifiersService _credentialVerifiersService;
        private readonly IConfiguration Configuration;
        private readonly IMessageLocalizer _messageLocalizer;
        private readonly OIDCConstants OIDCConstants;
        private readonly ILogger<CredentialVerifiersController> _logger;
        private readonly IGlobalConfiguration _globalConfiguration;
        public CredentialVerifiersController
            (ICredentialVerifiersService credentialVerifiersService,IMessageLocalizer messageLocalizer,
            ILogger<CredentialVerifiersController> logger,
            IConfiguration configuration, IGlobalConfiguration globalConfiguration)
        {
            _credentialVerifiersService = credentialVerifiersService;
            _logger = logger;
            Configuration = configuration;
            _messageLocalizer = messageLocalizer;
            _globalConfiguration = globalConfiguration;

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
        [Route("GetCredentialVerifiersList")]
        [HttpGet]
        public async Task<IActionResult> GetCredentialVerifiersList()
        {
            var response = await _credentialVerifiersService.
                GetCredentialVerifierDTOsListAsync();
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("GetActiveCredentialVerifiersList")]
        [HttpGet]
        public async Task<IActionResult> GetActiveCredentialVerifiersList()
        {
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }
            var response = await _credentialVerifiersService.
                GetActiveCredentialVerifiersListAsync(authHeaderVal.Parameter);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("GetCredentialVerifiersListByOrganizationId/{orgId}")]
        [HttpGet]
        public async Task<IActionResult> GetCredentialVerifiersListByOrganizationId
            (string orgId)
        {
            if (string.IsNullOrWhiteSpace(orgId))
                return BadRequest("Organization Id is required.");

            var response = await _credentialVerifiersService.
                GetCredentialVerifiersListByOrganizationIdAsync(orgId);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("GetActiveCredentialVerifiersListByOrganizationId/{orgId}")]
        [HttpGet]
        public async Task<IActionResult> GetActiveCredentialVerifiersListByOrganizationId
            (string orgId)
        {
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];

            if (string.IsNullOrWhiteSpace(orgId))
                return BadRequest("Organization Id is required.");

            if (string.IsNullOrEmpty(authHeader))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                ErrorResponseDTO errResponse = new ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                return Unauthorized(errResponse);
            }

            var response = await _credentialVerifiersService.
                GetActiveCredentialVerifiersListByOrganizationIdAsync
                (orgId, authHeaderVal.Parameter);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("GetCredentialVerifierById/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetCredentialVerifierById(int id)
        {
            var response = await _credentialVerifiersService.
                GetCredentialVerifierByIdAsync(id);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }


        [Route("CreateCredentialVerifier")]
        [HttpPost]
        public async Task<IActionResult> CreateCredentialVerifier
            (CredentialVerifierDTO credentialVerifierDTO)
        {
            var response = await _credentialVerifiersService.
                CreateCredentialVerifierAsync(credentialVerifierDTO);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("UpdateCredentialVerifier")]
        [HttpPost]
        public async Task<IActionResult> UpdateCredentialVerifier
            (CredentialVerifierDTO credentialVerifierDTO)
        {
            var response = await _credentialVerifiersService.
                UpdateCredentialVerifierAsync(credentialVerifierDTO);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("GetCredentialVerifierListByIssuerId/{orgId}")]
        [HttpGet]
        public async Task<IActionResult> GetCredentialVerifierListByIssuerId
            (string orgId)
        {
            if (string.IsNullOrWhiteSpace(orgId))
                return BadRequest("Organization Id is required.");

            var response = await _credentialVerifiersService.
                GetCredentialVerifierListByIssuerId(orgId);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);
        }

        [Route("ActivateCredential")]
        [HttpPost]
        public async Task<IActionResult> ActivateCredential
            ([FromBody] ActivateCredentialDTO activateCredentialDTO)
        {
            var response = await _credentialVerifiersService.
                ActivateCredentialById(activateCredentialDTO.Id);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);

        }

        [Route("RejectCredential")]
        [HttpPost]
        public async Task<IActionResult> RejectCredential
            ([FromBody] ActivateCredentialDTO activateCredentialDTO)
        {
            var response = await _credentialVerifiersService.
                RejectCredentialById(activateCredentialDTO.Id, activateCredentialDTO.Remarks);
            var result = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };
            return Ok(result);

        }
    }
}
