using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Extensions.Logging;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ICourseAuditService
    {
        Task RepopulateSearchIndex(ILogger log, bool deleteZombies);
        Task UploadCoursesToSearch(ILogger log, IReadOnlyList<Document> documents);
        Task<CourseAudit> Audit(ILogger log, Document auditee);
    }
}
