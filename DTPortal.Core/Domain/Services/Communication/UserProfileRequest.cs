using Fido2NetLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTPortal.Core.Domain.Services.Communication
{
    public class GetUserProfileRequest
    {
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserIdType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ProfileType { get; set; } = string.Empty;

        [StringLength(200)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [StringLength(500)]
        public string Scopes { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Token { get; set; } = string.Empty;
    }
}
