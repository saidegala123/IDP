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
using Microsoft.Extensions.Logging;
using AppShieldRestAPICore.Filters;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Newtonsoft.Json;
using DTPortal.Core.Utilities;
using Microsoft.Extensions.Configuration;
using DTPortal.Core.Constants;
using DTPortal.Core.Services;
using OtpSharp;
using System.Text.Json.Serialization;
using System.Text.Json;
using DTPortal.Core.DTOs;
using Base32;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger<UserInfoController> _logger;
        private readonly IUserInfoService _userInfoService;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly OIDCConstants OIDCConstants;
        private readonly IUserProfileService _userProfileService;
        private readonly IUserConsoleService _userConsoleService;
        private readonly IMessageLocalizer _messageLocalizer;

        public UserInfoController(ILogger<UserInfoController> logger, 
            IUserInfoService userInfoService,
            IGlobalConfiguration globalConfiguration,
            IConfiguration configuration,
            IUserProfileService userProfileService,
            IUserConsoleService userConsoleService,
            IMessageLocalizer messageLocalizer)
        {
            _logger = logger;
            _userInfoService = userInfoService;
            _globalConfiguration = globalConfiguration;
            Configuration = configuration;

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
            _userProfileService = userProfileService;
            _userConsoleService = userConsoleService;
            _messageLocalizer = messageLocalizer;
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [Route("userinfo")]
        [EnableCors("AllowedOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            GetUserInfoResponse response = new GetUserInfoResponse();

            // Check the value of authorization header
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Bearer token type.Recieved {0} type",
                    authHeaderVal.Scheme);
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.UnsupportedAuthSchm);
                return Unauthorized(errResponse);
            }

            bool signed = false;
            if (Request.Headers.TryGetValue("Accept", out var header))
            {
                if(header.Contains("application/jwt"))
                    signed = true;
            }

            //var result = await _userInfoService.GetUserInfo(authHeaderVal.Parameter, signed);
            var result = await _userInfoService.UserProfile(authHeaderVal.Parameter);
            if (null == result)
            {
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InternalError);
                return Unauthorized(errResponse);
            }
            if("ErrorResponseDTO" == result.GetType().Name)
            {
                return Unauthorized(result);
            }

            _logger.LogDebug("GetUserInfo response : {0}", result);

            if (true == signed)
            {
                Response.ContentType = "application/jwt";
                Response.StatusCode = 200;
                await Response.WriteAsync(result.ToString());
                return Ok();
            }

            _logger.LogDebug("<--GetUserInfo");
            return Ok(result);
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        [Route("userprofileNew")]
        [HttpGet]
        public async Task<IActionResult> GetUserProfileNew()
        {
            IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
            GetUserInfoResponse response = new GetUserInfoResponse();

            // Check the value of authorization header
            var authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
            {
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }

            _logger.LogInformation($"Authorization header recieved : {0}", authHeader);

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Bearer token type.Recieved {0} type",
                    authHeaderVal.Scheme);
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.UnsupportedAuthSchm);
                return Unauthorized(response);
            }

            var result = await _userInfoService.GetUserProfile(authHeaderVal.Parameter);

            if (!result.Success)
            {
                // Failure
                errResponse.error = result.Error;
                errResponse.error_description = result.Message;
                return Ok(errResponse);
            }
            else
            {
                // Success
                var successResponse = new UserInfoResponseDTO
                {
                    sub = result.Sub,
                    user_id = result.UserId,
                    name = result.Name,
                    birthdate = result.Dob.Value.ToString(),
                    gender = result.Gender,
                };

                if (null != result.MailId)
                {
                    successResponse.email = result.MailId;
                }
                if (null != result.MobileNo)
                {
                    successResponse.phone_number = result.MobileNo;
                }

                return Ok(successResponse);
            }

        }

        [Route("GetUserProfile")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<IActionResult> GetUserProfile(GetUserProfileRequest request)
        {
            if (null == request || string.IsNullOrEmpty(request.UserId) ||
               string.IsNullOrEmpty(request.UserIdType))
            {
                _logger.LogError("Invalid Parameters Recieved");
                return Ok(new GetUserProfileResponse("Invalid Parameters"));
            }
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(errResponse);
            }

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(errResponse);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Bearer token type.Recieved {0} type",
                    authHeaderVal.Scheme);
                IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.UnsupportedAuthSchm);
                return Unauthorized(errResponse);
            }
            request.Token = authHeaderVal.Parameter;
            // Get User Profile Data
            var response = await _userProfileService.GetUserProfileDataNewAsync(request);
            return Ok(response);
        }

        [Route("GetUserImage")]
        [EnableCors("AllowedOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetUserImage()
        {
            IDP.DTOs.ErrorResponseDTO errResponse = new IDP.DTOs.ErrorResponseDTO();
            GetUserInfoResponse response = new GetUserInfoResponse();

            // Check the value of authorization header
            var authHeader = Request.Headers[Configuration["AccessTokenHeaderName"]];
            if (string.IsNullOrEmpty(authHeader))
            {
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }

            _logger.LogInformation($"Authorization header recieved : {0}", authHeader);

            // Parse the authorization header
            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (null == authHeaderVal.Scheme || null == authHeaderVal.Parameter)
            {
                _logger.LogError("Invalid scheme or parameter in Authorization header");
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.InvalidAuthZHeader);
                return Unauthorized(response);
            }

            // Check the authorization is of Bearer type
            if (!authHeaderVal.Scheme.Equals("bearer",
                 StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Token is not Bearer token type.Recieved {0} type",
                    authHeaderVal.Scheme);
                errResponse.error = _messageLocalizer.GetMessage(OIDCConstants.InvalidToken);
                errResponse.error_description = _messageLocalizer.
                    GetMessage(OIDCConstants.UnsupportedAuthSchm);
                return Unauthorized(response);
            }

            var result = await _userInfoService.GetUserImage(authHeaderVal.Parameter);

            return Ok(result);
        }

        [Route("GetUserAuthData")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<APIResponse> GetUserAuthData([FromBody] UserAuthDataReq req)
        {

            if (string.IsNullOrEmpty(req.suid))
            {
                return new APIResponse()
                {
                    Success = false,
                    Message = "Suid cannot be null or empty",
                };
            }

            if (string.IsNullOrEmpty(req.priauthscheme))
            {
                return new APIResponse()
                {
                    Success = false,
                    Message = "Authentication scheme cannot be null or empty",
                };
            }

            if (req.priauthscheme != AuthNSchemeConstants.MOBILE_TOTP)
            {
                return new APIResponse()
                {
                    Success = false,
                    Message = "Authentication scheme not match",
                };
            }

            var UserAuthDataRes = new UserAuthDataRes()
            {
                priauthscheme = req.priauthscheme,
            };

            var userauthdata = new UserAuthDatum
            {
                AuthScheme = req.priauthscheme,
                UserId = req.suid
            };

            var response = await _userConsoleService.GetUserAuthDataAsync(userauthdata);
            if (response.Success)
            {
                UserAuthDataRes.AuthData = response.Result.AuthData;

                return new APIResponse()
                {
                    Success = true,
                    Message = response.Message,
                    Result = UserAuthDataRes
                };

            }
            else
            {
                if (req.priauthscheme == AuthNSchemeConstants.MOBILE_TOTP)
                {
                    byte[] secretKey = KeyGeneration.GenerateRandomKey(20);
                    userauthdata.AuthData = Base32Encoder.Encode(secretKey);
                    UserAuthDataRes.AuthData = userauthdata.AuthData;
                }

                var response1 = await _userConsoleService.ProvisionExternalUser(userauthdata);
                if (!response1.Success)
                {
                    return new APIResponse()
                    {
                        Success = false,
                        Message = response1.Message,
                    };
                }
                else
                {

                    return new APIResponse()
                    {
                        Success = true,
                        Message = response1.Message,
                        Result = UserAuthDataRes
                    };

                }
            }

        }

        [Route("GetAgentDetails")]
        [EnableCors("AllowedOrigins")]
        [HttpPost]
        public async Task<IActionResult> GetAgentDetails(GetAgentDetailsDTO request)
        {
            var response = await _userProfileService.GetAgentDetails(request);
            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }
    }
}
