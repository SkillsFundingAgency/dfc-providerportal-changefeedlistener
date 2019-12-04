using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ICosmosDbHelper
    {
        DocumentClient GetClient();
        Task<DocumentCollection> CreateDocumentCollectionIfNotExistsAsync(DocumentClient client, string collectionId);
        Task<IList<T>> GetCourseCollectionDocumentsByUKPRN<T>(DocumentClient client, string collectionId, int UKPRN);
        Task<Provider> GetProviderByUKPRN(DocumentClient client, string collectionId, int UKPRN);
        Task<Document> UpdateDocumentAsync(DocumentClient client, string collectionId, object document);
        Task<IList<Provider>> GetOnboardedProviders(DocumentClient client, string collectionId);
        Task<Document> CreateDocumentAsync(DocumentClient client, string collectionId, object document);
        T DocumentTo<T>(Document document);
        Task<IList<T>> GetAllDocuments<T>(DocumentClient client, string collectionId);
    }
}
