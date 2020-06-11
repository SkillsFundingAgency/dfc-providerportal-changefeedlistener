using Dfc.ProviderPortal.ChangeFeedListener.Models.Foac;
using Microsoft.Azure.Search.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IFaocSearchServiceWrapper
    {
        Task<IEnumerable<IndexingResult>> UploadFaocBatch(IEnumerable<FaocEntry> documents);
        Task DeleteStaleDocuments(IEnumerable<FaocEntry> documents);
    }
}
