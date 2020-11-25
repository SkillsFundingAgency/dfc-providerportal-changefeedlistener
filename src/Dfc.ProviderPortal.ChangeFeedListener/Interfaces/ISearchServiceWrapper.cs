using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ISearchServiceWrapper
    {
        Task<IReadOnlyList<string>> UploadBatch(
            Guid updateBatchId,
            IDictionary<int, AzureSearchProviderModel> providers,
            IDictionary<Guid, AzureSearchVenueModel> venues,
            IEnumerable<Document> documents);
    }
}
