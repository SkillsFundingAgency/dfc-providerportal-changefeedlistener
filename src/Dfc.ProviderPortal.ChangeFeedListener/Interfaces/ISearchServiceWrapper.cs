using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Azure.Search.Models;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ISearchServiceWrapper
    {
        Task<IEnumerable<IndexingResult>> UploadBatch(
            IDictionary<int, AzureSearchProviderModel> providers,
            IDictionary<Guid, AzureSearchVenueModel> venues,
            IEnumerable<Document> documents);
    }
}
