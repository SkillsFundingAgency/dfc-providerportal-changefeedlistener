using System;
using System.Collections.Generic;
using System.Text;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Search.Models;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ISearchServiceWrapper
    {
        IEnumerable<IndexingResult> UploadBatch(
            IEnumerable<AzureSearchProviderModel> providers,
            IEnumerable<AzureSearchVenueModel> venues,
            IReadOnlyList<Document> documents,
            out int succeeded);
    }
}
