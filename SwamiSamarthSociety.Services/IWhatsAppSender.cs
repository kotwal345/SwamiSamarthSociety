namespace SwamiSamarthSociety.Services
{
    public record WhatsAppSendResult(bool Success, string? ResponseOrError);

    public interface IWhatsAppSender
    {
        // bodyParameters fill the approved template's {{1}}, {{2}}, {{3}}... placeholders in order.
        Task<WhatsAppSendResult> SendTemplateAsync(string phoneNumber, string[] bodyParameters, CancellationToken ct = default);
    }
}
