using DTPortal.Core.Domain.Services.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTPortal.Core.DTOs
{
    public class WalletTransactionRequestDTO
    {
        [Required]
        [StringLength(10)]
        public string status {  get; set; }
        [Required]
        public string statusMessage { get; set; }
        [Required]
        [StringLength(100)]
        public string clientID { get; set; }
        [Required]
        [StringLength(100)]
        public string suid { get; set; }
        [Required]
        public string presentationToken { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "At least one profile is required.")]
        public List<CredentialDetail> profiles { get; set; } 
    }

    public class CallStackObject
    {
        public string presentationToken { get; set; }
        public List<CredentialDetail> profiles { get; set; }
    }
}
