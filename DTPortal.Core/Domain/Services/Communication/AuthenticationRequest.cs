using DTPortal.Core.Domain.Models;
using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DTPortal.Common.CommonResponse;

namespace DTPortal.Core.Domain.Services.Communication
{

    public class clientDetails
    {
        public string clientId { get; set; }
        public string scopes { get; set; }
        public string redirect_uri { get; set; }
        public string response_type { get; set; }
        public string grant_type { get; set; }
        public string nonce { get; set; }
        public string state { get; set; }
        public bool withPkce { get; set; }
    }

    public class GetAuthSessClientDetails
    {
        public string clientId { get; set; }
        public string scopes { get; set; }
        public string redirect_uri { get; set; }
        public string response_type { get; set; }
        public bool withPkce { get; set; }
    }
    public class Pkcedetails
    {
        public string codeChallenge { get; set; }
        public string codeChallengeMethod { get; set; }
    }

    public class ClientDetails
    {
        public string ClientId { get; set; }
        public string ExpiresAt { get; set; }
        public string Scopes { get; set; }
        public string GrantType { get; set; }
        public string RedirectUrl { get; set; }
        public string ResponseType { get; set; }
        public string AppName { get; set; }
    }

    public class TemporarySession
    {
        public string TemporarySessionId { get; set; }
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public IList<string> PrimaryAuthNSchemeList { get; set; }
        public IList<string> AuthNSuccessList { get; set; }
        public string LastAccessTime { get; set; }
        public string TypeOfDevice { get; set; }
        public string AdditionalValue { get; set; }
        public string UserAgentDetails { get; set; }
        public ClientDetails Clientdetails { get; set; }
        public string RandomCode { get; set; }
        public bool AllAuthNDone { get; set; }
        public bool withPkce { get; set; }
        public Pkcedetails PkceDetails { get; set; }
        public string AuthNStartTime { get; set; }
        public string CoRelationId { get; set; }
        public IList<LoginProfile> LoginProfile { get; set; }
        public List<ScopeAndClaimsInfo> allowedScopesAndClaims { get; set; }
        public string LoginType { get; set; }
        public string DocumentId { get; set; }
        public int TransactionId { get; set; }
        public string DeviceToken { get; set; }
        public string NotificationAdditionalValue { get; set; }
        public bool NotificationAuthNDone { get; set; }
        public string JourneyToken { get; set; }
    }

    public class OperationsDetails
    {
        public string OperationName { get; set; }
        public DateTime AuthenticatedTime { get; set; }
    }

    public class ScopeAndClaimsInfo
    {
        public string name { get; set; }
        public bool claimsPresent { get; set; }
        public List<string> claims { get; set; }
    }

    public class GlobalSession
    {
        public string GlobalSessionId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string AuthenticationScheme { get; set; }
        public string LoggedInTime { get; set; }
        public string LastAccessTime { get; set; }
        public string TypeOfDevice { get; set; }
        public string AdditionalValue { get; set; }
        public string UserAgentDetails { get; set; }
        public IList<string> ClientId { get; set; }
        public List<ClientAttributes> AcceptedAttributes { get; set; }
        public IList<OperationsDetails> OperationsDetails { get; set; }
        public List<string> LoggedClients { get; set; }
        public string CoRelationId { get; set; }
        public string LoginType { get; set; }
        public IList<LoginProfile> LoginProfile { get; set; }
    }

    public class ClientAttributes
    {
        public string ClientId { get; set; }
        public List<string> Attributes {  get; set; }
    }

    public class LoginProfile
    {
        public string Email { get; set; }
        public string OrgnizationId { get; set; }

    }

    public class UserSessions
    {
        public IList<string> GlobalSessionIds { get; set; }
    }

    public class ClientSessions
    {
        public IList<string> GlobalSessionIds { get; set; }
    }

    public class Authorizationcode
    {
        public string AuthZCode { get; set; }
        public string GlobalSessionId { get; set; }
        public string ClientId { get; set; }
        public string ExpiresAt { get; set; }
        public string Scopes { get; set; }
        public string RedirectUrl { get; set; }
        public string ResponseType { get; set; }
        public bool withPkce { get; set; }
        public string Nonce { get; set; }
        public string State { get; set; }
        public Pkcedetails PkceDetails { get; set; }
    }

