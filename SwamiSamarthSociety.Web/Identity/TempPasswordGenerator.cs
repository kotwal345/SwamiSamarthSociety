using System.Security.Cryptography;

namespace SwamiSamarthSociety.Web.Identity
{
    // Generates a random temp password that satisfies ASP.NET Core Identity's default
    // complexity policy (upper, lower, digit, non-alphanumeric, length >= 6), avoiding
    // visually-confusable characters (0/O, 1/l/I) since it gets read off a screen and typed.
    public static class TempPasswordGenerator
    {
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Digits = "23456789";
        private const string Special = "!@#$%*?";
        private const string All = Lower + Upper + Digits + Special;

        public static string Generate()
        {
            var chars = new List<char> { PickRandom(Upper), PickRandom(Lower), PickRandom(Digits), PickRandom(Special) };
            for (var i = 0; i < 8; i++)
                chars.Add(PickRandom(All));

            // Fisher-Yates shuffle so the guaranteed-category characters aren't always first.
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static char PickRandom(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
