using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace SwamiSamarthSociety.Services
{
    // Sends WhatsApp messages through Meta's WhatsApp Business Cloud API
    // (https://graph.facebook.com/{version}/{phone-number-id}/messages) -- free at this society's
    // volume, unlike SMS, but Meta requires the same kind of pre-approval SMS needs under India's
    // DLT rules: every business-initiated message must use a template that's been submitted and
    // approved in Meta Business Manager first. Free-form text only works as a *reply* within 24h of
    // the customer messaging you, which doesn't fit "notify them the moment payment is recorded".
    //
    // One-time setup (outside this codebase, in Meta Business Manager / developers.facebook.com):
    //   1. Create a Meta Business account and a WhatsApp Business Platform app.
    //   2. Add and verify a WhatsApp-enabled phone number for the society.
    //   3. Create a message template (Business Manager > WhatsApp Manager > Message Templates),
    //      category "Utility", with body text like:
    //        "नमस्कार {{1}}, || श्री स्वामी समर्थ सोसायटी || आपली दिनांक {{2}} रोजी रु.{{3}} रक्कम जमा झाली आहे. धन्यवाद."
    //      Submit it for approval (usually minutes to ~1 day for a straightforward utility template).
    //   4. Generate a permanent access token (System User token, not the 24h test token) for the app.
    // Then set these three settings before this will send anything for real:
    //   WhatsApp:AccessToken       - the permanent access token from step 4
    //   WhatsApp:PhoneNumberId     - the verified number's Phone Number ID (from the app dashboard)
    //   WhatsApp:TemplateName      - the exact name of the approved template from step 3
    //   WhatsApp:TemplateLanguage  - the template's language code, e.g. "mr" (default) or "en"
    // Until AccessToken/PhoneNumberId/TemplateName are set, SendTemplateAsync returns a clear
    // failure instead of calling out to Meta, so callers can run safely in an unconfigured environment.
    public class WhatsAppCloudApiSender : IWhatsAppSender
    {
        private const string GraphApiVersion = "v20.0";

        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public WhatsAppCloudApiSender(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<WhatsAppSendResult> SendTemplateAsync(string phoneNumber, string[] bodyParameters, CancellationToken ct = default)
        {
            var accessToken = _config["WhatsApp:AccessToken"];
            var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
            var templateName = _config["WhatsApp:TemplateName"];
            var templateLanguage = _config["WhatsApp:TemplateLanguage"];
            if (string.IsNullOrWhiteSpace(templateLanguage)) templateLanguage = "mr";

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(templateName))
                return new WhatsAppSendResult(false, "WhatsApp isn't configured yet (missing WhatsApp:AccessToken / PhoneNumberId / TemplateName in appsettings).");

            var mobile = NormalizeIndianMobile(phoneNumber);
            if (mobile is null)
                return new WhatsAppSendResult(false, $"'{phoneNumber}' doesn't look like a valid 10-digit Indian mobile number.");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = mobile,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = templateLanguage },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = bodyParameters.Select(p => new { type = "text", text = p }).ToArray()
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/{GraphApiVersion}/{phoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(payload);

            try
            {
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                return new WhatsAppSendResult(response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                return new WhatsAppSendResult(false, ex.Message);
            }
        }

        // Meta expects the number with the country code, no '+', no spaces (e.g. "919876543210").
        private static string? NormalizeIndianMobile(string phoneNumber)
        {
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 10) return "91" + digits;
            if (digits.Length == 12 && digits.StartsWith("91")) return digits;
            return null;
        }
    }
}
