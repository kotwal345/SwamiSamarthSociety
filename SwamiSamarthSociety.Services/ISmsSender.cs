namespace SwamiSamarthSociety.Services
{
    public record SmsSendResult(bool Success, string? ResponseOrError);

    public interface ISmsSender
    {
        Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
    }
}
