using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTPortal.Core.Domain.Services.Communication
{
    public class AuthorizationRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string client_id { get; set; }
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string redirect_uri { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string response_type { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string scope { get; set; }
        [StringLength(100)]
        public string state { get; set; }
        [StringLength(100)]
        public string nonce { get; set; }
        [StringLength(5000)]
        public string request { get; set; }
        [StringLength(100)]
        public string code_challenge { get; set; }
        [StringLength(100)]
        public string code_challenge_method { get; set; }
    }
}
