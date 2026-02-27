using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTPortal.Core.Domain.Services.Communication
{
    public class SaveConsentRequest
    {
        [Required]
        [StringLength(100)]
        public string sessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string suid { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one scope/profile must be selected.")]
        public List<ProfileInfo> scopes { get; set; } = new();
    }
}
