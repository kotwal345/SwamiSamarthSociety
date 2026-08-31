using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class BankAccount
    {
        public int BankAccountId { get; set; }
        public string AccountName { get; set; } = null!;
        public decimal OpeningBalance { get; set; }
        public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    }

    public class BankTransaction
    {
        public int BankTransactionId { get; set; }
        public int BankAccountId { get; set; }
        public BankAccount BankAccount { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = null!; // Credit / Debit
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public decimal Balance { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
