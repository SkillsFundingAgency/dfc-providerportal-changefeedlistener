
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Logging;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Document = Microsoft.Azure.Documents.Document;


namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ICourseArchiveService
    {
        Task<IEnumerable<Document>> ArchiveAllCourses(ILogger log, IEnumerable<Document> documents);
    }
}
