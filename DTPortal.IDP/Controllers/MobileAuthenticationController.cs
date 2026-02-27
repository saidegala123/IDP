using DTPortal.Core.Constants;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.DTOs;
using DTPortal.Core.Utilities;
using iTextSharp.text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DTPortal.IDP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MobileAuthenticationController : Controller
    {
        private readonly IMobileAuthenticationService _mobileAuthenticationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MobileAuthenticationController> _logger;
        private readonly ICredentialService _credentialService;
        private readonly IClientService _clientService;
        private readonly IOrganizationService _organizationService;
        private readonly IMessageLocalizer _messageLocalizer;
        private readonly MessageConstants Constants;
        private readonly OIDCConstants OIDCConstants;
        private readonly IGlobalConfiguration _globalConfiguration;
        public MobileAuthenticationController
            (IMobileAuthenticationService mobileAuthenticationService,
            IMessageLocalizer messageLocalizer,
            IConfiguration configuration,
            ICredentialService credentialService,
            IClientService clientService,
            IGlobalConfiguration globalConfiguration,
            ILogger<MobileAuthenticationController> logger,
            IOrganizationService organizationService)
        {
            _mobileAuthenticationService = mobileAuthenticationService;
            _configuration = configuration;
            _credentialService = credentialService;
            _clientService = clientService;
            _messageLocalizer = messageLocalizer;
            _globalConfiguration = globalConfiguration;
            _logger = logger;
            _organizationService = organizationService;

            var errorConfiguration = _globalConfiguration.
            GetErrorConfiguration();
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            Constants = errorConfiguration.Constants;
            if (null == Constants)
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


        [HttpGet("GetConsentDetails")]
        public async Task<IActionResult> GetConsentDetails
            (string sessionId, string userId)
        {
            var serviceResult = await _mobileAuthenticationService
                .GetConsentDetailsAsync(sessionId, userId);

            return Ok(new APIResponse()
            {
                Success = serviceResult.Success,
                Message = serviceResult.Message,
                Result = serviceResult.Resource

            });
        }

        [HttpPost("AuthenticateUser")]
        public async Task<IActionResult> AuthenticateUser
            ([FromBody] AuthenticateUserRequest request)
        {
            var response = await _mobileAuthenticationService
                .AuthenticateUserAsync(request);

            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }


        [HttpPost("VerifyClientDetails")]
        public async Task<IActionResult> VerifyClientDetails
            ([FromBody] AuthorizationRequest request)
        {
            var result = await _mobileAuthenticationService.
                VerifyClientDetails(request);

            if (!result.Success)
            {
                return Ok(new APIResponse()
                {
                    Success = result.Success,
                    Message = result.Message,
                });
            }
            var sessionId = (string)result.Resource;

            var Res = new
            {
                SessionId = sessionId
            };

            return Ok(new APIResponse()
            {
                Success = true,
                Message = _messageLocalizer.GetMessage(Constants.VerifiedClientDetails),
                Result = Res
            });
        }

        [HttpPost("VerifyUserAuthenticationData")]
        public async Task<IActionResult> VerifyUserAuthenticationData
            ([FromBody] AuthenticateUserRequest request)
        {
            var response = await _mobileAuthenticationService
                .AuthenticateUserAsync(request);

            if (!response.Success)
            {
                return Ok(new APIResponse()
                {
                    Success = response.Success,
                    Message = response.Message,
                    Result = response.Resource
                });
            }

            var result = await _mobileAuthenticationService
                .GetAuthorizationCode(request.SessionId);

            if (!response.Success)
            {
                return Ok(new APIResponse()
                {
                    Success = result.Success,
                    Message = result.Message
                });
            }
            var Res = new
            {
                AuthorizationCode = result.AuthorizationCode
            };
            return Ok(new APIResponse()
            {
                Success = result.Success,
                Message = result.Message,
                Result = Res
            });
        }

        [HttpGet("GetCredentialOffer")]
        public async Task<IActionResult> GetCredentialOffer()
        {
            var authHeader = Request.Headers[_configuration["AccessTokenHeaderName"]];
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
            var credentialId = _configuration["MobileAuthentication:CredentialId"];
            var response = await _credentialService.
                GetCredentialOfferByUid(credentialId, authHeaderVal.Parameter);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }

        [HttpPost("AddWalletTransactionLog")]
        public async Task<IActionResult> AddWalletTransactionLog
            (WalletTransactionRequestDTO walletTransactionRequestDTO)
        {
            var result = await _mobileAuthenticationService.
                AddTransactionLog(walletTransactionRequestDTO);

            return Ok(new APIResponse()
            {
                Success = result.Success,
                Message = result.Message,
                Result = result.Resource
            });
        }

        [HttpPost("GetTransactionLog")]
        public async Task<IActionResult> GetTransactionLog
            (AuthenticationTransactionRequest request)
        {
            var result = await _mobileAuthenticationService.
                GetAuthenticationTransactionLog(request.suid, request.pageNumber);

            if (result == null)
            {
                return Ok(new APIResponse()
                {
                    Success = false,
                    Message = _messageLocalizer.GetMessage(Constants.InternalError),
                    Result = null
                });
            }

            var clientDictionary = await _clientService.GetClientOrgApplicationMap();

            if (clientDictionary == null)
            {
                return Ok(new APIResponse()
                {
                    Success = false,
                    Message = _messageLocalizer.GetMessage(Constants.InternalError),
                    Result = null
                });
            }

            var organizationDictionary = await _organizationService
                .GetOrganizationsDictionary();

            if (organizationDictionary == null)
            {
                return Ok(new APIResponse()
                {
                    Success = false,
                    Message = _messageLocalizer.GetMessage(Constants.InternalError),
                    Result = null
                });
            }

            AuthenticationTransactionResponse resultLogs = new AuthenticationTransactionResponse();

            List<AuthenticationTransaction> logs = new List<AuthenticationTransaction>();

            if (clientDictionary != null)
            {
                foreach (var log in result)
                {
                    var resultLog = new AuthenticationTransaction
                    {
                        dateTime = log.EndTime,
                        authenticationStatus = log.AuthenticationType,
                        serviceName = log.ServiceName,
                        Id = log._id
                    };

                    if (clientDictionary.ContainsKey(log.ServiceProviderAppName))
                    {
                        var appInfo = clientDictionary[log.ServiceProviderAppName];
                        resultLog.serviceProviderAppName = appInfo.ApplicationName;
                        if (organizationDictionary.ContainsKey(appInfo.OrganizationUid))
                        {
                            resultLog.serviceProviderName = organizationDictionary[appInfo.OrganizationUid];
                        }
                        else
                        {
                            resultLog.serviceProviderName = "N/A";
                        }
                    }
                    else
                    {
                        resultLog.serviceProviderAppName = "N/A";
                        resultLog.serviceProviderName = "N/A";
                    }

                    logs.Add(resultLog);
                }
            }

            resultLogs.authenticationTransactions = logs;
            resultLogs.hasMoreResults = result.HasNextPage;
            resultLogs.totalResults = result.TotalCount;

            return Ok(new APIResponse()
            {
                Success = true,
                Message = _messageLocalizer.GetMessage(Constants.TransactionLogsFetchedSuccessfully),
                Result = resultLogs
            });
        }

        [HttpGet("GetServiceProviderAppDetails")]
        public async Task<IActionResult> GetServiceProviderAppDetails()
        {
            var authHeader = Request.Headers[_configuration["AccessTokenHeaderName"]];
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

            var response = await _mobileAuthenticationService.
                GetServiceProviderAppDetails(authHeaderVal.Parameter);

            var apiResponse = new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            };

            return Ok(apiResponse);
        }

        [HttpPost("ApproveConsent")]
        public async Task<IActionResult> ApproveConsent
            (ConsentApprovalRequest request)
        {
            var response = await _mobileAuthenticationService
                .SaveConsentAsync(request);
            return Ok(new APIResponse()
            {
                Success = response.Success,
                Message = response.Message,
                Result = response.Resource
            });
        }

        [HttpGet("GetLogDetails")]
        public async Task<IActionResult> GetLogDetails(string Identifier)
        {
            var response = await _mobileAuthenticationService
                .GetLogDetailsAsync(Identifier);

            if (!response.Success)
            {
                return Ok(new APIResponse()
                {
                    Success = response.Success,
                    Message = response.Message,
                    Result = response.Resource
                });
            }
            string callStack = null;

            var logDetails = (LogMessage)response.Resource;

            if(logDetails.serviceName != LogClientServices.walletAuthenticationLog)
            {
                var log=await _mobileAuthenticationService.GetAuthenticationDetailsAsync
                    (logDetails.correlationID,logDetails.serviceProviderAppName);

                if (!log.Success)
                {
                    return Ok(new APIResponse()
                    {
                        Success = log.Success,
                        Message = log.Message,
                        Result = log.Resource
                    });  
                }
                callStack = ((LogReportDTO)log.Resource).CallStack;
            }
            else
            {
                callStack = logDetails.callStack;
            }

            var scopes = _mobileAuthenticationService.
                    GetScopeDetailsAsync(callStack, logDetails.serviceName);

            return Ok(new APIResponse()
            {
                Success = scopes.Success,
                Message = scopes.Message,
                Result = scopes.Resource
            });
        }

        [HttpGet("GetAuthenticationLogsCount")]
        public async Task<IActionResult> GetAuthenticationLogsCount(string suid)
        {
            var result = await _mobileAuthenticationService.
                GetTransactionLogCount(suid);
            if (result == null)
            {
                return Ok(new APIResponse()
                {
                    Success = false,
                    Message = _messageLocalizer.GetMessage(Constants.InternalError),
                    Result = null
                });
            }
            return Ok(new APIResponse()
            {
                Success = true,
                Message = _messageLocalizer.GetMessage(Constants.TransactionLogsCountFetchedSuccessfully),
                Result = result.Resource
            });
        }
    }
}
