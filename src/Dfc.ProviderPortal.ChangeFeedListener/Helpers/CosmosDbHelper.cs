using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class CosmosDbHelper : ICosmosDbHelper
    {
        private readonly CosmosDbSettings _settings;

        public CosmosDbHelper(CosmosDbSettings settings)
        {
            Throw.IfNull(settings, nameof(settings));

            _settings = settings;
        }
        public async Task<DocumentCollection> CreateDocumentCollectionIfNotExistsAsync(
            DocumentClient client,
            string collectionId)
        {
            Throw.IfNull(client, nameof(client));
            Throw.IfNullOrWhiteSpace(collectionId, nameof(collectionId));

            var uri = UriFactory.CreateDatabaseUri(_settings.DatabaseId);
            var coll = new DocumentCollection { Id = collectionId };

            return await client.CreateDocumentCollectionIfNotExistsAsync(uri, coll);
        }

        public DocumentClient GetClient()
        {
            return new DocumentClient(new Uri(_settings.EndpointUri), _settings.PrimaryKey);
        }

        public async Task<IList<T>> GetCourseCollectionDocumentsByUKPRN<T>(DocumentClient client, string collectionId, int UKPRN)
        {
            Throw.IfNull(client, nameof(client));
            Throw.IfNullOrWhiteSpace(collectionId, nameof(collectionId));
            Throw.IfNull(UKPRN, nameof(UKPRN));

            Uri uri = UriFactory.CreateDocumentCollectionUri(_settings.DatabaseId, collectionId);
            FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true, MaxItemCount = -1 };

            var list = new List<T>();
            var docs = client.CreateDocumentQuery<T>(uri, $"SELECT * FROM c WHERE c.ProviderUKPRN = {UKPRN}").AsDocumentQuery();

            while (docs.HasMoreResults)
            {
                var response = await docs.ExecuteNextAsync<T>();
                list.AddRange(response);
            }

            return list;
        }

        public async Task<Provider> GetProviderByUKPRN(DocumentClient client, string collectionId, int UKPRN)
        {
            Throw.IfNull(client, nameof(client));
            Throw.IfNullOrWhiteSpace(collectionId, nameof(collectionId));
            Throw.IfNull(UKPRN, nameof(UKPRN));

            Uri uri = UriFactory.CreateDocumentCollectionUri(_settings.DatabaseId, collectionId);
            FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true, MaxItemCount = -1 };

            var list = new List<Provider>();
            var docs = client.CreateDocumentQuery<Provider>(uri, $"SELECT * FROM c WHERE c.UnitedKingdomProviderReferenceNumber = \"{UKPRN}\"").AsDocumentQuery();

            var response = await docs.ExecuteNextAsync<Provider>();

            return response.FirstOrDefault();
        }

        public async Task<Document> UpdateDocumentAsync(
        DocumentClient client,
        string collectionId,
        object document)
        {
            Throw.IfNull(client, nameof(client));
            Throw.IfNullOrWhiteSpace(collectionId, nameof(collectionId));
            Throw.IfNull(document, nameof(document));

            var uri = UriFactory.CreateDocumentCollectionUri(
                _settings.DatabaseId,
                collectionId);

            return await client.UpsertDocumentAsync(uri, document);
        }

        public async Task<IList<Provider>> GetOnboardedProviders(DocumentClient client, string collectionId)
        {
            var providers = new List<Provider>();

            Uri uri = UriFactory.CreateDocumentCollectionUri(_settings.DatabaseId, collectionId);
            FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true, MaxItemCount = -1 };

            using (var queryable = client.CreateDocumentQuery<Provider>(uri, options).Where(x => x.Status == Status.Onboarded).AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    foreach (Provider provider in await queryable.ExecuteNextAsync<Provider>())
                    {
                        providers.Add(provider);
                    }
                }
            }

            return providers;
        }

        public async Task<IList<T>> GetAllDocuments<T>(DocumentClient client, string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(_settings.DatabaseId, collectionId);
            FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true, MaxItemCount = -1 };

            var documents = new List<T>();

            using (var queryable = client.CreateDocumentQuery<T>(uri, "Select * from c", options).AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    foreach (T doc in await queryable.ExecuteNextAsync<T>())
                    {
                        documents.Add(doc);
                    }
                }

            }


            return documents;
        }

        public async Task<Document> CreateDocumentAsync(DocumentClient client, string collectionId, object document)
        {
            Throw.IfNull(client, nameof(client));
            Throw.IfNullOrWhiteSpace(collectionId, nameof(collectionId));
            Throw.IfNull(document, nameof(document));

            var uri = UriFactory.CreateDocumentCollectionUri(
                _settings.DatabaseId,
                collectionId);

            return await client.CreateDocumentAsync(uri, document);
        }

        public T DocumentTo<T>(Document document)
        {
            Throw.IfNull(document, nameof(document));
            return (T)(dynamic)document;
        }
    }
}
