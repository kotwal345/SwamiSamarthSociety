using System;
using System.Collections.Generic;

namespace SwamiSamarthSociety.Data.Entities
{
    // The tenant. Every client society the product hosts is one row here.
    public class Society
    {
        public int SocietyId { get; set; }
        public string Name { get; set; } = null!;
        public string? NameMarathi { get; set; } // used in SMS/WhatsApp reminder text when set
        public string Code { get; set; } = null!; // short unique slug, e.g. "SWAMI-SAMARTH"
        public string? ContactPersonName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? DeactivatedDate { get; set; }
    }
}
