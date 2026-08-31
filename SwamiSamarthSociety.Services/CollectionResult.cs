namespace SwamiSamarthSociety.Services
{
    public class CollectionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static CollectionResult Ok() => new() { Success = true };
        public static CollectionResult Fail(string error) => new() { Success = false, Error = error };
    }
}
