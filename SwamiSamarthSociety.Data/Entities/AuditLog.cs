using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Data.Entities
{
    public class AuditLog : ITenantScoped
    {
        public int AuditLogId { get; set; }
        public int SocietyId { get; set; }
        public string TableName { get; set; } = null!;
        public int RecordId { get; set; }
        public string Action { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    }

    public class ImportLog : ITenantScoped
    {
        public int ImportLogId { get; set; }
        public int SocietyId { get; set; }
        public string FileName { get; set; } = null!;
        public string ImportType { get; set; } = null!;
        public int RowsProcessed { get; set; }
        public int RowsSucceeded { get; set; }
        public int RowsFailed { get; set; }
        public string? ImportedBy { get; set; }
        public DateTime ImportedDate { get; set; } = DateTime.UtcNow;
        public string? ErrorDetails { get; set; }
    }
}
