namespace SwamiSamarthSociety.Web
{
    // Three roles: SuperAdmin (the product owner -- creates/deactivates societies, no
    // SocietyId of their own), SocietyAdmin (full read/write within their one society --
    // society अध्यक्ष), and Member (read-only within their one society).
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string SocietyAdmin = "SocietyAdmin";
        public const string Member = "Member";
    }
}
