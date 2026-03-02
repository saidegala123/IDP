using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DTPortal.Core.Domain.Services.Communication
{
    public class ConsentRequest
    {
        public string SessionId { get; set; }
    }

    public class ConsentResponse
    {
        public string clientId { get; set; }
        public string clientName { get; set; }
        public bool consentRequired { get; set; }
        public List<ScopeDetail> scopes { get; set; }
    }

    public class ScopeDetail
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<AttributeInfo> Attributes { get; set; }

    }

    public class AttributeInfo
    {
        public string Name { get; set; }
        public bool Mandatory { get; set; }
        public string DisplayName { get; set; }
    }

    public class ConsentApprovalRequest
    {
        [Required]
        [StringLength(100)]
        public string clientId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string suid { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one scope must be selected.")]
        public List<ScopeObject> scopes { get; set; } = new();
    }

    public class ScopeObject
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one attribute is required for the scope.")]
        public List<string> Attributes { get; set; } = new();
    }

    public class CredentialDetail
    {
        [Required]
        [StringLength(100)]
        public string CredentialId { get; set; }
        [Required]
        [StringLength(200)]
        public string DisplayName { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "At least one attribute is required.")]
        public List<ClaimsDetail> Attributes { get; set; }
    }

    public class ProfileInfo
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        public List<ClaimsDetail> Attributes { get; set; } = new();
    }

    public class ClaimsDetail
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;
    }
}
