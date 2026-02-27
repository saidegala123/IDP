using DTPortal.Common;
using DTPortal.Core.Constants;
using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Models.RegistrationAuthority;
using DTPortal.Core.Domain.Repositories;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DTPortal.Core.DTOs;
using DTPortal.Core.Exceptions;
using System.IO;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using iTextSharp.text.pdf.qrcode;
using iTextSharp.text;
using System.Globalization;
using static System.Net.WebRequestMethods;
using static DTPortal.Common.CommonResponse;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
//using static System.Formats.Asn1.AsnWriter;

namespace DTPortal.Core.Services
{
    public class UserProfileService : IUserProfileService
    {
        // Initialize logger
        private readonly ILogger<UserInfoService> _logger;
        // Initialize Db
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheClient _cacheClient;
        //private readonly ITokenManager _tokenManager;
        private readonly idp_configuration idpConfiguration;
        private readonly IGlobalConfiguration _globalConfiguration;
        private readonly OpenIdConnect openidconnect;
        private readonly OIDCConstants OIDCConstants;
        private readonly IConfiguration configuration;
        private readonly IClientService _clientService;
        private readonly IPushNotificationClient _pushNotificationClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IClientsPurposeService _clientsPurposeService;
        private readonly IPurposeService _purposeService;
        //private readonly ITokenManagerService _tokenManagerService;
        private readonly ITransactionProfileRequestService _transactionProfileRequestService;
        private readonly ITransactionProfileConsentService _transactionProfileConsentService;
        private readonly ITransactionProfileStatusService _transactionProfileStatusService;
        private readonly IAttributeServiceTransactionsService _attributeServiceTransactionsService;
        private readonly IScopeService _scopeService;
        private readonly IHelper _helper;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserClaimService _userClaimService;
        private readonly IMessageLocalizer _messageLocalizer;   
        public UserProfileService(ILogger<UserInfoService> logger,
            IUnitOfWork unitofWork, ICacheClient cacheClient,
            //ITokenManager tokenManager,
            IGlobalConfiguration globalConfiguration, IConfiguration Configuration,
            IClientService clientService,
            IPushNotificationClient pushNotificationClient,
            IHttpClientFactory httpClientFactory,
            IClientsPurposeService clientsPurposeService,
            IPurposeService purposeService,
            //ITokenManagerService tokenManagerService,
            ITransactionProfileRequestService transactionProfileRequestService,
            ITransactionProfileConsentService transactionProfileConsentService,
            ITransactionProfileStatusService transactionProfileStatusService,
            IAttributeServiceTransactionsService attributeServiceTransactionsService,
            IScopeService scopeService,
            IHelper helper,
            IWebHostEnvironment environment,
            IEConsentService econsentService,
            IUserClaimService userClaimService,
            IMessageLocalizer messageLocalizer
            )
        {
            _logger = logger;
            _unitOfWork = unitofWork;
            _cacheClient = cacheClient;
            //_tokenManager = tokenManager;
            _globalConfiguration = globalConfiguration;
            configuration = Configuration;
            _clientService = clientService;
            _pushNotificationClient = pushNotificationClient;
            _httpClientFactory = httpClientFactory;
            _clientsPurposeService = clientsPurposeService;
            _purposeService = purposeService;
            //_tokenManagerService = tokenManagerService;
            _transactionProfileConsentService = transactionProfileConsentService;
            _transactionProfileRequestService = transactionProfileRequestService;
            _transactionProfileStatusService = transactionProfileStatusService;
            _attributeServiceTransactionsService = attributeServiceTransactionsService;
            _scopeService = scopeService;
            _helper = helper;
            _environment = environment;
            _userClaimService = userClaimService;
            _messageLocalizer = messageLocalizer;

            idpConfiguration = _globalConfiguration.GetIDPConfiguration();
            if (null == idpConfiguration)
            {
                _logger.LogError("Get IDP Configuration failed");
                throw new NullReferenceException();
            }

            var errorConfiguration = _globalConfiguration.GetErrorConfiguration();
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            OIDCConstants = errorConfiguration.OIDCConstants;
            if (null == errorConfiguration)
            {
                _logger.LogError("Get Error Configuration failed");
                throw new NullReferenceException();
            }

            openidconnect = JsonConvert.DeserializeObject<OpenIdConnect>
                (idpConfiguration.openidconnect.ToString());
            //_econsentService = econsentService;
        }
        public async Task<TransactionProfileRequestResponse> AddTransactionProfileRequest(string transactionId, int clientId, string Suid, string requestDetails)
        {
            try
            {
                TransactionProfileRequest transactionProfileRequest = new TransactionProfileRequest()
                {
                    TransactionId = transactionId,
                    ClientId = clientId,
                    Suid = Suid,
                    RequestDetails = requestDetails,
                    CreatedDate = DateTime.Now
                };
                var response = await _transactionProfileRequestService.AddProfileRequest(transactionProfileRequest);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("AddTransactionProfileRequest Failed   " + ex.Message);
                return null;
            }
        }
        public async Task<TransactionProfileConsentResponse> AddTransactionProfileConsent(int transactionId, string ConsentStatus, string ConsentDataSignature, string RequestedProfileAttributes, string approvedAttributes)
        {
            try
            {
                TransactionProfileConsent transactionProfileConsent = new TransactionProfileConsent()
                {
                    TransactionId = transactionId,
                    ConsentStatus = ConsentStatus,
                    ConsentDataSignature = ConsentDataSignature,
                    RequestedProfileAttributes = RequestedProfileAttributes,
                    CreatedDate = DateTime.Now,
                    ApprovedProfileAttributes = approvedAttributes
                };
                var response = await _transactionProfileConsentService.AddProfileConsent(transactionProfileConsent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Add transactionProfileConsent Failed   " + ex.Message);
                return null;
            }
        }
        public async Task<TransactionProfileStatusResponse> AddTransactionProfileStatus(int transactionId, string TransactionStatus, string FailedReason, string PivotSignedConsent, int DatapivotId)
        {
            try
            {
                TransactionProfileStatus transactionProfileStatus = new TransactionProfileStatus()
                {
                    TransactionId = transactionId,
                    TransactionStatus = TransactionStatus,
                    FailedReason = FailedReason,
                    PivotSignedConsent = PivotSignedConsent,
                    CreatedDate = DateTime.Now,
                    DatapivotId = DatapivotId
                };
                var response = await _transactionProfileStatusService.AddProfileStatus(transactionProfileStatus);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Add TransactionProfileStatus Failed   " + ex.Message);
                return null;
            }
        }
        public async Task<TransactionProfileConsentResponse> UpdateTransactionProfileConsent(int transactionId, string approvedAttributes, string ConsentStatus)
        {
            try
            {
                var transactionprofileConsent = new TransactionProfileConsent()
                {
                    TransactionId = transactionId,
                    UpdatedDate = DateTime.Now,
                    ConsentStatus = ConsentStatus,
                    ApprovedProfileAttributes = approvedAttributes
                };
                var response = await _transactionProfileConsentService.UpdateTransactionProfileConsent(transactionprofileConsent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Update TransactionProfileConsent Failed   " + ex.Message);
                return null;
            }
        }
        [NonAction]
        public bool ValidateScopes(string requestScopes, string clientScopes)
        {
            var Scopes = requestScopes.Split(new char[] { ' ', '\t' });
            var clientcopes = clientScopes.Split(new char[] { ' ', '\t' });
            var count = 0;
            foreach (var item in Scopes)
            {
                if (clientcopes.Contains(item))
                {
                    count++;
                }
            }
            if (count != Scopes.Length)
            {

                return false;
            }

            return true;
        }

        [NonAction]
        public bool ValidateProfiles(string requestScopes, string clientScopes)
        {
            var Scopes = requestScopes.Split(new char[] { ',', '\t' });
            var clientcopes = clientScopes.Split(new char[] { ',', '\t' });
            var count = 0;
            foreach (var item in Scopes)
            {
                if (clientcopes.Contains(item))
                {
                    count++;
                }
            }
            if (count != Scopes.Length)
            {

                return false;
            }

            return true;
        }

        public async Task<ServiceResult> GetNiraToken()
        {
            var client = new HttpClient();

            var url = "https://api.ugpass.go.ug/nira-api/login";

            client.DefaultRequestHeaders.Add("daes_authorization", "VUpneWQ3OEp9eVMvKV1WOkxKTEtoakBxZjllSlFrSA==");

            client.DefaultRequestHeaders.Add("identifier", "abcd");

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                APIResponse apiResponse = JsonConvert.DeserializeObject<APIResponse>(await response.Content.ReadAsStringAsync());
                if (apiResponse.Success)
                {
                    return new ServiceResult(true, apiResponse.Message, apiResponse.Result);
                }
                else
                {
                    _logger.LogError(apiResponse.Message);
                    return new ServiceResult(false, apiResponse.Message);
                }
            }
            else
            {
                _logger.LogError($"The request with uri={response.RequestMessage.RequestUri} failed " +
                   $"with status code={response.StatusCode}");
                return new ServiceResult(false, "Internal Error");
            }

        }
        public async Task<ServiceResult> GetUserDetailsNira(string userId)
        {
            var tokenResponse = await GetNiraToken();
            if (!tokenResponse.Success)
            {
                return tokenResponse;
            }
            var tokenResult = JsonConvert.DeserializeObject<TokenResponseDTO>(tokenResponse.Resource.ToString());

            var client = new HttpClient();

            var url = $"https://api.ugpass.go.ug/nira-api/profile/{userId}";

            client.DefaultRequestHeaders.Add("daes_authorization", "VUpneWQ3OEp9eVMvKV1WOkxKTEtoakBxZjllSlFrSA==");

            client.DefaultRequestHeaders.Add("identifier", "abcd");

            client.DefaultRequestHeaders.Add("access_token", tokenResult.access_token);

            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                APIResponse apiResponse = JsonConvert.DeserializeObject<APIResponse>(await response.Content.ReadAsStringAsync());
                if (apiResponse.Success)
                {
                    var niraResponse = JsonConvert.DeserializeObject<NiraResponseDTO>(apiResponse.Result.ToString());
                    return new ServiceResult(true, apiResponse.Message, niraResponse);
                }
                else
                {
                    _logger.LogError(apiResponse.Message);
                    return new ServiceResult(false, apiResponse.Message);
                }
            }
            else
            {
                _logger.LogError($"The request with uri={response.RequestMessage.RequestUri} failed " +
                  $"with status code={response.StatusCode}");
                return new ServiceResult(false, "Internal Error");
            }
        }
        public bool CheckPurposes(string requestPurposes, IEnumerable<Purpose> PurposeListinDb)
        {
            var requestPurposeList = requestPurposes.Split(new char[] { ',' });
            foreach (var purpose in requestPurposeList)
            {
                foreach (var purposes in PurposeListinDb)
                {
                    var purposeId = purposes.Id.ToString();
                    if (purposeId.Equals(purpose))
                    {
                        if (purposes.UserConsentRequired)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool ValidatePurposes(string requestPurposes, IEnumerable<string> clientPurposesinDb)
        {
            var purposes = requestPurposes.Split(new char[] { ',' });

            var count = 0;

            foreach (var purpose in purposes)
            {
                if (clientPurposesinDb.Contains(purpose))
                {
                    count++;
                }
            }
            if (purposes.Length == count)
            {
                return true;
            }
            return false;
        }

        public List<string> GetClaimsList(string[] scopesList, IEnumerable<Scope> scopesinDb)
        {
            List<string> claimsList = new List<string>();
            string[] claims;
            foreach (var scope in scopesList)
            {
                foreach (var scopes in scopesinDb)
                {
                    if (scopes.Name.Equals(scope))
                    {
                        if (scopes.IsClaimsPresent)
                        {
                            claims = scopes.ClaimsList.Split(new char[] { ' ', '\t' });
                            claimsList.AddRange(claims);
                        }
                    }
                }
            }
            return claimsList;
        }
        public async Task<string> GetSubscriberPhoto(string url)
        {
            _logger.LogDebug("-->GetSubscriberPhoto");
            string response = null;
            var errorMessage = string.Empty;

            if (string.IsNullOrEmpty(url))
            {
                _logger.LogError("Invalid Input Parameter");
                return response;
            }

            _logger.LogDebug("Photo Url: {0}", url);
            try
            {
                HttpClient client = _httpClientFactory.CreateClient();

                // Call the webservice with Get method
                var result = await client.GetAsync(url);

                // Check the status code
                if (result.IsSuccessStatusCode)
                {
                    // Read the response
                    byte[] content = await result.Content.ReadAsByteArrayAsync();
                    response = Convert.ToBase64String(content);
                    return response;
                }
                else
                {
                    _logger.LogError("GetSubscriberPhoto failed returned" +
                        ",Status code : {0}", result.StatusCode);
                    return null;
                }
            }
            catch (TimeoutException error)
            {
                _logger.LogError("GetSubscriberPhoto failed due to timeout exception: {0}",
                    error.Message);
                return null;
            }
            catch (Exception error)
            {
                _logger.LogError("GetSubscriberPhoto failed: {0}", error.Message);
                return null;
            }
        }
        public async Task<UserData> GetUserBasicDataAsync(
            string UserId, string UserIdType)
        {
            var raSubscriber = new SubscriberView();

            switch (UserIdType)
            {
                case "3":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoByEmail(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "4":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoByPhone(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "2":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfobyDocType(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "1":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfobyDocType(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "5":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoBySUID(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                default:
                    {
                        _logger.LogError("Incorrect input received");
                        return null;
                    };
            }

            DateTime? dob = null;
            if (DateTime.TryParse(raSubscriber.DateOfBirth, out DateTime parsed))
            {
                dob = parsed;
            }

            var userBasicProfile = new UserData()
            {
                CertificateStatus = raSubscriber.CertificateStatus,
                Country = raSubscriber.Country,
                DateOfBirth = dob,
                DisplayName = raSubscriber.DisplayName,
                Email = raSubscriber.Email,
                FcmToken = raSubscriber.FcmToken,
                Gender = raSubscriber.Gender,
                IdDocNumber = raSubscriber.IdDocNumber,
                Loa = raSubscriber.Loa,
                MobileNumber = raSubscriber.MobileNumber,
                SubscriberStatus = raSubscriber.SubscriberStatus,
                SubscriberUid = raSubscriber.SubscriberUid
            };

            if (raSubscriber.IdDocType == "1")
            {
                userBasicProfile.IdDocType = "National ID";
            }
            if (raSubscriber.IdDocType == "3")
            {
                userBasicProfile.IdDocType = "Passport";
            }
            try
            {
                var response = await GetSubscriberPhoto(raSubscriber.SelfieUri);
                if (null == response)
                {
                    _logger.LogError("Failed to get Subscriber Photo");
                    //return null;
                }
                userBasicProfile.Photo = response;
            }
            catch (Exception)
            {

            }

            return userBasicProfile;
        }
        public async Task<UserBasicProfile> GetUserBasicProfileAsync(
            string UserId, string UserIdType, ProfileConfig config)
        {
            var raSubscriber = new SubscriberView();

            switch (UserIdType)
            {
                case "EMAIL":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoByEmail(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "PHONE_NUMBER":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoByPhone(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "PASSPORT":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfobyDocType(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "CARD_NUMBER":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfobyDocType(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                case "SUID":
                    {
                        raSubscriber = await _unitOfWork.Subscriber.
                            GetSubscriberInfoBySUID(UserId);
                        if (null == raSubscriber)
                        {
                            _logger.LogError("Subscriber details not found");
                            return null;
                        }
                        break;
                    }
                default:
                    {
                        _logger.LogError("Incorrect input received");
                        return null;
                    };
            }

            DateTime? dob = null;

            if (DateTime.TryParse(raSubscriber.DateOfBirth, out DateTime parsedDob))
            {
                dob = parsedDob;
            }

            var userBasicProfile = new UserBasicProfile()
            {
                CertificateStatus = raSubscriber.CertificateStatus,
                Country = raSubscriber.Country,
                DateOfBirth = dob, // ✅ safely assigned after parsing
                DisplayName = raSubscriber.DisplayName,
                Email = raSubscriber.Email,
                FcmToken = raSubscriber.FcmToken,
                Gender = raSubscriber.Gender,
                IdDocNumber = raSubscriber.IdDocNumber,
                //IdDocType = raSubscriber.IdDocType,
                Loa = raSubscriber.Loa,
                MobileNumber = raSubscriber.MobileNumber,
                SubscriberStatus = raSubscriber.SubscriberStatus,
                SubscriberUid = raSubscriber.SubscriberUid
            };

            if (raSubscriber.IdDocType == "1")
            {
                userBasicProfile.IdDocType = "National ID";
            }
            if (raSubscriber.IdDocType == "3")
            {
                userBasicProfile.IdDocType = "Passport";
            }
            try
            {
                var response = await GetSubscriberPhoto(raSubscriber.SelfieUri);
                //if (null == response)
                //{
                //    _logger.LogError("Failed to get Subscriber Photo");
                //    return null;
                //}
                userBasicProfile.Photo = response;
            }
            catch (Exception)
            {

            }

            var subscriberCard = await _unitOfWork.SubscriberCardDetail.GetSubscriberCard(raSubscriber.SubscriberUid);

            if (subscriberCard != null)
            {
                userBasicProfile.SubscriberCard = subscriberCard.CardDocumnet;
            }
            else
            {
                var _client = new HttpClient();

                var url = configuration.GetValue<string>("APIServiceLocations:Simulation") + $"api/update/Card/{userBasicProfile.SubscriberUid}";

                var content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(url,content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    APIResponse apiResponse = JsonConvert.DeserializeObject<APIResponse>(await response.Content.ReadAsStringAsync());
                    if (apiResponse.Success)
                    {
                        userBasicProfile.SubscriberCard = apiResponse.Result.ToString();
                    }
                    else
                    {
                        userBasicProfile.SubscriberCard = null;
                    }
                }
                else
                {
                    userBasicProfile.SubscriberCard = null;
                }                
            }
            return userBasicProfile;
        }
        public async Task<GetUserProfileResponse> GetUserProfileDataAsync(
            GetUserProfileRequest request)
        {
            if (null == request || string.IsNullOrEmpty(request.UserId) ||
                string.IsNullOrEmpty(request.UserIdType))
            {
                _logger.LogError("Invalid Parameters Recieved");
                return new GetUserProfileResponse("Invalid Parameters");
            }
            HttpClient client = _httpClientFactory.CreateClient();
            ProfileConfig config = new ProfileConfig();
            UserBasicProfile userBasicProfile = new UserBasicProfile();
            UserMDLProfile userMDLProfile = new UserMDLProfile();
            UserHealthProfile userHealthProfile = new UserHealthProfile();
            UserSocialProfile userSocialProfile = new UserSocialProfile();

            TransactionProfileRequest transactionProfileRequest = new TransactionProfileRequest();
            TransactionProfileConsent transactionProfileConsent = new TransactionProfileConsent();
            TransactionProfileStatus transactionProfileStatus = new TransactionProfileStatus();

            TransactionProfileRequestResponse transactionProfileRequestResponse;
            TransactionProfileConsentResponse transactionProfileConsentResponse;
            TransactionProfileStatusResponse transactionProfileStatusResponse;

            int transactionid;

            var transactionId = Guid.NewGuid().ToString();

            string jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);


            transactionProfileRequest.TransactionId = transactionId;

            transactionProfileRequest.RequestDetails = jsonRequest;

            transactionProfileRequest.CreatedDate = DateTime.Now;

            transactionProfileStatus.CreatedDate = DateTime.Now;

            var clientinDb = await _clientService.GetClientByClientIdAsync(request.ClientId);
            if (null == clientinDb)
            {
                transactionProfileRequestResponse = await _transactionProfileRequestService.AddProfileRequest(transactionProfileRequest);

                transactionid = await _transactionProfileRequestService.GetIdByTransactionId(transactionId);
                if (transactionid == -1)
                {
                    return new GetUserProfileResponse("Failed to get transaction details");
                }
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Client not found", null, -1);
                return new GetUserProfileResponse("User not found");
            }

            transactionProfileRequest.ClientId = clientinDb.Id;

            userBasicProfile = await GetUserBasicProfileAsync(request.UserId,
                    request.UserIdType, config);

            if (null == userBasicProfile)
            {
                transactionProfileRequestResponse = await _transactionProfileRequestService.AddProfileRequest(transactionProfileRequest);
                transactionid = await _transactionProfileRequestService.GetIdByTransactionId(transactionId);
                if (transactionid == -1)
                {
                    return new GetUserProfileResponse("Failed to get transaction details");
                }
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "user not found", null, -1);
                _logger.LogError("GetUserBasicProfileAsync Failed");
                return new GetUserProfileResponse("User not found");
            }
            transactionProfileRequest.Suid = userBasicProfile.SubscriberUid;

            var response = await _transactionProfileRequestService.AddProfileRequest(transactionProfileRequest);

            if (response == null || !response.Success)
            {
                return new GetUserProfileResponse(response.Message);
            }

            transactionid = await _transactionProfileRequestService.GetIdByTransactionId(transactionId);
            if (transactionid == -1)
            {
                return new GetUserProfileResponse("Failed to get transaction details");
            }

            transactionProfileStatus.TransactionId = transactionid;
            transactionProfileStatus.CreatedDate = DateTime.Now;
            if (request.ProfileType == null ||
                request.ProfileType == "BASIC" ||
                request.ProfileType == "HEALTH" ||
                request.ProfileType == "MDL" ||
                request.ProfileType == "CREDITSCORE" ||
                request.ProfileType == "SOCIALPROFILE")
            {
                config.UserStatusUrl = configuration["Profile:Basic:UserStatusUrl"];
                config.ProfileUrl = configuration["Profile:Basic:ProfileServiceUrl"];

                //var daesClaims1 = new GetUserBasicProfileResult();
                //daesClaims1.suid = "Test";
                //    daesClaims1.name = null;
                //return new GetUserProfileResponse(daesClaims1);

                userBasicProfile = await GetUserBasicProfileAsync(request.UserId,
                    request.UserIdType, config);
                if (null == userBasicProfile)
                {

                    transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "GetUserBasicProfileAsync Failed", null, -1);
                    _logger.LogError("GetUserBasicProfileAsync Failed");
                    return new GetUserProfileResponse("User not found");
                }
            }
            else
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Invalid Profile Type", null, -1);
                _logger.LogError("Invalid Profile Type");
                return new GetUserProfileResponse("Invalid Profile Type");
            }
            if (request.ProfileType == "BASIC")
            {
                transactionProfileStatus.DatapivotId = 5;
            }

            if (request.ProfileType == "HEALTH")
            {
                config.UserStatusUrl = configuration["Profile:Health:UserStatusUrl"];
                config.ProfileUrl = configuration["Profile:Health:ProfileServiceUrl"];

                userHealthProfile.blood_group = "A+";
                userHealthProfile.height = 182;
                userHealthProfile.weight = 80;
                userHealthProfile.bmi = 24.2;

            }

            if (userBasicProfile.SubscriberStatus != StatusConstants.ACTIVE)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "User Account is not Active", null, -1);
                _logger.LogInformation("User account is not Active");
                return new GetUserProfileResponse("User Account is not Active");
            }

            var Purpose = request.Purpose;

            var PurposeConsentRequired = true;

            var PurposeinDb = await _clientsPurposeService.GetPurposeByClientId(request.ClientId);

            var purposesListinDb = await _purposeService.GetPurposeListAsync();

            if (null == purposesListinDb)
            {
                transactionProfileStatus.UpdatedDate = DateTime.Now;
                transactionProfileStatus.TransactionStatus = "FAILED";
                transactionProfileStatus.FailedReason = "Failed to get purpose list";
                transactionProfileStatus.DatapivotId = -1;
                transactionProfileStatusResponse = await _transactionProfileStatusService.AddProfileStatus(transactionProfileStatus);
                if (transactionProfileStatusResponse == null || !transactionProfileStatusResponse.Success)
                {
                    _logger.LogError("Adding transaction profile response failed");
                }
                return new GetUserProfileResponse("Failed to get the purposes list");
            }
            if (PurposeConsentRequired)
            {
                if (!string.IsNullOrEmpty(Purpose))
                {
                    if (!ValidatePurposes(Purpose, PurposeinDb))
                    {
                        transactionProfileStatus.UpdatedDate = DateTime.Now;
                        transactionProfileStatus.TransactionStatus = "FAILED";
                        transactionProfileStatus.FailedReason = "request purposes not matched with client purposes";
                        transactionProfileStatus.DatapivotId = -1;
                        transactionProfileStatusResponse = await _transactionProfileStatusService.AddProfileStatus(transactionProfileStatus);
                        if (transactionProfileStatusResponse == null || !transactionProfileStatusResponse.Success)
                        {
                            _logger.LogError("Adding transaction profile response failed");
                        }
                        return new GetUserProfileResponse("Invalid Request");
                    }
                    PurposeConsentRequired = CheckPurposes(request.Purpose, purposesListinDb);
                }
            }

            var tempAuthNSessId = string.Empty;

            try
            {
                // Generate sessionid
                tempAuthNSessId = EncryptionLibrary.KeyGenerator.GetUniqueKey();
            }
            catch (Exception error)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Get Unique Key failed", null, -1);
                _logger.LogError("GetUniqueKey failed: {0}", error.Message);
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }

            var clientInDb = await _clientService.GetClientByClientIdAsync(
                request.ClientId);
            if (null == clientInDb)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Failed to get client Details", null, -1);
                _logger.LogError(OIDCConstants.ClientNotFound.En);
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.ClientNotFound));
            }

            // Validate client scopes
            var isTrue = ValidateScopes(request.Scopes, clientInDb.Scopes);
            if (false == isTrue)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Scopes not matched", null, -1);
                _logger.LogError(OIDCConstants.ClientScopesNotMatched.En);
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.ClientScopesNotMatched));
            }

            // Requested Scopes
            var Scopes = request.Scopes.Split(new char[] { ' ', '\t' });
            if (Scopes.Contains("kycprofile"))
            {
                var kycurl = configuration["KycUrl"];
                kycurl += userBasicProfile.IdDocNumber;
                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(kycurl);
                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogError($"Request to {kycurl} failed with status code {result.StatusCode}");
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "Failed", "Failed to connect Datapivot", "", -1);
                        return new GetUserProfileResponse("Internal error");
                    }
                }
                catch (Exception)
                {

                    _logger.LogError("Failed to connect Datapivot");
                    transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "Failed", "Failed to connect to datapivot", "", -1);
                    return new GetUserProfileResponse("Internal error");
                }

                var responseString = await result.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<KycResponse>(responseString);
                if (apiResponse == null)
                {
                    return new GetUserProfileResponse("Internal error");
                }
                if (!apiResponse.success)
                {
                    return new GetUserProfileResponse(apiResponse.message);
                }
                userBasicProfile.KycDocument = apiResponse.result;
            }
            if (Scopes.Contains("ekycprofile"))
            {
                var kycurl = configuration["EKycUrl"];

                kycurl += userBasicProfile.IdDocNumber;

                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(kycurl);
                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogError($"Request to {kycurl} failed with status code {result.StatusCode}");
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "Failed", "Failed to connect Datapivot", "", -1);
                        return new GetUserProfileResponse("Internal error");
                    }
                }

                catch (Exception)
                {

                    _logger.LogError("Failed to connect Datapivot");
                    transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "Failed", "Failed to connect to datapivot", "", -1);
                    return new GetUserProfileResponse("Internal error");
                }

                var responseString = await result.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<KycResponse>(responseString);
                if (apiResponse == null)
                {
                    return new GetUserProfileResponse("Internal error");
                }
                if (!apiResponse.success)
                {
                    return new GetUserProfileResponse(apiResponse.message);
                }
                userBasicProfile.EKycDocument = apiResponse.result;
            }
            // Get all scopes from Db
            var scopesinDB = await _unitOfWork.Scopes.ListAllScopeAsync();
            if (null == scopesinDB)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Failed to get Scopes from DB", null, -1);
                _logger.LogError("ListAllScopeAsync failed");
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }

            // Get all Claims from Database
            var claimsinDb = await _unitOfWork.UserClaims.
                ListAllUserClaimAsync();
            if (null == claimsinDb)
            {
                transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Failed to get Claims from DB", null, -1);
                _logger.LogError("ListAllUserClaimAsync failed");
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }

            //var deselectScopesAndClaims = (bool)clientInDb.DeselectScopesClaims;
            var deselectScopesAndClaims = true;
            var consentScopes = new List<ScopeInfo>() { };

            var daesClaim = new GetUserBasicProfileResult();

            var userApprovedClaims = new List<string>() { };

            var scopeConsentRequired = false;

            HashSet<string> requestedProfileAttributes = new HashSet<string>();

            if (!PurposeConsentRequired)
            {
                userApprovedClaims = GetClaimsList(Scopes, scopesinDB);
            }
            else
            {
                // Get all requested scopes details
                foreach (var reqScope in Scopes)
                {
                    foreach (var scopeinDb in scopesinDB)
                    {
                        if ((reqScope == scopeinDb.Name) &&
                            (scopeinDb.UserConsent == true))
                        {
                            scopeConsentRequired = true;
                            ScopeInfo scopeInfo = new ScopeInfo();
                            scopeInfo.Name = scopeinDb.Name;
                            scopeInfo.DisplayName = scopeinDb.DisplayName;
                            scopeInfo.Description = scopeinDb.Description;

                            if (deselectScopesAndClaims)
                                scopeInfo.Mandatory = scopeinDb.DefaultScope;
                            else
                                scopeInfo.Mandatory = true;

                            if (scopeinDb.IsClaimsPresent)
                            {
                                scopeInfo.ClaimsInfo = new List<ClaimInfo>() { };
                                // Parse space seperated claims into list
                                var claims = scopeinDb.ClaimsList.Split(
                                    new char[] { ' ', '\t' });

                                foreach (var claim in claims)
                                {
                                    foreach (var claiminDb in claimsinDb)
                                    {
                                        if ((claim == claiminDb.Name) &&
                                            (claiminDb.UserConsent == true))
                                        {
                                            scopeConsentRequired = true;
                                            ClaimInfo claimInfo = new ClaimInfo();
                                            claimInfo.Name = claiminDb.Name;
                                            claimInfo.DisplayName = claiminDb.DisplayName;
                                            claimInfo.Description = claiminDb.Description;

                                            if (deselectScopesAndClaims)
                                                claimInfo.Mandatory = claiminDb.DefaultClaim;
                                            else
                                                claimInfo.Mandatory = true;

                                            scopeInfo.ClaimsInfo.Add(claimInfo);
                                            requestedProfileAttributes.Add(claiminDb.DisplayName);
                                        }
                                    }
                                }
                            }
                            scopeInfo.ClaimsPresent = scopeinDb.IsClaimsPresent;
                            consentScopes.Add(scopeInfo);
                        }
                    }
                }
                if (scopeConsentRequired)
                {
                    ClientDetails clientdetails = new ClientDetails();
                    clientdetails.AppName = clientInDb.ApplicationName;
                    clientdetails.ClientId = clientInDb.ClientId;

                    var authScheme = configuration["Consent_Authentication"];

                    // Prepare temporary session object
                    TemporarySession temporarySession = new TemporarySession
                    {
                        TemporarySessionId = tempAuthNSessId,
                        UserId = userBasicProfile.SubscriberUid,
                        DisplayName = userBasicProfile.DisplayName,
                        PrimaryAuthNSchemeList = new List<string> { authScheme },
                        AuthNSuccessList = new List<string>(),
                        Clientdetails = clientdetails,
                        IpAddress = string.Empty,
                        UserAgentDetails = string.Empty,
                        TypeOfDevice = string.Empty,
                        MacAddress = DTInternalConstants.NOT_AVAILABLE,
                        withPkce = false,
                        AdditionalValue = DTInternalConstants.pending,
                        AllAuthNDone = false,
                        //TransactionId=transactionId,
                        AuthNStartTime = DateTime.Now.ToString("s"),
                        CoRelationId = Guid.NewGuid().ToString()
                    };

                    // Create temporary session
                    var task = await _cacheClient.Add(CacheNames.TemporarySession,
                        tempAuthNSessId, temporarySession);
                    if (DTInternalConstants.S_OK != task.retValue)
                    {
                        _logger.LogError("_cacheClient.Add failed");
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Failed to Add Record in DB", null, -1);
                        return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
                    }

                    transactionProfileConsent.ConsentStatus = "PENDING";

                    transactionProfileConsent.CreatedDate = DateTime.Now;

                    transactionProfileConsent.RequestedProfileAttributes = string.Join(",", requestedProfileAttributes);

                    transactionProfileConsent.TransactionId = transactionid;

                    var Consentresult = await _transactionProfileConsentService.AddProfileConsent(transactionProfileConsent);

                    if (null == Consentresult || !Consentresult.Success)
                    {
                        return new GetUserProfileResponse("Failed to add Consent Status");
                    }
                    // Send notification to mobile
                    var eConsentNotification = new EConsentNotification()
                    {
                        AuthnScheme = authScheme,
                        AuthnToken = tempAuthNSessId,
                        RegistrationToken = userBasicProfile.FcmToken,
                        ApplicationName = clientInDb.ApplicationName,
                        ConsentScopes = consentScopes,
                        DeselectScopesAndClaims = deselectScopesAndClaims
                    };

                    try
                    {
                        var result = await _pushNotificationClient.SendEConsentNotification(
                            eConsentNotification);
                        if (null == result)
                        {
                            transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Failed to send Notification", null, -1);
                            _logger.LogError("_pushNotificationClient.SendAuthnNotification" +
                                " failed");
                            return new GetUserProfileResponse("Failed to send Notification");
                        }
                    }
                    catch (Exception error)
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Error sending Notification", null, -1);
                        _logger.LogError("_pushNotificationClient." +
                            "SendAuthnNotification " +
                            "failed : {0}", error.Message);
                        return new GetUserProfileResponse("Failed to send Notification");
                    }

                    DateTime date1 = DateTime.Now;
                    DateTime date2 = DateTime.Now.AddMinutes(1);
                    bool isError = false;
                    string message = string.Empty;
                    TemporarySession tempSession = null;

                    while (0 > DateTime.Compare(date1, date2))
                    {
                        // Check whether the temporary session exists
                        var isExists = await _cacheClient.Exists(CacheNames.TemporarySession,
                            tempAuthNSessId);
                        if (CacheCodes.KeyExist != isExists.retValue)
                        {
                            _logger.LogError("Temporary Session Expired/Not Found");
                            message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                            isError = true;
                            break;
                        }

                        // Get the temporary session object
                        tempSession = await _cacheClient.Get<TemporarySession>
                            (CacheNames.TemporarySession,
                            tempAuthNSessId);
                        if (null == tempSession)
                        {
                            _logger.LogError("Get Temporary Session Failed,Expired/Not Found");
                            message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                            isError = true;
                            break;
                        }

                        if (!tempSession.AdditionalValue.Equals(DTInternalConstants.pending))
                            break;

                        // Wait 500 Milli Seconds(Half Second) without blocking
                        await Task.Delay(500);
                        date1 = DateTime.Now;
                    }

                    if (isError)
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", message, null, -1);
                        return new GetUserProfileResponse(message);
                    }
                    if (tempSession.AdditionalValue.Equals(DTInternalConstants.pending))
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Request TimeOut", null, -1);
                        //transactionProfileConsentResponse = await UpdateTransactionProfileConsent(transactionid, "", "PENDING");
                        return new GetUserProfileResponse("Request TimeOut");
                    }
                    if (tempSession.AdditionalValue.Equals(DTInternalConstants.FailedStatus))
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "NA", null, -1);
                        //transactionProfileConsentResponse = await UpdateTransactionProfileConsent(transactionid, "", "Request Iimeout");

                        return new GetUserProfileResponse("Request TimeOut");
                    }
                    if (tempSession.AdditionalValue.Equals(DTInternalConstants.deny))
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Consent Denied", null, -1);
                        //transactionProfileConsentResponse = await UpdateTransactionProfileConsent(transactionid, "", "Consent Denied");
                        return new GetUserProfileResponse("Access Denied");
                    }
                    if (tempSession.AdditionalValue.Equals("Wrong Pin"))
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Wrong Pin", null, -1);
                        //transactionProfileConsentResponse = await UpdateTransactionProfileConsent(transactionid, "", "Wrong pin");
                        return new GetUserProfileResponse("Wrong pin");
                    }
                    if (tempSession.AdditionalValue.Equals("Subscriber Denied Authentication"))
                    {
                        transactionProfileStatusResponse = await AddTransactionProfileStatus(transactionid, "FAILED", "Subscriber Denied Authentication", null, -1);

                        //transactionProfileConsentResponse = await UpdateTransactionProfileConsent(transactionid, "", "Subscriber Denied Authentication");
                        return new GetUserProfileResponse("Access Denied");
                    }
                    // Get all requested scopes details
                    foreach (var reqScope in Scopes)
                    {
                        foreach (var approvedScope in tempSession.allowedScopesAndClaims)
                        {
                            if ((reqScope == approvedScope.name) &&
                                approvedScope.claimsPresent)
                            {
                                userApprovedClaims.AddRange(approvedScope.claims);
                            }
                        }
                    }
                    HashSet<string> ApprovedProfileSet = new HashSet<string>(userApprovedClaims);
                    HashSet<string> ApprovedClaims = new HashSet<string>();
                    foreach (var item in consentScopes)
                    {
                        foreach (var claims in item.ClaimsInfo)
                        {
                            if (userApprovedClaims.Contains(claims.Name))
                            {
                                ApprovedClaims.Add(claims.DisplayName);
                            }
                        }
                    }
                    transactionProfileConsent.ApprovedProfileAttributes = string.Join(",", ApprovedClaims);
                    transactionProfileConsent.UpdatedDate = DateTime.Now;
                    transactionProfileConsent.ConsentStatus = "APPROVED";
                    transactionProfileConsentResponse = await _transactionProfileConsentService.UpdateTransactionProfileConsent(transactionProfileConsent);
                }
            }

            var daesClaims = new GetUserBasicProfileResult();
            if (userApprovedClaims.Contains(Claims.SUID))
                daesClaims.suid = userBasicProfile.SubscriberUid;
            if (userApprovedClaims.Contains(Claims.Name))
                daesClaims.name = userBasicProfile.DisplayName;
            if (userApprovedClaims.Contains(Claims.BirthDate))
                daesClaims.birthdate = userBasicProfile.DateOfBirth.ToString();
            if (userApprovedClaims.Contains(Claims.Gender))
                daesClaims.gender = userBasicProfile.Gender;

            if (userApprovedClaims.Contains(Claims.Email))
                daesClaims.email = userBasicProfile.Email;
            if (userApprovedClaims.Contains(Claims.PhoneNumber))
                daesClaims.phone_number = userBasicProfile.MobileNumber;
            if (userApprovedClaims.Contains(Claims.LOA))
                daesClaims.loa = userBasicProfile.Loa;
            if (userApprovedClaims.Contains(Claims.IdDocumentNumber))
                daesClaims.id_document_number = userBasicProfile.IdDocNumber;
            if (userApprovedClaims.Contains(Claims.IdDocumentType))
                daesClaims.id_document_type = userBasicProfile.IdDocType;
            if (userApprovedClaims.Contains(Claims.Country))
                daesClaims.country = userBasicProfile.Country;
            if (userApprovedClaims.Contains(Claims.Email_Verified))
                daesClaims.email_verified = true;
            if (userApprovedClaims.Contains(Claims.PhoneNumber_Verified))
                daesClaims.phone_number_verified = true;
            if (userApprovedClaims.Contains("photo"))
                daesClaims.photo = userBasicProfile.Photo;
            if (userApprovedClaims.Contains("ekycdocument"))
                daesClaims.Ekyc_document = userBasicProfile.EKycDocument;
            if (userApprovedClaims.Contains("kycdocument"))
                daesClaims.kyc_document = userBasicProfile.KycDocument;

            if (userApprovedClaims.Contains("issue_date"))
                daesClaims.issue_date = userMDLProfile.issue_date;
            if (userApprovedClaims.Contains("expiry_date"))
                daesClaims.expiry_date = userMDLProfile.expiry_date;
            if (userApprovedClaims.Contains("issuing_country"))
                daesClaims.issuing_country = userMDLProfile.issuing_country;
            if (userApprovedClaims.Contains("issuing_authority"))
                daesClaims.issuing_authority = userMDLProfile.issuing_authority;
            if (userApprovedClaims.Contains("document_number"))
                daesClaims.document_number = userMDLProfile.document_number;

            if (userApprovedClaims.Contains("blood_group"))
                daesClaims.blood_group = userHealthProfile.blood_group;
            if (userApprovedClaims.Contains("height"))
                daesClaims.height = userHealthProfile.height;
            if (userApprovedClaims.Contains("weight"))
                daesClaims.weight = userHealthProfile.weight;
            if (userApprovedClaims.Contains("bmi"))
                daesClaims.bmi = userHealthProfile.bmi;
            if (request.ProfileType == "SOCIALPROFILE")
            {
                if (userApprovedClaims.Contains("address"))
                    daesClaims.address = userSocialProfile.address;
                if (userApprovedClaims.Contains("age"))
                    daesClaims.age = userSocialProfile.age;
                if (userApprovedClaims.Contains("birthdate"))
                    daesClaims.birthdate = userSocialProfile.birthdate;
                if (userApprovedClaims.Contains("email"))
                    daesClaims.email = userSocialProfile.email;
                if (userApprovedClaims.Contains("income"))
                    daesClaims.income = userSocialProfile.income;
                if (userApprovedClaims.Contains("x_cst_indv_Doc_type"))
                    daesClaims.x_cst_indv_Doc_type = userSocialProfile.x_cst_indv_Doc_type;
                if (userApprovedClaims.Contains("x_cst_indv_Doc_value"))
                    daesClaims.x_cst_indv_Doc_value = userSocialProfile.x_cst_indv_Doc_value;
                if (userApprovedClaims.Contains("x_cst_indv_children"))
                    daesClaims.x_cst_indv_children = userSocialProfile.x_cst_indv_children;
                if (userApprovedClaims.Contains("x_cst_indv_suid"))
                    daesClaims.x_cst_indv_suid = userSocialProfile.x_cst_indv_suid;
                if (userApprovedClaims.Contains("name"))
                    daesClaims.name = userSocialProfile.name;
                if (userApprovedClaims.Contains("phone_sanitized"))
                    daesClaims.phone_number = userSocialProfile.phone_sanitized;
                if (userApprovedClaims.Contains("occupation"))
                    daesClaims.occupation = userSocialProfile.occupation;
                if (userApprovedClaims.Contains("programs"))
                    daesClaims.programs = userSocialProfile.Programs;
                if (userApprovedClaims.Contains(Claims.SUID))
                {
                    daesClaims.suid = userSocialProfile.x_cst_indv_suid;
                }
            }

            /*if (userApprovedClaims.Contains("Creditscore"))
                daesClaims.Creditscore = creditScoreDetail.Creditscore;
            if (userApprovedClaims.Contains("JobTitle"))
                daesClaims.JobTitle = creditScoreDetail.JobTitle;
            if (userApprovedClaims.Contains("IncomeLevel"))
                daesClaims.IncomeLevel = creditScoreDetail.IncomeLevel;
            if (userApprovedClaims.Contains(Claims.Email))
                daesClaims.email = creditScoreDetail.Email;
            if (userApprovedClaims.Contains(Claims.PhoneNumber))
                daesClaims.phone_number = creditScoreDetail.PhoneNo;
            if (userApprovedClaims.Contains(Claims.Name))
                daesClaims.name = creditScoreDetail.Name;*/

            transactionProfileStatus.UpdatedDate = DateTime.Now;
            transactionProfileStatus.TransactionStatus = "SUCCESS";
            var transactionProfileResponse = await _transactionProfileStatusService.AddProfileStatus(transactionProfileStatus);
            if (transactionProfileResponse == null || !transactionProfileResponse.Success)
            {
                _logger.LogError("Adding transaction profile response failed");
            }
            _logger.LogDebug("<--GetUserProfileDataAsync");
            return new GetUserProfileResponse(daesClaims);
        }

        public async Task<GetUserProfileResponse> GetUserProfileDataAsync1(
           GetUserProfileRequest request)
        {
            if (null == request || string.IsNullOrEmpty(request.UserId) ||
                string.IsNullOrEmpty(request.UserIdType))
            {
                _logger.LogError("Invalid Parameters Recieved");
                return new GetUserProfileResponse("Invalid Parameters");
            }

            ProfileConfig config = new ProfileConfig();
            UserBasicProfile userBasicProfile = new UserBasicProfile();
            UserMDLProfile userMDLProfile = new UserMDLProfile();
            UserHealthProfile userHealthProfile = new UserHealthProfile();

            if (request.ProfileType == null ||
                request.ProfileType == "BASIC" ||
                request.ProfileType == "HEALTH" ||
                request.ProfileType == "MDL")
            {
                config.UserStatusUrl = configuration["Profile:Basic:UserStatusUrl"];
                config.ProfileUrl = configuration["Profile:Basic:ProfileServiceUrl"];

                //var daesClaims1 = new GetUserBasicProfileResult();
                //daesClaims1.suid = "Test";
                //    daesClaims1.name = null;
                //return new GetUserProfileResponse(daesClaims1);

                userBasicProfile = await GetUserBasicProfileAsync(request.UserId,
                    request.UserIdType, config);
                if (null == userBasicProfile)
                {
                    _logger.LogError("GetUserBasicProfileAsync Failed");
                    return new GetUserProfileResponse("User not found");
                }
            }
            else
            {
                _logger.LogError("Invalid Profile Type");
                return new GetUserProfileResponse("Invalid Profile Type");
            }

            if (request.ProfileType == "MDL")
            {
                config.UserStatusUrl = configuration["Profile:Health:UserStatusUrl"];
                config.ProfileUrl = configuration["Profile:Health:ProfileServiceUrl"];

                userMDLProfile.document_number = "KL0520140009308";
                userMDLProfile.issue_date = "2021-04-19T22:00:00Z";
                userMDLProfile.issuing_country = "IN";
                userMDLProfile.issuing_authority = "IN-TG";
                userMDLProfile.expiry_date = "2024-04-19T22:00:00Z";
            }
            if (request.ProfileType == "HEALTH")
            {
                config.UserStatusUrl = configuration["Profile:Health:UserStatusUrl"];
                config.ProfileUrl = configuration["Profile:Health:ProfileServiceUrl"];

                userHealthProfile.blood_group = "A+";
                userHealthProfile.height = 182;
                userHealthProfile.weight = 80;
                userHealthProfile.bmi = 24.2;

            }

            if (userBasicProfile.SubscriberStatus != StatusConstants.ACTIVE)
            {
                _logger.LogInformation("User account is not Active");
                return new GetUserProfileResponse("User Account is not Active");
            }


            var clientInDb = await _clientService.GetClientByClientIdAsync(
                request.ClientId);
            if (null == clientInDb)
            {
                _logger.LogError(OIDCConstants.ClientNotFound.En);
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.ClientNotFound));
            }

            // Validate client scopes
            var isTrue = ValidateScopes(request.Scopes, clientInDb.Scopes);
            if (false == isTrue)
            {
                _logger.LogError(OIDCConstants.ClientScopesNotMatched.En);
                return new GetUserProfileResponse(_messageLocalizer.GetMessage(OIDCConstants.ClientScopesNotMatched));
            }


            var daesClaims = new GetUserBasicProfileResult();

            daesClaims.suid = userBasicProfile.SubscriberUid;
            daesClaims.name = userBasicProfile.DisplayName;
            daesClaims.birthdate = userBasicProfile.DateOfBirth.ToString();

            daesClaims.gender = userBasicProfile.Gender;

            daesClaims.email = userBasicProfile.Email;
            daesClaims.phone_number = userBasicProfile.MobileNumber;
            daesClaims.loa = userBasicProfile.Loa;
            daesClaims.id_document_number = userBasicProfile.IdDocNumber;
            daesClaims.id_document_type = userBasicProfile.IdDocType;
            daesClaims.country = userBasicProfile.Country;
            daesClaims.email_verified = true;
            daesClaims.phone_number_verified = true;
            daesClaims.photo = userBasicProfile.Photo;

            if (!string.IsNullOrEmpty(userMDLProfile.issue_date))
                daesClaims.issue_date = userMDLProfile.issue_date;

            if (!string.IsNullOrEmpty(userMDLProfile.expiry_date))
                daesClaims.expiry_date = userMDLProfile.expiry_date;
            if (!string.IsNullOrEmpty(userMDLProfile.issuing_country))
                daesClaims.issuing_country = userMDLProfile.issuing_country;
            if (!string.IsNullOrEmpty(userMDLProfile.issuing_authority))
                daesClaims.issuing_authority = userMDLProfile.issuing_authority;
            if (!string.IsNullOrEmpty(userMDLProfile.document_number))
                daesClaims.document_number = userMDLProfile.document_number;

            if (!string.IsNullOrEmpty(userHealthProfile.blood_group))
                daesClaims.blood_group = userHealthProfile.blood_group;
            if (userHealthProfile.height != 0)
                daesClaims.height = userHealthProfile.height;
            if (userHealthProfile.weight != 0)
                daesClaims.weight = userHealthProfile.weight;
            if (userHealthProfile.bmi != 0.0)
                daesClaims.bmi = userHealthProfile.bmi;

            _logger.LogDebug("<--GetUserProfileDataAsync");
            return new GetUserProfileResponse(daesClaims);
        }

        public async Task<APIResponse> GetUserProfileDataNewAsync(
            GetUserProfileRequest request)
        {
            if (null == request || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.UserIdType))
            {
                _logger.LogError("Invalid Parameters Received");

                return new APIResponse("Invalid Parameters");
            }

            ProfileConfig config = new ProfileConfig();

            UserData userBasicProfile = new UserData();

            Accesstoken accessToken = null;

            var reqScopes = request.ProfileType.Split(',');

            try
            {
                accessToken = await _cacheClient.Get<Accesstoken>("AccessToken",
                    request.Token);
                if (null == accessToken)
                {
                    _logger.LogError("Access token not recieved from cache." +
                        "Expired or Invalid access token");
                    return new APIResponse("UnAuthorized");
                }
            }
            catch (CacheException ex)
            {
                _logger.LogError("Failed to get Access Token Record");
                ErrorResponseDTO error = new ErrorResponseDTO();
                error.error = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                error.error_description = _helper.GetRedisErrorMsg(
                    ex.ErrorCode, ErrorCodes.REDIS_ACCESS_TOKEN_GET_FAILED);
                return new APIResponse("Internal Error" + error.error_description);
            }

            userBasicProfile = await GetUserBasicDataAsync(request.UserId,
                        request.UserIdType);

            if (null == userBasicProfile)
            {
                _logger.LogError("GetUserBasicProfileAsync Failed");

                return new APIResponse("User not found");
            }

            var clientInDb = await _clientService.GetClientProfilesAndPurposesAsync(accessToken.ClientId);

            if (null == clientInDb)
            {
                _logger.LogError("Client not found");

                return new APIResponse("Client not found");
            }
            var eConsentClient = clientInDb.EConsentClients.FirstOrDefault(s => s.Status == "ACTIVE");

            if (null == eConsentClient)
            {
                _logger.LogError("EConsent Client not found");

                return new APIResponse("EConsent Client not found");
            }
            var transactionUid = Guid.NewGuid().ToString();

            var suid = userBasicProfile.SubscriberUid;

            List<string> scopesList = new List<string>();

            HashSet<string> requestedAttributes = new HashSet<string>();

            string[] Scopes = new string[1];

            if (!string.IsNullOrEmpty(request.ProfileType))
            {
                Scopes = request.ProfileType.Split(new char[] { ',', '\t' });

                foreach (var scope in Scopes)
                {
                    var profileId = await _scopeService.GetScopeIdByNameAsync(scope);

                    if (profileId == -1)
                    {
                        _logger.LogError("Profile not found");

                        return new APIResponse("Profile not found");
                    }
                    scopesList.Add(profileId.ToString());
                }

                request.ProfileType = string.Join(',', scopesList);
            }
            else
            {
                _logger.LogError("Empty Profiles");

                return new APIResponse("Invalid Profiles");
            }
            Scopes=scopesList.ToArray();
            if (!string.IsNullOrEmpty(request.Purpose))
            {
                var purposeId = await _purposeService.GetPurposeIdByNameAsync(request.Purpose);
                if (purposeId == -1)
                {
                    _logger.LogError("Purpose not found");

                    return new APIResponse("Purpose not found");
                }
                request.Purpose = purposeId.ToString();
            }

            var transactionResponse = await AddTransactionProfileRequest(transactionUid, clientInDb.Id, suid, JsonConvert.SerializeObject(request));

            if (!transactionResponse.Success)
            {
                _logger.LogError("Failed to Add Transactions");

                return new APIResponse("Internal Error");
            }

            var transactionId = transactionResponse.Result.Id;

            var profilesList = await _userClaimService.GetAttributes();

            foreach (var scope in scopesList)
            {
                var scopeinDb = await _unitOfWork.Scopes.GetByIdAsync(int.Parse(scope));

                if (scopeinDb != null)
                {
                    if (!scopeinDb.IsClaimsPresent)
                    {
                        _logger.LogError("Profile has no Attributes");

                        return new APIResponse("Profile Has No Attributes");
                    }
                    var claims = scopeinDb.ClaimsList.Split(new char[] { ' ', '\t' });
                    foreach (var claim in claims)
                    {
                        requestedAttributes.Add(claim);
                    }
                }
            }

            await AddTransactionProfileConsent(transactionId, "NA", "", string.Join(",", requestedAttributes), "");

            if (userBasicProfile.SubscriberStatus != StatusConstants.ACTIVE)
            {
                _logger.LogError("User account is not Active");

                await AddTransactionProfileStatus(transactionId, "FAILED", "User Account Not Active", "", -1);

                return new APIResponse("User Account is not Active");
            }

            var Purpose = request.Purpose;

            var PurposeConsentRequired = true;

            List<string> clientPurposes = new List<string>();

            if (eConsentClient.Purposes != null)
            {
                clientPurposes = eConsentClient.Purposes.Split(',').ToList();
            }

            var PurposesInDb = await _purposeService.GetPurposeListAsync();

            if (null == PurposesInDb)
            {
                _logger.LogError("Failed to get the purposes list");
                return new APIResponse("Failed to get the purposes list");
            }

            if (!string.IsNullOrEmpty(Purpose))
            {
                if (!ValidatePurposes(Purpose, clientPurposes))
                {
                    await AddTransactionProfileStatus(transactionId, "FAILED", "Invalid Purposes", "", -1);

                    return new APIResponse("Invalid Purpose");
                }
                PurposeConsentRequired = CheckPurposes(request.Purpose, PurposesInDb);
            }

            var tempAuthNSessId = string.Empty;

            try
            {
                tempAuthNSessId = EncryptionLibrary.KeyGenerator.GetUniqueKey();
            }
            catch (Exception error)
            {
                _logger.LogError("GetUniqueKey failed: {0}", error.Message);
                return new APIResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }

            // Validate client scopes

            var isTrue = ValidateProfiles(request.ProfileType, eConsentClient.Scopes);
            if (false == isTrue)
            {
                _logger.LogError(OIDCConstants.ClientScopesNotMatched.En);

                await AddTransactionProfileStatus(transactionId, "FAILED", _messageLocalizer.GetMessage(OIDCConstants.ClientScopesNotMatched), "", -1);

                return new APIResponse(_messageLocalizer.GetMessage(OIDCConstants.ClientScopesNotMatched));
            }

            // Requested Scopes


            // Get all scopes from Db
            var scopesinDB = await _unitOfWork.Scopes.ListAllScopeAsync();
            if (null == scopesinDB)
            {
                _logger.LogError("ListAllScopeAsync failed");

                await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);

                return new APIResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }
            var userProfileResponse = await GetProfile(reqScopes, userBasicProfile.IdDocNumber);

            if (!userProfileResponse.Success)
            {
                await AddTransactionProfileStatus(transactionId, "FAILED", userProfileResponse.Message, "", -1);
                return new APIResponse(userProfileResponse.Message);
            }

            var userProfile = (Dictionary<string, object>)userProfileResponse.Resource;
            // Get all Claims from Database
            var claimsinDb = await _unitOfWork.UserClaims.
                ListAllUserClaimAsync();
            if (null == claimsinDb)
            {
                _logger.LogError("ListAllUserClaimAsync failed");
                await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);
                return new APIResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }

            var deselectScopesAndClaims = true;

            var consentScopes = new List<ScopeInfo>() { };

            var daesClaim = new GetUserBasicProfileResult();

            var userApprovedClaims = new List<string>() { };

            HashSet<string> ApprovedClaims = new HashSet<string>();

            HashSet<string> requestedProfileAttributes = new HashSet<string>();

            var ProfilesSet = new Dictionary<string, UserProfilesConsent>();

            if (!PurposeConsentRequired)
            {
                userApprovedClaims = GetClaimsList(reqScopes, scopesinDB);
                foreach (var item in userApprovedClaims)
                {
                    ApprovedClaims.Add(item);
                }
            }
            else
            {
                foreach (var reqScope in Scopes)
                {
                    foreach (var scopeinDb in scopesinDB)
                    {
                        if (reqScope == scopeinDb.Id.ToString())
                        {
                            var userProfileConsent = await _unitOfWork.UserProfilesConsent.
                                GetUserProfilesConsentByProfileAsync(userBasicProfile.SubscriberUid, clientInDb.ClientId, scopeinDb.Id.ToString());

                            if (userProfileConsent != null)
                            {
                                ProfilesSet[scopeinDb.Name] = userProfileConsent;
                            }

                            ScopeInfo scopeInfo = new ScopeInfo();
                            scopeInfo.Name = scopeinDb.Name;
                            scopeInfo.DisplayName = scopeinDb.DisplayName;
                            scopeInfo.Description = scopeinDb.Description;
                            scopeInfo.Mandatory = scopeinDb.DefaultScope;

                            if (scopeinDb.IsClaimsPresent)
                            {
                                scopeInfo.ClaimsInfo = new List<ClaimInfo>() { };

                                var claims = scopeinDb.ClaimsList.Split(
                                    new char[] { ' ', '\t' });

                                List<Attributes> attributesList = new List<Attributes>();

                                if (userProfileConsent != null)
                                {
                                    attributesList = JsonConvert.DeserializeObject<List<Attributes>>(userProfileConsent.Attributes);
                                }
                                foreach (var claim in claims)
                                {
                                    foreach (var claiminDb in claimsinDb)
                                    {
                                        if (claim == claiminDb.Name)
                                        {
                                            Attributes AttributeinDb = null;
                                            foreach (var attribute in attributesList)
                                            {
                                                if (attribute.name == claim)
                                                {
                                                    AttributeinDb = attribute;
                                                }
                                            }
                                            if (AttributeinDb == null || !scopeinDb.SaveConsent)
                                            {
                                                ClaimInfo claimInfo = new ClaimInfo();
                                                claimInfo.Name = claiminDb.Name;
                                                claimInfo.DisplayName = claiminDb.DisplayName;
                                                claimInfo.Description = claiminDb.Description;
                                                claimInfo.Mandatory = claiminDb.DefaultClaim;
                                                scopeInfo.ClaimsInfo.Add(claimInfo);
                                                requestedProfileAttributes.Add(claiminDb.Id.ToString());
                                            }
                                            else
                                            {
                                                userApprovedClaims.Add(claim);
                                            }
                                        }
                                    }
                                }
                            }
                            if (scopeInfo.ClaimsInfo.Any())
                            {
                                scopeInfo.ClaimsPresent = scopeinDb.IsClaimsPresent;
                                consentScopes.Add(scopeInfo);
                            }
                        }
                    }
                }

                if (consentScopes.Count > 0)
                {
                    ClientDetails clientdetails = new ClientDetails();
                    clientdetails.AppName = clientInDb.ApplicationName;
                    clientdetails.ClientId = clientInDb.ClientId;

                    //var authScheme = configuration["Consent_Authentication"];
                    var authScheme = "USER_AUTH_SELECTION";

                    // Prepare temporary session object
                    TemporarySession temporarySession = new TemporarySession
                    {
                        TemporarySessionId = tempAuthNSessId,
                        UserId = userBasicProfile.SubscriberUid,
                        DisplayName = userBasicProfile.DisplayName,
                        PrimaryAuthNSchemeList = new List<string> { authScheme },
                        AuthNSuccessList = new List<string>(),
                        Clientdetails = clientdetails,
                        IpAddress = string.Empty,
                        UserAgentDetails = string.Empty,
                        TypeOfDevice = string.Empty,
                        MacAddress = DTInternalConstants.NOT_AVAILABLE,
                        withPkce = false,
                        AdditionalValue = DTInternalConstants.pending,
                        AllAuthNDone = false,
                        AuthNStartTime = DateTime.Now.ToString("s"),
                        CoRelationId = Guid.NewGuid().ToString(),
                        TransactionId = transactionId
                    };

                    // Create temporary session
                    var task = await _cacheClient.Add(CacheNames.TemporarySession,
                        tempAuthNSessId, temporarySession);
                    if (DTInternalConstants.S_OK != task.retValue)
                    {
                        _logger.LogError("_cacheClient.Add failed");
                        await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);
                        return new APIResponse(_messageLocalizer.GetMessage(OIDCConstants.InternalError));
                    }

                    // Send notification to mobile
                    var eConsentNotification = new EConsentNotification()
                    {
                        AuthnScheme = authScheme,
                        AuthnToken = tempAuthNSessId,
                        RegistrationToken = userBasicProfile.FcmToken,
                        ApplicationName = clientInDb.ApplicationName,
                        ConsentScopes = consentScopes,
                        DeselectScopesAndClaims = deselectScopesAndClaims
                    };
                    try
                    {
                        var result = await _pushNotificationClient.SendEConsentNotification(
                            eConsentNotification);
                        if (null == result)
                        {
                            _logger.LogError("_pushNotificationClient.SendAuthnNotification" +
                                " failed");
                            await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);
                            return new APIResponse("Failed to send Notification");
                        }
                    }
                    catch (Exception error)
                    {
                        _logger.LogError("_pushNotificationClient." +
                            "SendAuthnNotification " +
                            "failed : {0}", error.Message);
                        await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);
                        return new APIResponse("Failed to send Notification");
                    }

                    await UpdateTransactionProfileConsent(transactionId, string.Join(", ", requestedAttributes), "PENDING");

                    DateTime date1 = DateTime.Now;
                    DateTime date2 = DateTime.Now.AddMinutes(1);
                    bool isError = false;
                    string message = string.Empty;
                    TemporarySession tempSession = null;

                    while (0 > DateTime.Compare(date1, date2))
                    {
                        // Check whether the temporary session exists
                        var isExists = await _cacheClient.Exists(CacheNames.TemporarySession,
                            tempAuthNSessId);

                        if (CacheCodes.KeyExist != isExists.retValue)
                        {
                            _logger.LogError("Temporary Session Expired/Not Found");
                            message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                            isError = true;
                            break;
                        }

                        // Get the temporary session object
                        tempSession = await _cacheClient.Get<TemporarySession>
                            (CacheNames.TemporarySession,
                            tempAuthNSessId);
                        if (null == tempSession)
                        {
                            _logger.LogError("Get Temporary Session Failed,Expired/Not Found");
                            await AddTransactionProfileStatus(transactionId, "FAILED", "Internal Error", "", -1);
                            message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                            isError = true;
                            break;
                        }

                        if (!tempSession.AdditionalValue.Equals(DTInternalConstants.pending))
                            break;

                        // Wait 500 Milli Seconds(Half Second) without blocking
                        await Task.Delay(500);
                        date1 = DateTime.Now;
                    }

                    if (isError)
                    {
                        await AddTransactionProfileStatus(transactionId, "FAILED", message, "", -1);
                        return new APIResponse(message);
                    }
                    if (tempSession.AdditionalValue == DTInternalConstants.DeniedConsent)
                    {
                        await AddTransactionProfileStatus(transactionId, "SUCCESS", tempSession.AdditionalValue, "", -1);
                        return new APIResponse(tempSession.AdditionalValue);
                    }
                    if (tempSession.AdditionalValue == DTInternalConstants.pending)
                    {
                        await AddTransactionProfileStatus(transactionId, "FAILED", "Request Timed Out", "", -1);
                        return new APIResponse("Request Timed Out");
                    }
                    if (tempSession.AdditionalValue != "true")
                    {
                        await AddTransactionProfileStatus(transactionId, "FAILED", tempSession.AdditionalValue, "", -1);
                        //await UpdateTransactionProfileConsent(transactionId, "NA", "FAILED");
                        return new APIResponse(tempSession.AdditionalValue);

                    }
                    foreach (var reqScope in Scopes)
                    {
                        foreach (var approvedScope in tempSession.allowedScopesAndClaims)
                        {
                            List<Attributes> attributes = new List<Attributes>();
                            var scope = await _scopeService.GetScopeIdByNameAsync(approvedScope.name);
                            var id = scope.ToString();
                            if ((reqScope == id) &&
                                approvedScope.claimsPresent)
                            {
                                foreach (var claim in approvedScope.claims)
                                {
                                    userApprovedClaims.Add(claim);
                                }
                            }
                        }
                    }
                }
            }
            ApprovedClaims = new HashSet<string>(userApprovedClaims);

            await UpdateTransactionProfileConsent(transactionId, string.Join(",", ApprovedClaims), "");

            Dictionary<string, object> response = new Dictionary<string, object>();

            foreach (var item in ApprovedClaims)
            {
                if (userProfile.ContainsKey(item))
                {
                    response[item] = userProfile[item];
                }
            }
            APIResponse aPIResponse = new APIResponse()
            {
                Success = true,
                Result = response,
                Message = "Get User Profile Success"
            };

            await AddTransactionProfileStatus(transactionId, "SUCCESS", "", "", -1);
            _logger.LogDebug("<--GetUserProfileDataAsync");
            return aPIResponse;
        }

        public async Task<ServiceResult> GetProfile(string[] scopes, string documentNumber)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            var raSubscriber = await GetUserBasicProfileAsync(documentNumber,
                    "PASSPORT", null);

            if (null == raSubscriber)
            {
                _logger.LogError("Subscriber details not found");
                return new ServiceResult(false, "User not Found");
            }

            var userDetails = new Dictionary<string, object>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };

            JObject keyValuePairs = JObject.Parse(JsonConvert.SerializeObject(raSubscriber, settings));

            foreach (var keyValue in keyValuePairs.Properties())
            {
                userDetails[keyValue.Name] = keyValue.Value.ToString();
            }

            if (scopes.Contains("kycprofile"))
            {
                var kycurl = configuration["KycUrl"];
                kycurl += documentNumber;
                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(kycurl);
                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogError($"Request to {kycurl} failed with status code {result.StatusCode}");

                        return new ServiceResult(false, "Internal error");
                    }
                }
                catch (Exception)
                {

                    _logger.LogError("Failed to connect Datapivot");
                    return new ServiceResult(false, "Internal error");
                }

                var responseString = await result.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<KycResponse>(responseString);
                if (apiResponse == null)
                {
                    return new ServiceResult(false, "Internal error");
                }
                if (!apiResponse.success)
                {
                    return new ServiceResult(false, apiResponse.message);
                }
                userDetails["kyc_document"] = apiResponse.result;
            }
            if (scopes.Contains("ekycprofile"))
            {
                var kycurl = configuration["EKycUrl"];

                kycurl += documentNumber;

                HttpResponseMessage result;
                try
                {
                    result = await client.GetAsync(kycurl);
                    if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogError($"Request to {kycurl} failed with status code {result.StatusCode}");
                        return new ServiceResult(false, "Internal error");
                    }
                }

                catch (Exception)
                {

                    _logger.LogError("Failed to connect Datapivot");
                    return new ServiceResult(false, "Internal error");
                }

                var responseString = await result.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<KycResponse>(responseString);
                if (apiResponse == null)
                {
                    return new ServiceResult(false, "Internal error");
                }
                if (!apiResponse.success)
                {
                    return new ServiceResult(false, apiResponse.message);
                }
                userDetails["Ekyc_document"] = apiResponse.result;
            }
            return new ServiceResult(true, "Success", userDetails);
        }

        public async Task<ServiceResult> GetAgentDetails(GetAgentDetailsDTO request)
        {
            _logger.LogInformation("Get Agent Details Started");

            var raSubscriber = await _unitOfWork.Subscriber.GetSubscriberInfobyDocType(request.Agent);
            if (raSubscriber == null)
            {
                return new ServiceResult(false, "Agent Not Found");
            }

            var tempAuthNSessId = string.Empty;
            try
            {
                tempAuthNSessId = EncryptionLibrary.KeyGenerator.GetUniqueKey();
            }
            catch (Exception error)
            {
                _logger.LogError(error.Message, error);
                return new ServiceResult(false, _messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }
            TemporarySession temporarySession = new TemporarySession
            {
                TemporarySessionId = tempAuthNSessId,
                UserId = raSubscriber.SubscriberUid,
                DisplayName = raSubscriber.DisplayName,
                PrimaryAuthNSchemeList = new List<string> { "DeviceAuthentication" },
                AuthNSuccessList = new List<string>(),
                Clientdetails = null,
                IpAddress = string.Empty,
                UserAgentDetails = string.Empty,
                TypeOfDevice = string.Empty,
                MacAddress = DTInternalConstants.NOT_AVAILABLE,
                withPkce = false,
                AdditionalValue = DTInternalConstants.pending,
                AllAuthNDone = false,
                AuthNStartTime = DateTime.Now.ToString("s"),
                CoRelationId = Guid.NewGuid().ToString()
            };

            var task = await _cacheClient.Add(CacheNames.TemporarySession,
                tempAuthNSessId, temporarySession);
            if (DTInternalConstants.S_OK != task.retValue)
            {
                return new ServiceResult(false, _messageLocalizer.GetMessage(OIDCConstants.InternalError));
            }
            var walletDelegationNotification = new WalletDelegationNotification
            {
                AuthnScheme = "DeviceAuthentication",
                AuthnToken = tempAuthNSessId,
                RegistrationToken = raSubscriber.FcmToken,
                Principal = request.Principal,
                DelegationPurpose = request.DelegationPurpose,
                NotaryInformation = request.NotaryInformation,
                ValidityPeriod = request.ValidityPeriod,
                Context = "POADelegation"
            };
            try
            {
                var result = _pushNotificationClient.SendWalletDelegationNotification(
                    walletDelegationNotification);
                if (null == result)
                {
                    return new ServiceResult(false, "Failed to send Notification");
                }
            }
            catch (Exception)
            {
                return new ServiceResult(false, "Failed to send Notification");
            }
            DateTime date1 = DateTime.Now;
            DateTime date2 = DateTime.Now.AddMinutes(2);
            bool isError = false;
            string message = string.Empty;
            TemporarySession tempSession = null;

            while (0 > DateTime.Compare(date1, date2))
            {
                // Check whether the temporary session exists
                var isExists = await _cacheClient.Exists(CacheNames.TemporarySession,
                    tempAuthNSessId);
                if (CacheCodes.KeyExist != isExists.retValue)
                {
                    _logger.LogError("Temporary Session Expired/Not Found");
                    message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                    isError = true;
                    break;
                }

                tempSession = await _cacheClient.Get<TemporarySession>
                    (CacheNames.TemporarySession,
                    tempAuthNSessId);
                if (null == tempSession)
                {
                    _logger.LogError("Get Temporary Session Failed,Expired/Not Found");
                    message = _messageLocalizer.GetMessage(OIDCConstants.InternalError);
                    isError = true;
                    break;
                }

                if (!tempSession.AdditionalValue.Equals(DTInternalConstants.pending))
                {
                    _logger.LogInformation("Consent Status :" + temporarySession.AdditionalValue);
                    break;
                }


                await Task.Delay(500);
                date1 = DateTime.Now;
            }
            if (isError)
            {
                return new ServiceResult(false, "Internal Error");
            }
            if (tempSession.AdditionalValue == DTInternalConstants.pending)
            {
                return new ServiceResult(false, "Request Timed Out");
            }
            if (tempSession.AdditionalValue != DTInternalConstants.S_True)
            {
                return new ServiceResult(false, tempSession.AdditionalValue);
            }
            Dictionary<string, string> agentDetails = new Dictionary<string, string>();

            agentDetails["agentName"] = raSubscriber.DisplayName;

            agentDetails["agentEmail"] = raSubscriber.Email;

            agentDetails["agentSuid"] = raSubscriber.SubscriberUid;

            _logger.LogInformation("Get Agent Details Ended");

            return new ServiceResult(true, "Get Agent Details Success", agentDetails);
        }

    }
}