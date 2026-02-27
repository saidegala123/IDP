using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Services;
using DTPortal.Common;
using DTPortal.IDP.DTOs;
using DTPortal.Core.Domain.Services.Communication;
using System.Net.Http.Headers;
using DTPortal.Core.Services;
using Microsoft.Extensions.Logging;
using System.Xml;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Cors;
using DTPortal.Core.Constants;
using Microsoft.Extensions.Configuration;
using DTPortal.Core.Utilities;
using DTPortal.Core.DTOs;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IClientService _clientService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IConfiguration Configuration;
        private readonly OIDCConstants OIDCConstants;
        private readonly WebConstants WebConstants;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly ISubscriberService _subscriberService;
        private readonly IMessageLocalizer _messageLocalizer;
        public AuthenticationController(IAuthenticationService authenticationService,
            IClientService clientService,
            ILogger<AuthenticationController> logger,
            IConfiguration configuration,
            IGlobalConfiguration globalConfiguration,
            ISubscriberService subscriberService,
            IMessageLocalizer messageLocalizer)
        {
            _authenticationService = authenticationService;
            _clientService = clientService;
            _logger = logger;
            Configuration = configuration;
            _globalConfiguration = globalConfiguration;
            _subscriberService = subscriberService;
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
            WebConstants = errorConfiguration.WebConstants;
            if (null == WebConstants)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Endpoint for ValidateSession
        [Route("ValidateSession/{GlobalSessionId}")]
        [HttpGet]
        public async Task<IActionResult> ValidateSession(string GlobalSessionId)
        {
            _logger.LogDebug("--->ValidateSession");

            var result = await _authenticationService.ValidateSession(GlobalSessionId);

            if (!result.Success)
            {
                // Failure
                var response = new ResponseDTO
                {
                    Success = result.Success,
                    Message = result.Message
                };
                return Ok(response);
            }
            else
            {
                // Success
                var response = new ResponseDTO
                {
                    Success = result.Success,
                    Message = result.Message
                };

                return Ok(response);
            }

        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [Route("VerifyUserAuthData")]
        [HttpPost]
        public async Task<IActionResult> VerifyUserAuthData
            (VerifyUserAuthDataRequest requestObj)
        {
            _logger.LogDebug("---->VerifyUserAuthData");

            var result = await _authenticationService.VerifyUserAuthData(requestObj);

            if (!result.Success)
            {
                // Failure
                var response = new ResponseDTO
                {
                    Success = result.Success,
                    Message = result.Message
                };
                return Ok(response);
            }
            else
            {
                // Success
                //var response = new ResponseDTO
                //{
                //    Success = result.Success,
                //    Message = result.Message
                //};

                return Ok(result);
            }
        }
        
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [Route("token")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> GetAccessToken
            ([FromForm] GetAccessTokenRequest request)
        {
            _logger.LogDebug("--->GetAccessToken");

            // For Staging/Development
            _logger.LogDebug("GetAccessToken Request Data:: {0}", JsonConvert.SerializeObject(request));

            DTOs.ErrorResponseDTO errResponse = new DTOs.ErrorResponseDTO();
            var credential = string.Empty;
            var type = string.Empty;

            if (null == request)
            {
                _logger.LogError("Invalid input");
                errResponse.error = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidInput);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidInput);
                return Ok(errResponse);
            }

            var grantTypesSupported = Configuration
                .GetSection("grant_types_supported").Get<string[]>();
            if (grantTypesSupported == null)
            {
                _logger.LogError("Invalid grantTypesSupported " +
                    "or token_endpoint_auth_methods_supported recieved from config");
                errResponse.error = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                return Ok(errResponse);
            }

            if (string.IsNullOrEmpty(request.grant_type) ||
            !grantTypesSupported.Contains(request.grant_type))
            {
                _logger.LogError("Invalid Grant type recieved");
                errResponse.error = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidGrantType);
                return Ok(errResponse);
            }

            var idpConfiguration = _globalConfiguration.GetIDPConfiguration();
            if (null == idpConfiguration)
            {
                _logger.LogError("Get IDP Configuration failed");
                errResponse.error = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                return Ok(errResponse);
            }

            // Check the flag
            // IF the flag is true Allow only Private_key_jwt Authentication
            Core.Domain.Services.Communication.Common openIdCommonConfig = JsonConvert.DeserializeObject<Core.Domain.Services.Communication.Common>
                (idpConfiguration.common.ToString());
            if (null == openIdCommonConfig)
            {
                _logger.LogError("Get IDP Configuration failed");
                errResponse.error = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(WebConstants.InternalServerError);
                return Ok(errResponse);
            }

            if (openIdCommonConfig.TokenEndPointReqSigning)
            {
                if (string.IsNullOrEmpty(request.client_assertion) ||
                    string.IsNullOrEmpty(request.client_assertion_type) ||
                    request.client_assertion_type !=
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer")
                {
                    _logger.LogError(
                        "Invalid client_assertion/client_assertion_type recieved");
                    errResponse.error = _messageLocalizer.
                        GetMessage(OIDCConstants.InvalidInput);
                    errResponse.error_description =
                         _messageLocalizer.GetMessage(OIDCConstants.InvalidClientAssertionOrType);
                    return Ok(errResponse);
                }

                if (string.IsNullOrEmpty(request.code) ||
                    string.IsNullOrEmpty(request.redirect_uri) ||
                    string.IsNullOrEmpty(request.client_id))
                {
                    _logger.LogError(
                        "Invalid Code/RedirectUrl/ClientId recieved");
                    errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidInput);
                    errResponse.error_description =
                         _messageLocalizer.GetMessage(WebConstants.InvalidParams);
                    return Ok(errResponse);
                }
            }

            if (request.client_assertion_type == null)
            {
                // Check the value of authorization header
                var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogInformation("NO Authorization header received");
                    errResponse.error = _messageLocalizer.
                        GetMessage(OIDCConstants.InvalidClient);
                    errResponse.error_description = _messageLocalizer.
                        GetMessage(OIDCConstants.InvalidAuthZHeader);
                    return Unauthorized(errResponse);
                }

                // Parse the authorization header
                var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
                if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
                {
                    errResponse.error = _messageLocalizer.
                        GetMessage(OIDCConstants.InvalidClient);
                    errResponse.error_description = _messageLocalizer.
                        GetMessage(OIDCConstants.InvalidAuthZHeader);
                    return Unauthorized(errResponse);
                }

                if (authHeaderVal.Scheme.Contains("Basic"))
                {
                    credential = authHeaderVal.Parameter;
                    type = "client_secret_basic";
                }
            }
            if (request.client_assertion_type != null)
            {
                if (request.client_assertion_type.Contains
                    ("urn:ietf:params:oauth:client-assertion-type:jwt-bearer"))
                {
                    credential = request.client_assertion;
                    type = "private_key_jwt";
                }
            }
            var result = await _authenticationService.GetAccessToken(
                request, credential, type);
            if (!result.Success)
            {
                // Failure
                errResponse.error = result.error;
                errResponse.error_description = result.error_description;
                return Ok(errResponse);
            }
            else
            {
                if (null != result.scopes && result.scopes.Contains("openid"))
                {
                    if (string.IsNullOrEmpty(result.refresh_token))
                    {
                        // Success
                        var successResponse = new AccessTokenOpenIdResponseDTO();
                        successResponse.access_token = result.access_token;
                        successResponse.expires_in = result.expires_in;
                        successResponse.scopes = result.scopes;
                        successResponse.token_type = result.token_type;
                        successResponse.id_token = result.id_token;

                        return Ok(successResponse);
                    }
                    else
                    {
                        var successResponse = new AccessTokenOpenIdRefreshTokenDTO();
                        successResponse.access_token = result.access_token;
                        successResponse.expires_in = result.expires_in;
                        successResponse.scopes = result.scopes;
                        successResponse.token_type = result.token_type;
                        successResponse.id_token = result.id_token;
                        successResponse.refresh_token = result.refresh_token;

                        return Ok(successResponse);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(result.refresh_token))
                    {
                        // Success
                        var successResponse = new AccessTokenOAuthResponseDTO();
                        successResponse.access_token = result.access_token;
                        successResponse.expires_in = result.expires_in;
                        successResponse.scopes = result.scopes;
                        successResponse.token_type = result.token_type;
                        return Ok(successResponse);
                    }
                    else
                    {
                        // Success
                        var successResponse = new AccessTokenOAuthRefreshTokenDTO();
                        successResponse.access_token = result.access_token;
                        successResponse.expires_in = result.expires_in;
                        successResponse.scopes = result.scopes;
                        successResponse.token_type = result.token_type;
                        successResponse.refresh_token = result.refresh_token;
                        return Ok(successResponse);
                    }
                }
            }
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("ActivateSubscriber/{id}")]
        [HttpPost]
        public async Task<IActionResult> CheckandUpdateSubscriber(string id)
        {
            var result = await _subscriberService.CheckandUpdateSubscriber(id);

            var response = new ResponseDTO
            {
                Success = result.Success,
                Message = result.Message
            };

            return Ok(response);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("ClientConfiguration/{clientId}")]
        [HttpGet]
        public async Task<IActionResult> GetClientConfiguration(string clientId)
        {
            var result = await _clientService.GetSaml2Config(clientId);

            if (null == result)
            {
                // Failure
                var response = new ResponseDTO
                {
                    Success = false,
                    Message = "no client found with the clientd id"
                };
                return Ok(response);
            }
            else
            {
                var response = new ResponseDTO
                {
                    Success = true,
                    Message = string.Empty,
                    Result = result
                };
                return Ok(response);
            }
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("VerifyUserAuthNData")]
        [HttpPost]
        public async Task<IActionResult> VerifyUserAuthNData(
            VerifyUserAuthNDataRequest request)
        {
            _logger.LogDebug("---->VerifyUserAuthNData");

            if (null == request || null == request.allowedScopesAndClaims ||
                string.IsNullOrEmpty(request.authnToken) ||
                string.IsNullOrEmpty(request.authenticationScheme) ||
                string.IsNullOrEmpty(request.authenticationData) ||
                string.IsNullOrEmpty(request.approved))
            {
                _logger.LogError("Invalid Parameters Recieved");
                var response = new APIResponse();
                response.Success = false;
                response.Message = "Invalid Parameters Recieved";
                return Ok(response);
            }

            var result = await _authenticationService.VerifyUserAuthNData(request);
            if (null == result)
            {
                _logger.LogError("VerifyUserAuthNData Failed");
                var response = new APIResponse();
                response.Success = false;
                response.Message = "Internal Server Error";
                return Ok(response);
            }

            return Ok(result);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("VerifyAgentConsent")]
        [HttpPost]
        public async Task<IActionResult> VerifyAgentConsent(
            VerifyAgentConsentRequest request)
        {
            var result = await _authenticationService.VerifyAgentConsent(request);
            if (null == result)
            {
                _logger.LogError("VerifyUserAuthenticationData Failed");
                var response = new APIResponse();
                response.Success = false;
                response.Message = "Internal Server Error";
                return Ok(response);
            }

            return Ok(result);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("GetJourneyToken")]
        [HttpPost]
        public async Task<IActionResult> GenerateICPJourneyToken
            (ICPAuthRequest request)
        {
            _logger.LogDebug("---->GenerateICPJourneyToken");

            var result = await _authenticationService.ICPLoginVerify(request);
            if (null == result)
            {
                _logger.LogError("GenerateICPJourneyToken Failed");
                var response = new APIResponse();
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WebConstants.InternalServerError);
                return Ok(response);
            }

            return Ok(result);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Route("GenerateLoginJourneyTokenBySuid")]
        [HttpPost]
        public async Task<IActionResult> GenerateLoginJourneyTokenBySuid
            (ICPAuthRequest request)
        {
            _logger.LogDebug("---->GenerateLoginJourneyTokenBySuid");

            var result = await _authenticationService.ICPLoginVerify(request);
            if (null == result)
            {
                _logger.LogError("GenerateLoginJourneyTokenBySuid Failed");
                var response = new APIResponse();
                response.Success = false;
                response.Message = "Internal Server Error";
                return Ok(response);
            }

            return Ok(result);
        }
    }
}
