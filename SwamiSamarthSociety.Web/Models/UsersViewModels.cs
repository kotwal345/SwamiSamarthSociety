using System.ComponentModel.DataAnnotations;

namespace SwamiSamarthSociety.Web.Models
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool MustChangePassword { get; set; }
    }

    public class CreateMemberLoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Member's email (used as their login)")]
        public string Email { get; set; } = null!;
    }

    public class MemberLoginCreatedViewModel
    {
        public string Email { get; set; } = null!;
        public string TempPassword { get; set; } = null!;
    }
}
