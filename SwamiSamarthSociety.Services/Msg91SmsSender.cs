using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace SwamiSamarthSociety.Services
{
    // Sends SMS through MSG91's Flow API (https://control.msg91.com/api/v5/flow/).
    //
    // India requires DLT-registered templates for transactional/promotional SMS -- MSG91 will
    // reject (or the telecom carrier will silently drop) any message that doesn't match a template
    // pre-approved on your DLT entity. The simplest way to still send a different message every
    // month (varying amounts, names, etc.) is to register ONE DLT template whose entire approved
    // content is a single variable, e.g. "{#var#}", then this class sends the fully-composed
    // reminder text as that one variable (VAR1). Configure these three settings before this will
    // send anything for real:
    //   Msg91:AuthKey      - from your MSG91 dashboard (Settings > API)
    //   Msg91:TemplateId   - the DLT-approved template's MSG91 template_id
    //   Msg91:SenderId     - your approved 6-character DLT sender ID (for logging only; the
    //                        template itself already has the sender ID bound to it on MSG91)
    // Until AuthKey/TemplateId are set, SendAsync returns a clear failure instead of calling out
    // to MSG91, so the reminder job can run safely in an unconfigured environment.
    public class Msg91SmsSender : ISmsSender
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public Msg91SmsSender(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            var authKey = _config["Msg91:AuthKey"];
            var templateId = _config["Msg91:TemplateId"];

            if (string.IsNullOrWhiteSpace(authKey) || string.IsNullOrWhiteSpace(templateId))
                return new SmsSendResult(false, "MSG91 is not configured yet (missing Msg91:AuthKey / Msg91:TemplateId in appsettings).");

            var mobile = NormalizeIndianMobile(phoneNumber);
            if (mobile is null)
                return new SmsSendResult(false, $"'{phoneNumber}' doesn't look like a valid 10-digit Indian mobile number.");

            var payload = new Msg91FlowRequest
            {
                TemplateId = templateId,
                ShortUrl = "0",
                Recipients = new[] { new Msg91Recipient { Mobiles = mobile, Var1 = message } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/flow/");
            request.Headers.Add("authkey", authKey);
            request.Content = JsonContent.Create(payload);

            try
            {
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                return new SmsSendResult(response.IsSuccessStatusCode, body);
            }
            catch (Exception ex)
            {
                return new SmsSendResult(false, ex.Message);
            }
        }

        // MSG91 expects the number with the country code, no '+', no spaces (e.g. "919876543210").
        private static string? NormalizeIndianMobile(string phoneNumber)
        {
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 10) return "91" + digits;
            if (digits.Length == 12 && digits.StartsWith("91")) return digits;
            return null;
        }

        private class Msg91FlowRequest
        {
            [JsonPropertyName("template_id")]
            public string TemplateId { get; set; } = null!;
            [JsonPropertyName("short_url")]
            public string ShortUrl { get; set; } = "0";
            [JsonPropertyName("recipients")]
            public Msg91Recipient[] Recipients { get; set; } = Array.Empty<Msg91Recipient>();
        }

        private class Msg91Recipient
        {
            [JsonPropertyName("mobiles")]
            public string Mobiles { get; set; } = null!;
            [JsonPropertyName("VAR1")]
            public string Var1 { get; set; } = null!;
        }
    }
}
