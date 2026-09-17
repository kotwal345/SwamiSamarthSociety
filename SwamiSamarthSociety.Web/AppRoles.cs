namespace SwamiSamarthSociety.Web
{
    // The two roles this app knows about: Chairman (full read/write access -- society अध्यक्ष)
    // and Member (read-only -- everyone else who signs in just to view records).
    public static class AppRoles
    {
        public const string Chairman = "Chairman";
        public const string Member = "Member";
    }
}
