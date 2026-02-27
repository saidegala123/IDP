using DTPortal.Core.Constants;
using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.Utilities;
using DTPortal.IDP.Attribute;
using DTPortal.IDP.ViewModel.Oauth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DTPortal.IDP.Controllers
{

    // [CustomAuthorization]
    [Route("authorization")]
    [ServiceFilter(typeof(CustomAuthorizationAttribute))]
    public class Oauth2Controller : Controller
    {
        private DTPortal.Core.Domain.Services.IAuthenticationService
                            _authenticationService;
        private IUserConsentService _userConsentService;
        private ITokenManagerService _tokenManager;
        private readonly IConfigurationService _configurationService;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly ILogger<Oauth2Controller> _logger;
        private readonly WebConstants WEBConstants;
        private readonly MessageConstants ErrorMsgConstants;
        private readonly IScopeService _scopeService;
        private DTPortal.Core.Domain.Services.IClientService _clientService;
        private readonly IUserClaimService _userClaimService;
        private readonly IHelper _helper;
        private readonly IMessageLocalizer _messageLocalizer;
        public Oauth2Controller(ILogger<Oauth2Controller> logger,
                      IConfigurationService configurationService,
                      ITokenManagerService tokenManager,
                      IGlobalConfiguration globalConfiguration,
                      DTPortal.Core.Domain.Services.IAuthenticationService authenticationService,
                      IUserConsentService userConsentService,
                      IHelper helper,
                      IScopeService scopeService,
                      IUserClaimService userClaimService,
                      DTPortal.Core.Domain.Services.IClientService clientService,
                      IMessageLocalizer messageLocalizer)
        {
            _authenticationService = authenticationService;
            _configurationService = configurationService;
            _userConsentService = userConsentService;
            _tokenManager = tokenManager;
            _clientService = clientService;
            _logger = logger;
            _globalConfiguration = globalConfiguration;
            _helper = helper;
            _scopeService = scopeService;
            _userClaimService = userClaimService;

            var errorConfiguration = _globalConfiguration.
               GetErrorConfiguration();
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            ErrorMsgConstants = errorConfiguration.Constants;
            if (null == ErrorMsgConstants)
            {
                _logger.LogError("Get Error Constants failed");
                throw new NullReferenceException();
            }

            WEBConstants = errorConfiguration.WebConstants;
            if (null == WEBConstants)
            {
                _logger.LogError("Get Error WebConstants failed");
                throw new NullReferenceException();
            }

            _messageLocalizer = messageLocalizer;
        }

        public APIResponse ValidateData(AuthorizationViewModel model,
            clientDetails jwtParams = null)
        {
            APIResponse response = new APIResponse();
            response.Success = true;
            response.Message = "";

            if (jwtParams != null)
            {
                if (!string.IsNullOrEmpty(jwtParams.clientId))
                {
                    model.client_id = jwtParams.clientId;
                }
                if (!string.IsNullOrEmpty(jwtParams.redirect_uri))
                {
                    model.redirect_uri = jwtParams.redirect_uri;
                }
                if (!string.IsNullOrEmpty(jwtParams.response_type))
                {
                    model.response_type = jwtParams.response_type;
                }
                if (!string.IsNullOrEmpty(jwtParams.scopes))
                {
                    model.scope = jwtParams.scopes;
                }
                if (!string.IsNullOrEmpty(jwtParams.state))
                {
                    model.state = jwtParams.state;
                }
                if (!string.IsNullOrEmpty(jwtParams.nonce))
                {
                    model.nonce = jwtParams.nonce;
                }
            }

            if (string.IsNullOrEmpty(model.client_id))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.ClientIdNotFound);
            }
            if (string.IsNullOrEmpty(model.redirect_uri) && !Uri.IsWellFormedUriString(
                model.redirect_uri, UriKind.Absolute))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.RedirectUriMissing);
            }
            if (string.IsNullOrEmpty(model.response_type))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.ResponseTypeNotFound)  ;
            }
            if (string.IsNullOrEmpty(model.scope))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.ScopeNotFound);
            }
            if (string.IsNullOrEmpty(model.state))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.StateNotFound);
            }
            if (model.scope.Contains("openid") && string.IsNullOrEmpty(model.nonce))
            {
                response.Success = false;
                response.Message = _messageLocalizer.GetMessage(WEBConstants.NonceNotFound);
            }

            return response;
        }

        [HttpGet]
        public async Task<IActionResult> Index(AuthorizationViewModel model)
        {
            try
            {
                _logger.LogDebug("---> Oauth2Controller Get");
                clientDetails clientData = null;

                var IDPconfigInDB = _globalConfiguration.GetIDPConfiguration();
                if (null == IDPconfigInDB)
                {
                    _logger.LogError("GetCode:fail to get idp_configuration");
                    ViewBag.error = WEBConstants.InternalError;
                    ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GET_IDPCONFIG_RES_NULL);
                    return View("Error");
                }

                if (string.IsNullOrEmpty(model.client_id))
                {
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?error=invalid_request_object" +
                        "&error_description=" + WEBConstants.ClientIdNotFound +
                        "&state=" + model.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = "invalid_request_object";
                        ViewBag.error_description = WEBConstants.ClientIdNotFound;
                        return View("Error");
                    }
                }

                // Get client details
                var client = await _clientService.GetClientByClientIdAsync(model.client_id);
                if (null == client)
                {
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?error=invalid_request_object" +
                        "&error_description=" + WEBConstants.ClientIdNotFound +
                        "&state=" + model.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = "invalid_request_object";
                        ViewBag.error_description = WEBConstants.ClientIdNotFound;
                        return View("Error");
                    }
                }

                Core.Domain.Services.Communication.Common openIdCommonConfig = JsonConvert.DeserializeObject<Core.Domain.Services.Communication.Common>
                        (IDPconfigInDB.common.ToString());
                if (openIdCommonConfig.RequestSigningMandatory)
                {
                    if (string.IsNullOrEmpty(model.request))
                    {
                        if (!string.IsNullOrEmpty(model.redirect_uri))
                        {
                            var url = model.redirect_uri + "?error=invalid_request_object" +
                            "&error_description= Request has missing request parameter" +
                            "&state=" + model.state;
                            return LocalRedirect(url);
                        }
                        else
                        {
                            ViewBag.error = "invalid_request_object";
                            ViewBag.error_description = "Request has missing request parameter";
                            return View("Error");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(model.request))
                {
                    var isTokenValid = _tokenManager.ValidateRequestJWToken(
                        model.request, model.client_id, client.PublicKeyCert);
                    if (!isTokenValid)
                    {
                        if (!string.IsNullOrEmpty(model.redirect_uri))
                        {
                            var url = model.redirect_uri + "?error=invalid_request_object&" +
                                "error_description=" + WEBConstants.InvalidJWT +
                                 "&state=" + model.state;
                            return LocalRedirect(url);
                        }
                        else
                        {
                            ViewBag.error = "invalid_request_object";
                            ViewBag.error_description = WEBConstants.InvalidJWT;
                            return View("Error");
                        }
                    }
                    clientData = _tokenManager.GetClientDetailsfromJwt(model.request);
                }

                var isValidParam = ValidateData(model, clientData);
                if (!isValidParam.Success)
                {
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?error=invalid_request&" +
                            "error_description=" + isValidParam.Message +
                             "&state=" + model.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = "invalid_request";
                        ViewBag.error_description = isValidParam.Message;
                        return View("Error");
                    }
                }

                if (openIdCommonConfig.OpenIdConnectMandatory)
                {
                    if (!model.scope.Contains("openid"))
                    {
                        if (!string.IsNullOrEmpty(model.redirect_uri))
                        {
                            var url = model.redirect_uri + "?error=invalid_request_object" +
                            "&error_description= Request has missing openid scope" +
                            "&state=" + model.state;
                            return LocalRedirect(url);
                        }
                        else
                        {
                            ViewBag.error = "invalid_request_object";
                            ViewBag.error_description = "Request has missing openid scope";
                            return View("Error");
                        }
                    }
                }

                //if (!model.scope.Contains("urn:idp:digitalid:profile") ||
                //    !model.scope.Contains("uaeid:idp:basic:profile"))
                //{
                //    if (!string.IsNullOrEmpty(model.redirect_uri))
                //    {
                //        var url = model.redirect_uri + "?error=invalid_request_object" +
                //        "&error_description= Invalid scopes" +
                //        "&state=" + model.state;
                //        return Redirect(url);
                //    }
                //    else
                //    {
                //        ViewBag.error = "invalid_request_object";
                //        ViewBag.error_description =
                //            "Request has Invalid scopes";
                //        return View("Error");
                //    }
                //}

                var clientDetails = new GetAuthSessClientDetails
                {
                    clientId = model.client_id,
                    redirect_uri = model.redirect_uri,
                    response_type = model.response_type,
                    scopes = model.scope,
                    withPkce = (string.IsNullOrEmpty(model.code_challenge) &&
                    string.IsNullOrEmpty(model.code_challenge_method)) ? false : true
                };

                var pkceDetails = new Pkcedetails
                {
                    codeChallenge = (!string.IsNullOrEmpty(model.code_challenge))
                                                        ? model.code_challenge : "",
                    codeChallengeMethod = (!string.IsNullOrEmpty(model.code_challenge_method))
                                                     ? model.code_challenge_method : ""
                };

                var data = new ValidateClientRequest
                {
                    clientDetails = clientDetails,
                    PkceDetails = pkceDetails,
                    clientDetailsInDb = client
                };

                var response = _authenticationService.ValidateClient(data);

                if (response == null)
                {
                    _logger.LogError("Oauth2Controller: Response value getting null ");
                    ViewBag.error = "Internal_Error";
                    ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GET_VALIDATCLIENT_RES_NULL);
                    return View("Error");
                }

                if (!response.Success)
                {
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?error=invalid_request&" +
                            "error_description=" + response.Message +
                             "&state=" + model.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = "invalid_request";
                        ViewBag.error_description = response.Message;
                        return View("Error");
                    }

                }

                model.Application_Name = response.Result;

                _logger.LogDebug("<--- Oauth2Controller Get");

                var GlobalSession = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "Session").Value;

                var LogResponse = await _authenticationService.SendAuthenticationLogMessage
                    (GlobalSession, model.client_id);

                if(LogResponse == null || !LogResponse.Success)
                {
                    ViewBag.error = WEBConstants.InternalError;
                    ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GET_METHOD_EXCP);
                    return View("Error");
                }

                var result = await _authenticationService.
                    IsUserGivenConsent(GlobalSession,model.client_id);

                if(result == null || !result.Success)
                {
                    var url = model.redirect_uri + "?error=invalid_request&" +
                        "error_description=" + result.Message +
                        "&state=" + model.state;
                }

                if (result.ConsentGiven)
                {
                    return RedirectToAction("Getcode", model);
                }

                return RedirectToAction("ConsentPage", model);
            }
            catch (Exception e)
            {
                _logger.LogError("Oauth2Controller Get:{0}", e.Message);
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GET_METHOD_EXCP);
                return View("Error");
            }
        }

        [Route("Getcode")]
        [HttpGet]
        public async Task<IActionResult> Getcode(AuthorizationViewModel model)
        {
            try
            {
                _logger.LogDebug("---> GetCode");
                var clientDetails = new clientDetails
                {
                    clientId = model.client_id,
                    redirect_uri = model.redirect_uri,
                    response_type = model.response_type,
                    scopes = model.scope,
                    nonce = (string.IsNullOrEmpty(model.nonce)) ? "" : model.nonce,
                    grant_type = (!string.IsNullOrEmpty(model.response_type) &&
                                model.response_type == "code") ?
                                "authorization_code" : "implicit",
                    withPkce = (string.IsNullOrEmpty(model.code_challenge) &&
                    string.IsNullOrEmpty(model.code_challenge_method)) ? true : false
                };

                var pkceDetails = new Pkcedetails
                {
                    codeChallenge = (!string.IsNullOrEmpty(model.code_challenge))
                                            ? model.code_challenge : "",
                    codeChallengeMethod = (!string.IsNullOrEmpty(model.code_challenge_method))
                                                              ? model.code_challenge_method : ""
                };

                var GlobalSession = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "Session").Value;
                if (string.IsNullOrEmpty(GlobalSession))
                {
                    _logger.LogError("GetCode : GlobalSession not found");
                    _logger.LogDebug("<--- GetCode");
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?error=Internal_Error" +
                            "&error_description=" + _helper.GetErrorMsg(ErrorCodes.SESSION_NOT_FOUND) +
                            "&state=" + model.state;

                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = WEBConstants.InternalError;
                        ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.SESSION_NOT_FOUND);
                        return View("GetCodeError");
                    }
                }
                var data = new GetAuthZCodeRequest
                {
                    ClientDetails = clientDetails,
                    GlobalSessionId = GlobalSession,
                    pkcedetails = pkceDetails
                };

                if (null != model.code_challenge)
                {
                    clientDetails.withPkce = true;
                    Pkcedetails pkcedetails = new Pkcedetails()
                    {
                        codeChallenge = model.code_challenge,
                        codeChallengeMethod = model.code_challenge_method
                    };
                    data.pkcedetails = pkcedetails;
                }

                var response = await _authenticationService.GetAuthorizationCode(data);
                if (response == null)
                {
                    _logger.LogError("GetCode: Response value getting null ");
                    ViewBag.error = WEBConstants.InternalError;
                    ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GETCODE_GETAUTHORIZATIONCODE_RES_NULL);
                    return View("GetCodeError");
                }
                if (response.Success)
                {
                    _logger.LogDebug("<--- GetCode");
                    if (!string.IsNullOrEmpty(model.redirect_uri))
                    {
                        var url = model.redirect_uri + "?code=" + response.AuthorizationCode +
                            "&state=" + model.state;

                        return LocalRedirect(url);
                    }
                    else
                    {
                        ViewBag.error = "invalid_request";
                        ViewBag.error_description = WEBConstants.RedirectUriMissing;
                        return View("GetCodeError");
                    }
                }
                else
                {
                    _logger.LogDebug("GetCode :{0}", response.Message);
                    if (response.Message.StartsWith("User denied consent") ||
                        response.Message.StartsWith("Subscriber denied consent"))
                    {
                        _logger.LogDebug("<--- GetCode");
                        if (!string.IsNullOrEmpty(model.redirect_uri))
                        {
                            var url = model.redirect_uri + "?error=Access_Denied" +
                                "&error_description=" + WEBConstants.DeniedConsent;
                            return LocalRedirect(url);
                        }
                        else
                        {
                            ViewBag.error = "Access_Denied";
                            ViewBag.error_description = WEBConstants.DeniedConsent;
                            return View("GetCodeError");
                        }
                    }
                    else if (response.Message.StartsWith("User Consent Required for") ||
                         response.Message.StartsWith("Subscriber Consent Required for"))
                    {
                        var Email = HttpContext.User.Claims
                           .FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;

                        var UserName = HttpContext.User.Claims
                            .FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;

                        var start = response.Message.IndexOf("(") + 1;
                        var end = response.Message.IndexOf(")");
                        var scope = response.Message.Substring(start, end - start);
                        _logger.LogDebug("GetCode -> required user consent " +
                            "for scopes:{0}", scope);
                        if (scope.Length == 0)
                        {
                            throw new Exception("Invalid Message :" + response.Message);
                        }
                        start = response.Message.IndexOf("[") + 1;
                        end = response.Message.IndexOf("]");
                        var suid = response.Message.Substring(start, end - start);
                        _logger.LogDebug("GetCode suid:{0}", suid);

                        var IDPconfigInDB = _globalConfiguration.GetIDPConfiguration();
                        if (null == IDPconfigInDB)
                        {
                            _logger.LogError("GetCode:fail to get idp_configuration");
                            ViewBag.error = WEBConstants.InternalError;
                            ViewBag.error_description = WEBConstants.SomethingWrong;
                            return View("Error");
                        }

                        var scopesListInDb = await _scopeService.ListScopeAsync();

                        if (null == scopesListInDb)
                        {
                            _logger.LogError("GetCode:fail to get scopes List");
                            ViewBag.error = WEBConstants.InternalError;
                            ViewBag.error_description = WEBConstants.SomethingWrong;
                            return View("Error");
                        }

                        var scopes = scope.Split(" ").ToList<string>();
                        var scopeList = new List<ScopeDetails>() { };

                        var attributesListInDb = await _userClaimService.ListUserClaimAsync();

                        foreach (var key in scopes)
                        {
                            foreach (var obj in scopesListInDb)
                            {
                                if (obj.Name == key)
                                {
                                    var attributesList = obj.ClaimsList?.Split(' ', StringSplitOptions.RemoveEmptyEntries).
                                        ToList() ?? new List<string>();

                                    List<AttributeDetails> attributesDetailsList = new List<AttributeDetails>();

                                    foreach (var attribute in attributesList)
                                    { 
                                        
                                        var attributeObj = attributesListInDb.FirstOrDefault(a => a.Name == attribute);
                                        if (attributeObj != null)
                                        {
                                            AttributeDetails attributeDetails = new AttributeDetails()
                                            {
                                                displayName = attributeObj.DisplayName,
                                                name = attributeObj.Name,
                                                mandatory = attributeObj.DefaultClaim
                                            };
                                            attributesDetailsList.Add(attributeDetails);
                                        } 
                                    }

                                    scopeList.Add(new ScopeDetails
                                    {
                                        name = key,
                                        displayName = obj.DisplayName,
                                        description = obj.Description,
                                        version = obj.Version,
                                        attributes = attributesDetailsList
                                    });
                                }
                            }
                        }
                        ;

                        var consentModel = new ConsentViewModel
                        {
                            clientDetails = model,
                            username = UserName,
                            usermail = Email,
                            suid = suid,
                            scopes = scope.Split(" ").ToList<string>(),
                            scopesList = scopeList
                        };

                        _logger.LogDebug("<--- GetCode");
                        return View("ConsentPage", consentModel);

                    }
                    else if (response.Message == "Client is not Active")
                    {
                        _logger.LogDebug("<--- GetCode");
                        if (!string.IsNullOrEmpty(model.redirect_uri))
                        {
                            var url = model.redirect_uri + "?error=Access_Denied" +
                                "&error_description=" + WEBConstants.ClientNotActive;
                            return LocalRedirect(url);
                        }
                        else
                        {
                            ViewBag.error = "Access_Denied";
                            ViewBag.error_description = WEBConstants.ClientNotActive;
                            return View("GetCodeError");
                        }

                    }
                    else
                    {
                        ViewBag.error = WEBConstants.InternalError;
                        ViewBag.error_description = response.Message;
                        return View("GetCodeError");
                    }

                }

            }
            catch (Exception e)
            {
                _logger.LogError("GetCode:{0}", e.Message);
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_GETCODE_METHOD_EXCP);
                return View("GetCodeError");
            }
        }

        [Route("ConsentPage")]
        [HttpGet]
        public async Task<IActionResult> ConsentPage(AuthorizationViewModel model)
        {
            var Email = HttpContext.User.Claims
                           .FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;

            var UserName = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;

            var Suid = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == "UserId").Value;

            var scope = model.scope;

            _logger.LogDebug("GetCode -> required user consent " +
                "for scopes:{0}", scope);
            if (scope.Length == 0)
            {
                throw new Exception("Invalid Scopes :");
            }

            var IDPconfigInDB = _globalConfiguration.GetIDPConfiguration();
            if (null == IDPconfigInDB)
            {
                _logger.LogError("GetCode:fail to get idp_configuration");
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = WEBConstants.SomethingWrong;
                return View("Error");
            }

            var scopesListInDb = await _scopeService.ListScopeAsync();

            if (null == scopesListInDb)
            {
                _logger.LogError("GetCode:fail to get scopes List");
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = WEBConstants.SomethingWrong;
                return View("Error");
            }

            var scopes = scope.Split(" ").ToList<string>();
            var scopeList = new List<ScopeDetails>() { };

            var attributesListInDb = await _userClaimService.ListUserClaimAsync();

            foreach (var key in scopes)
            {
                foreach (var obj in scopesListInDb)
                {
                    if (obj.Name == key)
                    {
                        var attributesList = obj.ClaimsList?.Split(' ', StringSplitOptions.RemoveEmptyEntries).
                            ToList() ?? new List<string>();

                        List<AttributeDetails> attributesDetailsList = new List<AttributeDetails>();

                        foreach (var attribute in attributesList)
                        {

                            var attributeObj = attributesListInDb.FirstOrDefault(a => a.Name == attribute);
                            if (attributeObj != null)
                            {
                                AttributeDetails attributeDetails = new AttributeDetails()
                                {
                                    displayName = attributeObj.DisplayName,
                                    name = attributeObj.Name,
                                    mandatory = attributeObj.DefaultClaim
                                };
                                attributesDetailsList.Add(attributeDetails);
                            }
                        }

                        scopeList.Add(new ScopeDetails
                        {
                            name = key,
                            displayName = obj.DisplayName,
                            description = obj.Description,
                            version = obj.Version,
                            attributes = attributesDetailsList
                        });
                    }
                }
            }
                        ;

            var consentModel = new ConsentViewModel
            {
                clientDetails = model,
                username = UserName,
                usermail = Email,
                suid = Suid,
                scopes = scope.Split(" ").ToList<string>(),
                scopesList = scopeList
            };

            _logger.LogDebug("<--- GetCode");
            return View("ConsentPage", consentModel);
        }

        [Route("Allow1")]
        [HttpPost]
        public async Task<IActionResult> Allow1(ConsentViewModel model)
        {
            try
            {
                _logger.LogDebug("--->Allow");
                List<approved_scopes> array = new List<approved_scopes>();

                var scopesList = await _scopeService.ListScopeAsync();

                if (scopesList == null)
                {
                    ViewBag.error = "Internal_Error";
                    ViewBag.error_description = "Failed to Get Scopes List";
                    return View("GetCodeError");
                }
                var GlobalSession = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "Session").Value;

                
                foreach (var element in model.scopes)
                {
                    var scope = scopesList
                        .FirstOrDefault(s => s.Name.Equals(element, StringComparison.OrdinalIgnoreCase));
                    if (scope == null)
                    {
                        ViewBag.error = "Internal_Error";
                        ViewBag.error_description = "Failed to Get Scopes";
                        return View("GetCodeError");
                    }
                    var Arrayobj = new approved_scopes()
                    {
                        scope = element,
                        permission = true,
                        version = scope.Version,
                        attributes = scope.ClaimsList?
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .ToList()
                            ?? new List<string>(),
                        created_date = DateTime.Now.ToString()
                    };
                    array.Add(Arrayobj);
                }
                var AllowedScopeDetailsList = new Scopes()
                {
                    approved_scopes = array
                };

                var data = new UserConsent
                {
                    ClientId = model.clientDetails.client_id,
                    Suid = model.suid,
                    Scopes = JsonConvert.SerializeObject(AllowedScopeDetailsList)
                };

                var response = await _userConsentService.ModifyUserConsent(data);
                if (response == null)
                {
                    _logger.LogError("Allow: Response value getting null ");
                    ViewBag.error = WEBConstants.InternalError;
                    ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_ALLOW_MODIFYUSERCONCENT_RES_NULL);
                    return View("GetCodeError");
                }
                if (response.Success)
                {
                    _logger.LogInformation("User Allowed scopes {0}",
                        string.Join(",", model.scopes));
                    return RedirectToAction("Getcode", model.clientDetails);
                }
                else
                {

                    if (!string.IsNullOrEmpty(model.clientDetails.redirect_uri))
                    {
                        _logger.LogError("Allow:{0}", response.Message);
                        _logger.LogDebug("<---Allow");
                        var url = model.clientDetails.redirect_uri + "?error=Internal_Error" +
                            "&error_description=" + response.Message +
                            "&state=" + model.clientDetails.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        _logger.LogError("Allow:{0}", response.Message);
                        _logger.LogDebug("<---Allow");
                        ViewBag.error = "Internal_Error";
                        ViewBag.error_description = response.Message;
                        return View("GetCodeError");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Allow:{0}", e.Message);
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_ALLOW_METHOD_EXCP);
                return View("GetCodeError");
            }
        }

        [Route("Deny")]
        [HttpPost]
        public async Task<IActionResult> Deny(ConsentViewModel model)
        {
            _logger.LogInformation("User denied scopes {0}", model.scopes.ToString());

            var GlobalSession = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == "Session").Value;

            var response = await _authenticationService.SendConsentDeniedLogMessage
                (GlobalSession, model.clientDetails.client_id);

            if (!string.IsNullOrEmpty(model.clientDetails.redirect_uri))
            {
                _logger.LogDebug("<---Deny");
                var url = model.clientDetails.redirect_uri + "?error=Access denied" +
                    "&error_description=" + WEBConstants.DeniedConsent + "&state="
                    + model.clientDetails.state;
                return LocalRedirect(url);
            }
            else
            {
                ViewBag.error = "Access denied";
                ViewBag.error_description = WEBConstants.DeniedConsent;
                return View("GetCodeError");
            }
        }

        [Route("ApproveScopes")]
        [HttpPost]
        public async Task<IActionResult> ApproveScopes(ConsentViewModel model)
        {
            try
            {
                var GlobalSession = HttpContext.User.Claims
                    .FirstOrDefault(c => c.Type == "Session").Value;

                List<ScopeDetail> scopeDetailList = new List<ScopeDetail>();

                var LogAttributesRequest = new LogAttributesRequest
                {
                    clientId = model.clientDetails.client_id,
                };
                foreach (var scope in model.scopesList)
                {
                    ScopeDetail scopeDetail = new ScopeDetail
                    {
                        Name = scope.name,
                        DisplayName = scope.displayName,
                    };

                    List<AttributeInfo> attributeDetailList = new List<AttributeInfo>();

                    foreach (var attribute in scope.attributes)
                    {
                        AttributeInfo attributeInfo = new AttributeInfo
                        {
                            Name = attribute.name,
                            DisplayName = attribute.displayName
                        };
                        attributeDetailList.Add(attributeInfo);
                    }
                }
                var response=await _authenticationService.LogAttributes
                    (GlobalSession,LogAttributesRequest);

                if (response == null)
                {
                    _logger.LogError("Allow: Response value getting null ");
                    ViewBag.error = WEBConstants.InternalError;
                    ViewBag.error_description = _helper.GetErrorMsg
                        (ErrorCodes.OAUTH2_ALLOW_MODIFYUSERCONCENT_RES_NULL);
                    return View("GetCodeError");
                }
                if (response.Success)
                {
                    _logger.LogInformation("User Allowed scopes {0}",
                        string.Join(",", model.scopes));
                    return RedirectToAction("Getcode", model.clientDetails);
                }
                else
                {

                    if (!string.IsNullOrEmpty(model.clientDetails.redirect_uri))
                    {
                        _logger.LogError("Allow:{0}", response.Message);
                        _logger.LogDebug("<---Allow");
                        var url = model.clientDetails.redirect_uri + "?error=Internal_Error" +
                            "&error_description=" + response.Message +
                            "&state=" + model.clientDetails.state;
                        return LocalRedirect(url);
                    }
                    else
                    {
                        _logger.LogError("Allow:{0}", response.Message);
                        _logger.LogDebug("<---Allow");
                        ViewBag.error = "Internal_Error";
                        ViewBag.error_description = response.Message;
                        return View("GetCodeError");
                    }
                }

            }
            catch (Exception e)
            {
                _logger.LogError("Allow:{0}", e.Message);
                ViewBag.error = WEBConstants.InternalError;
                ViewBag.error_description = _helper.GetErrorMsg(ErrorCodes.OAUTH2_ALLOW_METHOD_EXCP);
                return View("GetCodeError");
            }
        }

        [Route("Allow")]
        [HttpPost]
        public async Task<IActionResult> Allow(ConsentViewModel model)
        {
            if(string.IsNullOrEmpty(model.SelectedAttributesJson))
            {
                _logger.LogError("Allow: SelectedAttributesJson is null or empty");
                return View("GetCodeError");
            }

            var selectedAttributesMap = JsonConvert.DeserializeObject
                    <Dictionary<string, List<string>>>(model.SelectedAttributesJson);

            var attributeDictionary = await _userClaimService.
                GetAttributeNameDisplayNameAsync();

            if (attributeDictionary == null) 
            {
                return View("GetCodeError");
            }

            var scopesDictionary = await _scopeService.
                GetScopeNameDisplayNameAsync();

            if (scopesDictionary == null)
            { 
                return View("GetCodeError");
            }

            List<ProfileInfo> ScopeDetails = new List<ProfileInfo>();

            List<string> approvedAttributes = new List<string>();

            var GlobalSession = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == "Session").Value;

            foreach (var (scopeName, attributes) in selectedAttributesMap)
            {
                List<ClaimsDetail> attributeInfoList = new List<ClaimsDetail>();

                foreach (var attribute in attributes)
                {
                    approvedAttributes.Add(attribute);

                    ClaimsDetail attributeInfo = new ClaimsDetail
                    {
                        Name = attribute,
                        DisplayName = attributeDictionary.ContainsKey(attribute)
                        ? attributeDictionary[attribute] : attribute
                    };
                    attributeInfoList.Add(attributeInfo);
                }
                ProfileInfo scopeDetail = new ProfileInfo
                {
                    Name = scopeName,
                    DisplayName = scopesDictionary.ContainsKey(scopeName)
                    ? scopesDictionary[scopeName] : scopeName,
                    Attributes = attributeInfoList
                };
                ScopeDetails.Add(scopeDetail);
            }

            LogAttributesRequest LogAttributesRequest = new LogAttributesRequest
            {
                clientId = model.clientDetails.client_id,
                ScopeDetail = ScopeDetails
            };

            var response = await _authenticationService.LogAttributes
                (GlobalSession, LogAttributesRequest);

            if (response == null || !response.Success)
            {
                return View("GetCodeError");
            }

            return RedirectToAction("Getcode", model.clientDetails);
        }
    }
}