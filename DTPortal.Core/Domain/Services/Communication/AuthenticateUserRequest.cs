using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTPortal.Core.Domain.Services.Communication
{
    public class AuthenticateUserRequest
    {
        [Required]
        [StringLength(100)]
        public string SessionId { get; set; }
        [Required]
        [StringLength(100)]
        public string AuthenticationScheme { get; set; }
        [StringLength(500)]
        public string AuthenticationData { get; set; }
        public bool Approved { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int statusCode { get; set; }
        [Required]
        public List<ProfileInfo> scopes { get; set; }
    }
}
