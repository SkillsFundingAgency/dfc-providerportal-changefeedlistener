using Microsoft.Azure.Documents;
using System;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class CourseAudit
    {
        public Guid id { get; set; }
        public string Collection { get; set; }
        public string DocumentId { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public Document Document { get; set; }
    }
}