    public class Accesstoken
    {
        public string GlobalSessionId { get; set; }
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public int ExpiresAt { get; set; }
        public string ClientId { get; set; }
        public string Scopes { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenExpiresAt { get; set; }
        public string GrantType { get; set; }
        public string RedirectUrl { get; set; }
        public List<string> AcceptedAttributes { get; set; }
    }

    public class Refreshtoken
    {
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshTokenExpiresAt { get; set; }
        public string ClientId { get; set; }
        public string GlobalSession { get; set; }
        public string RedirectUrl { get; set; }
        public string Scopes { get; set; }
        public string Nonce { get; set; }
    }

    public class ValidateClientRequest
    {
        public GetAuthSessClientDetails clientDetails { get; set; }
        public Pkcedetails PkceDetails { get; set; }
        public Client clientDetailsInDb { get; set; }
    }

    public class VerifyUserRequest
    {
        [Required]
        [StringLength(50)]
        public string userInput { get; set; }
        [Required]
        [Range(0,10)]
        public int type { get; set; }
        [Required]
        [StringLength(50)]
        public string clientId { get; set; }
        [Required]
        public int clientType { get; set; }
        [StringLength(100)]
        public string ip { get; set; }
        [StringLength(1000)]
        public string userAgent { get; set; }
        [StringLength(100)]
        public string typeOfDevice { get; set; }
        public bool rememberUser{ get; set; }

    }
    public class VerifyUserAuthDataRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 30)]
        public string AuthnToken { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string authenticationScheme { get; set; }
        [StringLength(50)]
        public string authenticationData { get; set; }
        public bool approved { get; set; }
        [StringLength(4)]
        public string randomCode { get; set; }
        [StringLength(20)]
        public string documentNumber { get; set; }
        [Required]
        [Range(-2,8)]
        public int statusCode { get; set; }
    }

    public class GetAccessTokenRequest
    {
        [StringLength(100)]
        public string code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string grant_type { get; set; } = string.Empty;

        [StringLength(1000)]
        public string redirect_uri { get; set; } = string.Empty;

        [StringLength(100)]
        public string client_id { get; set; } = string.Empty;

        [StringLength(200)]
        public string client_secret { get; set; } = string.Empty;

        [StringLength(500)]
        public string scope { get; set; } = string.Empty;

        [StringLength(200)]
        public string code_verifier { get; set; } = string.Empty;

        [StringLength(100)]
        public string client_assertion_type { get; set; } = string.Empty;

        [StringLength(5000)]
        public string client_assertion { get; set; } = string.Empty;

        [StringLength(2000)]
        public string assertion { get; set; } = string.Empty;

        [StringLength(2000)]
        public string refresh_token { get; set; } = string.Empty;
    }

    public class LogoutUserRequest
    {
        public string GlobalSession { get; set; }
    }
    public class GetTokenResponse
    {
        public string access_token { get; set; }
        // Type of access token. Always has the “Bearer” value.
        public string token_type { get; set; }
        // Lifetime (in seconds) of the access token.
        public long expires_in { get; set; }
        // Scopes granted to those to which the access token is associated,
        // separated by spaces.
        public string scopes { get; set; }

        public string id_token { get; set; }
    }
    public class GetAuthZCodeRequest
    {
        public string GlobalSessionId { get; set; }
        public clientDetails ClientDetails { get; set; }
        public Pkcedetails pkcedetails { get; set; }
    }
    public class GetAuthNSessReq
    {
        public GetAuthSessClientDetails clientDetails { get; set; }
        public Pkcedetails PkceDetails { get; set; }
        public string userId { get; set; }
        public string ip { get; set; }
        public string mac { get; set; }
    }

    public class GetInternalAuthNSessReq
    {
        public string clientId { get; set; }
        public string userId { get; set; }
        public string ip { get; set; }
        public string mac { get; set; }
    }

    public class Authentication_Scheme
    {
        public string name { get; set; }
        public string description { get; set; }
    }
    public class GetAuthNSessRes
    {
        public string success { get; set; }
        public string message { get; set; }
        public string temporarySession { get; set; }
        public IList<string> authenticationSchemes { get; set; }

    }

    public class SendMobileNotificationRes
    {
        public string success { get; set; }
        public string message { get; set; }
        public string randomCode { get; set; }
    }

    public class fido2Response
    {
        public AssertionOptions AssertionOptions { get; set; }
        public AuthenticatorAssertionRawResponse AuthenticatorAssertionRawResponse { get; set; }
    }

    public class LogoutRequest
    {
        public string id_token_hint { get; set; }
        public string post_logout_redirect_uri { get; set; }
        public string state { get; set; }
    }
    public class VerifyUserAuthNDataRequest
    {
        [StringLength(50, MinimumLength = 30)]
        public string authnToken { get; set; }
        [StringLength(50, MinimumLength = 1)]
        public string authenticationScheme { get; set; }
        [StringLength(50)]
        public string authenticationData { get; set; }
        [StringLength(10)]
        public string approved { get; set; }
        [MaxLength(20)]
        public List<ScopeAndClaimsInfo> allowedScopesAndClaims { get; set; }
    }
    public class VerifyUserAuthenticationDataRequest
    {
        public string authenticationScheme { get; set; }
        public string authenticationData { get; set; }
        public string suid { get; set; }
        public string token { get; set; }
    }
    public class VerifyQrCodeRequest
    {
        public string tempSession { get; set; }
        public string qrCode { get; set; }
    }
    public class UserAuthDataReq
    {
        [Required]
        [StringLength(50, MinimumLength = 30)]
        public string suid { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string priauthscheme { get; set; }
    }

    public class UserAuthDataRes
    {
        public string AuthData { get; set; }
        public string priauthscheme { get; set; }
    }

    public class VerifyAgentConsentRequest
    {
        [StringLength(50, MinimumLength = 30)]
        public string authnToken { get; set; }
        [StringLength(50, MinimumLength = 1)]
        public string authenticationScheme { get; set; }
        [StringLength(50)]
        public string authenticationData { get; set; }
        [StringLength(10)]
        public string approved { get; set; }
    }
    public class VerifyUserAuthenticationRequest
    {
        public string authenticationScheme { get; set; }
        public string authenticationData { get; set; }
        public string Suid { get; set; }
    }
    public class  VerifierUrlResponse()
    {
        public string verifierUrl { get; set; }
        public string VerifierCode { get; set; }
    }
    public class VerifyQrRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 30)]
        public string clientId { get; set; }
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string qrCode { get; set; }
    }
    public class MobileAuthTemporarySession
    {
        public string TemporarySessionId { get; set; }
        public ClientDetails ClientDetails { get; set; }
        public bool AllAuthNDone { get; set; }
        public string globalSessionId { get; set; }
        public bool withPkce { get; set; }
        public Pkcedetails PkceDetails { get; set; }
        public string State { get; set; }
        public string AcrValues { get; set; }
        public string nonce { get; set; }
        public string CoRelationId { get; set; }
        public List<string> AcceptedAttributes { get; set; }
        public string JourneyToken { get; set; }
    }

    public class ICPAuthRequest : IValidatableObject
    {
        [StringLength(50)]
        public string DocumentNumber { get; set; }

        [StringLength(20)]
        public string DocumentType { get; set; }

        [StringLength(50)]
        public string Suid { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasDocumentInfo =
                !string.IsNullOrWhiteSpace(DocumentNumber) &&
                !string.IsNullOrWhiteSpace(DocumentType);

            bool hasSuid =
                !string.IsNullOrWhiteSpace(Suid);

            if (!hasDocumentInfo && !hasSuid)
            {
                yield return new ValidationResult(
                    "Either (DocumentNumber and DocumentType) OR Suid must be provided.");
            }

            if (hasDocumentInfo && hasSuid)
            {
                yield return new ValidationResult(
                    "Provide either (DocumentNumber and DocumentType) OR Suid, not both.");
            }

            if (!string.IsNullOrWhiteSpace(DocumentNumber) &&
                string.IsNullOrWhiteSpace(DocumentType))
            {
                yield return new ValidationResult(
                    "DocumentType is required when DocumentNumber is provided.");
            }

            if (!string.IsNullOrWhiteSpace(DocumentType) &&
                string.IsNullOrWhiteSpace(DocumentNumber))
            {
                yield return new ValidationResult(
                    "DocumentNumber is required when DocumentType is provided.");
            }
        }
    }
}
