using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DTPortal.Core.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class GetAgentDetailsDTO
    {
        [Required]
        [StringLength(100)]
        public string Principal { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one delegation purpose is required.")]
        public List<string> DelegationPurpose { get; set; } = new();

        [StringLength(500)]
        public string NotaryInformation { get; set; } = string.Empty;

        [StringLength(100)]
        public string ValidityPeriod { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Agent { get; set; } = string.Empty;
    }
}
