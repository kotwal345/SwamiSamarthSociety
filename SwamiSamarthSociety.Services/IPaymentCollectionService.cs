namespace SwamiSamarthSociety.Services
{
    public interface IPaymentCollectionService
    {
        // Records one member's payment (share + optional loan installment) for one open monthly
        // cycle. Overwrites (not increments) so resubmitting the same row is always safe.
        Task<CollectionResult> RecordPaymentAsync(
            int monthlyCycleId,
            int memberId,
            decimal shareAmountPaid,
            decimal? loanAmountPaid,
            DateTime paymentDate,
            string? paymentMethod,
            string? receiptNumber,
            string? createdBy);
    }
}
