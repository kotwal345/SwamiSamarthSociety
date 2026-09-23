using System.ComponentModel.DataAnnotations;

namespace SwamiSamarthSociety.Web.Areas.SuperAdmin.Models
{
    public class SocietyListItemViewModel
    {
        public int SocietyId { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateSocietyViewModel
    {
        [Required(ErrorMessage = "Society name is required.")]
        [Display(Name = "Society name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Society name (Marathi)")]
        public string? NameMarathi { get; set; }

        [Required(ErrorMessage = "A short code is required.")]
        [Display(Name = "Short code")]
        public string Code { get; set; } = null!;

        [Display(Name = "Contact person")]
        public string? ContactPersonName { get; set; }

        [Display(Name = "Contact phone")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Contact email")]
        public string? ContactEmail { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "The admin's email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Admin's email (used as their login)")]
        public string AdminEmail { get; set; } = null!;
    }

    public class SocietyCreatedViewModel
    {
        public string SocietyName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string TempPassword { get; set; } = null!;
    }
}
