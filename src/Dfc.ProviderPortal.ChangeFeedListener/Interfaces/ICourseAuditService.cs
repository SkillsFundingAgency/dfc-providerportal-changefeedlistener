using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ICourseAuditService
    {
        Task RepopulateSearchIndex(ILogger log);
        Task<IEnumerable<IndexingResult>> UploadCoursesToSearch(ILogger log, IReadOnlyList<Document> documents);
        Task<CourseAudit> Audit(ILogger log, Document auditee);
    }
}
