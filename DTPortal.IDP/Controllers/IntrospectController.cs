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
using DTPortal.Core.Domain.Repositories;
using DTPortal.Core.Utilities;
using static DTPortal.Common.CommonResponse;
using System.Text;
using DTPortal.Core.Constants;
using Microsoft.Extensions.Configuration;
using DTPortal.Core.Exceptions;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntrospectController : ControllerBase
    {
        // Initialize logger
        private readonly ILogger<IntrospectController> _logger;

        // Initialize Db
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheClient _cacheClient;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly MessageConstants Constants;
        private readonly OIDCConstants OIDCConstants;
        private readonly IConfiguration Configuration;
        private readonly IHelper _helper;
        private readonly IClientService _clientService;
        private readonly IMessageLocalizer _messageLocalizer;
        public IntrospectController(ILogger<IntrospectController> logger,
            IUnitOfWork unitofWork, IGlobalConfiguration globalConfiguration,
            ICacheClient cacheClient, IConfiguration configuration,
            IHelper helper, IClientService clientService,
            IMessageLocalizer messageLocalizer)
        {
            _logger = logger;
            _unitOfWork = unitofWork;
            _cacheClient = cacheClient;
            _globalConfiguration = globalConfiguration;
            Configuration = configuration;
            _helper = helper;
            _clientService = clientService;

            var errorConfiguration = _globalConfiguration.
                GetErrorConfiguration();
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            Constants = errorConfiguration.Constants;
            OIDCConstants = errorConfiguration.OIDCConstants;
            _messageLocalizer = messageLocalizer;
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [Route("introspect")]
        [HttpPost]
        public async Task<IActionResult> VerifyToken(
            [FromForm] VerifyTokenReq verifyTokenReq)
        {
            _logger.LogDebug("-->VerifyToken");

            // Check the value of authorization header
            var authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
            {
                ErrorResponse response = new ErrorResponse();
                _logger.LogError("Authorization header not found in request");
                response.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }
            _logger.LogDebug("Authorization header recieved : {0}", authHeader);

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }

            // Check the authorization is of Basic type
            if (!authHeaderVal.Scheme.Equals("basic",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Basic token type.Recieved :{0} type",
                        authHeaderVal.Scheme);
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.UnsupportedAuthSchm);
                return Unauthorized(response);
            }

            // Validate the client credentials and scope
            var result = await VerifyClientAndScope(authHeaderVal.Parameter,
                OAuth2Constants.VerifyToken);
            if ("Success" != result && "InvalidScope" != result)
            {
                _logger.LogError("Invalid credentials");
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidClient);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.InvalidCredentials);
                return Unauthorized(response);
            }

            if (OIDCConstants.ClientNotActive.En == result)
            {
                _logger.LogError(OIDCConstants.ClientNotActive.En);
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.ClientNotActive);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.ClientNotActive);
                return Unauthorized(response);
            }

            // If client has no verify token scope,send error response
            if ("InvalidScope" == result)
            {
                _logger.LogError(OIDCConstants.insufficientScope.En);
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.insufficientScope);
                response.error_description = _messageLocalizer.GetMessage(OIDCConstants.insufficientScope);
                return Unauthorized(response);
            }

            // Create Inactive token response
            VerifyTokenInActiveRes verifyTokenInActiveRes =
                new VerifyTokenInActiveRes();

            Accesstoken accessToken = null;
            try
            {
                // Get the access token record
                accessToken = await _cacheClient.Get<Accesstoken>(
                    "AccessToken", verifyTokenReq.token);
                if (null == accessToken)
                {
                    _logger.LogError("Access token not found or expired");
                    verifyTokenInActiveRes.active = false;
                    return Ok(verifyTokenInActiveRes);
                }
            }
            catch (CacheException ex)
            {
                _logger.LogError("Failed to get Access Token Record");
                ErrorResponse response = new ErrorResponse();
                response.error = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                response.error_description = _helper.GetRedisErrorMsg(
                    ex.ErrorCode, ErrorCodes.REDIS_ACCESS_TOKEN_GET_FAILED);
                return Unauthorized(response);
            }
            var client = await _unitOfWork.Client.GetClientByClientIdAsync(
                accessToken.ClientId);
            string organizationId = string.Empty;
            if (null != client)
            {
                organizationId = client.OrganizationUid;
            }
            string kyc_methods = string.Empty;
            string profile = string.Empty;
            string attributes = string.Empty;
            // Create active token response
            VerifyTokenActiveRes verifyTokenActiveRes =
                new VerifyTokenActiveRes
                {
                    active = true,
                    client_id = accessToken.ClientId,
                    username = accessToken.UserId,
                    scope = accessToken.Scopes,
                    org_id = organizationId,
                    supported_kyc_method = kyc_methods,
                    profile = profile,
                    attributes = attributes
                };

            _logger.LogInformation("VerifyToken response: {0}", verifyTokenActiveRes);
            _logger.LogInformation("<--VerifyToken");
            // Send active token response
            return Ok(verifyTokenActiveRes);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [NonAction]
        // Validate the client credentials and scope
        private async Task<string> VerifyClientAndScope(
            string credentials, string scope)
        {
            _logger.LogDebug("-->VerifyClientAndScope");

            // Validate input
            if ((null == credentials) || (null == scope))
            {
                _logger.LogError(OIDCConstants.InvalidInput.En);
                return "Failed";
            }

            try
            {
                var encoding = Encoding.GetEncoding("iso-8859-1");
                credentials = encoding.GetString(
                    Convert.FromBase64String(credentials));
            }
            catch (Exception error)
            {
                _logger.LogError("GetEncoding failed: {0}", error.Message);
                return "Failed";
            }

            int separator = credentials.IndexOf(':');
            if (-1 == separator)
            {
                _logger.LogError("credentials not found");
                return "Failed";
            }

            string clientId = credentials.Substring(0, separator);
            string clientSecret = credentials.Substring(separator + 1);

            Client client = null;
            try
            {
                // Get Client details from database
                client = await _unitOfWork.Client.GetClientByClientIdAsync(clientId);
                if (null == client)
                {
                    _logger.LogError("_unitOfWork.Client.GetClientByClientId failed");
                    return "Failed";
                }
            }
            catch (Exception)
            {
                var errorMessage = _helper.GetErrorMsg(ErrorCodes.DB_ERROR);
                return "Failed";
            }

            if (StatusConstants.ACTIVE != client.Status)
            {
                _logger.LogError("Client status is not active: {0}", client.Status);
                return _messageLocalizer.GetMessage(OIDCConstants.ClientNotActive);
            }

            if (client.ClientSecret != clientSecret)
            {
                _logger.LogError("Client secret not matched");
                return "Failed";
            }

            // Parse the space seperated scopes
            var scopes = client.Scopes.Split(' ');
            if (0 == scope.Length)
            {
                _logger.LogError("Client scopes not found");
                return "Failed";
            }

            // Check for verify token scope
            if (!scopes.Contains(scope))
            {
                _logger.LogError($"The scope is not there in access token scopes {0}",
                    scope);
                return "InvalidScope";
            }

            _logger.LogDebug("<--VerifyClientAndScope");
            return "Success";
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    }
}
