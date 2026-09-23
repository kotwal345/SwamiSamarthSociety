using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class PaymentConfirmationService : IPaymentConfirmationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWhatsAppSender _whatsAppSender;
        private readonly ICurrentSocietyContext _tenant;

        public PaymentConfirmationService(ApplicationDbContext db, IWhatsAppSender whatsAppSender, ICurrentSocietyContext tenant)
        {
            _db = db;
            _whatsAppSender = whatsAppSender;
            _tenant = tenant;
        }

        public async Task NotifyAsync(int memberId, decimal amountPaid, DateTime paymentDate, CancellationToken ct = default)
        {
            if (amountPaid <= 0) return;

            var member = await _db.Members.FindAsync(new object[] { memberId }, ct);
            if (member is null || string.IsNullOrWhiteSpace(member.MobileNumber)) return;

            var society = _tenant.SocietyId is { } societyId
                ? await _db.Societies.FirstOrDefaultAsync(s => s.SocietyId == societyId, ct)
                : null;
            var societyName = society?.NameMarathi ?? society?.Name ?? "आमची सोसायटी";

            var name = member.FullNameMarathi ?? member.FullName;
            var dateText = paymentDate.ToString("dd-MM-yyyy");
            var amountText = amountPaid.ToString("N0");

            // Must match the approved template's {{1}} {{2}} {{3}} order -- see the comment at the
            // top of WhatsAppCloudApiSender.cs for the template text this expects.
            var sendResult = await _whatsAppSender.SendTemplateAsync(
                member.MobileNumber!, new[] { name, dateText, amountText }, ct);

            _db.SmsLogs.Add(new SmsLog
            {
                MemberId = member.MemberId,
                PhoneNumber = member.MobileNumber!,
                Message = $"नमस्कार {name}, || {societyName} || आपली दिनांक {dateText} रोजी रु.{amountText} रक्कम जमा झाली आहे. धन्यवाद.",
                ReminderType = "PaymentConfirmation",
                Channel = "WhatsApp",
                SentDate = DateTime.UtcNow,
                Success = sendResult.Success,
                ProviderResponse = sendResult.ResponseOrError
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
