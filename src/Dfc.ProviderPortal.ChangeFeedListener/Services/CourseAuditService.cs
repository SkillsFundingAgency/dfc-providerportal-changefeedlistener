using System;
using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using Document = Microsoft.Azure.Documents.Document;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public class CourseAuditService : ICourseAuditService
    {
       private readonly ISearchServiceSettings _searchServiceSettings;
       private readonly IVenueServiceSettings _venueServiceSettings;
       private readonly IProviderServiceSettings _providerServiceSettings;
       private readonly ICosmosDbHelper _cosmosDbHelper;
       private readonly CosmosDbSettings _cosmosDbSettings;
       private readonly CosmosDbCollectionSettings _settings;

        public CourseAuditService(IOptions<SearchServiceSettings> searchServiceSettings, IOptions<VenueServiceSettings> venueServiceSettings,
           IOptions<ProviderServiceSettings> providerServiceSettings, ICosmosDbHelper cosmosDbHelper, CosmosDbCollectionSettings settings, CosmosDbSettings cosmosDbSettings)
       {
           _searchServiceSettings = searchServiceSettings.Value;
           _venueServiceSettings = venueServiceSettings.Value;
           _providerServiceSettings = providerServiceSettings.Value;
           _cosmosDbHelper = cosmosDbHelper;
           _settings = settings;
            _cosmosDbSettings = cosmosDbSettings;
       }

       public async Task UploadCoursesToSearch(ILogger log, IReadOnlyList<Document> documents)
        {
            if (documents.Count == 0)
            {
                return;
            }

            log.LogInformation("Getting provider data");
            IEnumerable<AzureSearchProviderModel> providers = new ProviderServiceWrapper(_providerServiceSettings, new HttpClient()).GetLiveProvidersForAzureSearch();

            IEnumerable<AzureSearchVenueModel> venues = await GetVenues(
                log,
                documents.Select(d => new Course() { CourseRuns = d.GetPropertyValue<IEnumerable<CourseRun>>("CourseRuns") })
                    .SelectMany(c => c.CourseRuns)
            );

            await new SearchServiceWrapper(log, _searchServiceSettings)
                .UploadBatch(
                    updateBatchId: Guid.NewGuid(),
					providers
                        .GroupBy(g => g.UnitedKingdomProviderReferenceNumber, g => g)
                        .ToDictionary(p => p.Key, p => p.First()),
                    venues.ToDictionary(v => v.id.Value, v => v),
                    documents);
        }

        private async Task<IEnumerable<AzureSearchVenueModel>> GetVenues(ILogger log, IEnumerable<CourseRun> runs = null)
        {
            IVenueServiceWrapper service = new VenueServiceWrapper(_venueServiceSettings);
            IEnumerable<CourseRun> venueruns = runs?.Where(x => x.VenueId != null);
            log.LogInformation($"Getting data for { venueruns?.Count().ToString() ?? "all" } venues");

            // Get all venues to save time & RUs if there's too many to get by Id
            if (venueruns == null || venueruns.Count() > _searchServiceSettings.ThresholdVenueCount)
                return await service.GetVenues();
            else
            {
                List<AzureSearchVenueModel> venues = new List<AzureSearchVenueModel>();
                foreach (CourseRun r in venueruns)
                    if (!venues.Any(x => x.id == r.VenueId.Value))
                    {
                        AzureSearchVenueModel venue = await service.GetById<AzureSearchVenueModel>(r.VenueId.Value);
                        if (venue != null)
                            venues.Add(venue);
                    }
                log.LogInformation($"Successfully retrieved data for {venues.Count()} venues");
                return venues;
            }
        }

        public async Task<CourseAudit> Audit(ILogger log, Document auditee)
        {
            Throw.IfNull(auditee, nameof(auditee));
            Throw.IfNull(auditee.GetPropertyValue<Guid>("id"), "Document id");
            CourseAudit persisted;

            Guid id = auditee.GetPropertyValue<Guid>("id");

            try
            {

                log.LogInformation($"Writing audit for course { id }");
                using (var client = _cosmosDbHelper.GetClient())
                {
                    await _cosmosDbHelper.CreateDocumentCollectionIfNotExistsAsync(client, _settings.AuditCollectionId);
                    Document doc = await _cosmosDbHelper.CreateDocumentAsync(client, _settings.AuditCollectionId,
                        new CourseAudit()
                        {
                            id = Guid.NewGuid(),
                            Collection = _settings.CoursesCollectionId,
                            DocumentId = id.ToString(),
                            UpdatedBy = auditee.GetPropertyValue<string>("UpdatedBy") ?? auditee.GetPropertyValue<string>("CreatedBy"),
                            UpdatedDate = auditee.GetPropertyValue<DateTime?>("UpdatedDate") ?? DateTime.Now,
                            Document = auditee
                        });
                    persisted = _cosmosDbHelper.DocumentTo<CourseAudit>(doc);
                }
                return persisted;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task RepopulateSearchIndex(ILogger log, bool deleteZombies)
        {
            var allProviders = new ProviderServiceWrapper(_providerServiceSettings, new HttpClient())
                .GetLiveProvidersForAzureSearch()
                .GroupBy(p => p.UnitedKingdomProviderReferenceNumber)
                .ToDictionary(g => g.Key, g => g.First());

            var venueService = new VenueServiceWrapper(_venueServiceSettings);
            var allVenues = (await venueService.GetVenues())
                .ToDictionary(v => v.id.Value, v => v);

            var searchServiceWrapper = new SearchServiceWrapper(log, _searchServiceSettings);

            var updateBatchId = Guid.NewGuid();

            var indexedCount = 0;
            using (var client = _cosmosDbHelper.GetClient())
            {
                var collectionLink = UriFactory.CreateDocumentCollectionUri(
                    _cosmosDbSettings.DatabaseId,
                    _settings.CoursesCollectionId);

                // N.B. We deliberately don't filter out non-live courses here;
                // UploadBatch needs to be passed those documents so it can remove them from the index
                var query = client.CreateDocumentQuery<Course>(
                    collectionLink,
                    new FeedOptions() { EnableCrossPartitionQuery = true })
                    .AsDocumentQuery();

                var indexedIds = new List<string>();

                while (query.HasMoreResults)
                {
                    var result = await query.ExecuteNextAsync<Document>();

                    var indexed = await searchServiceWrapper.UploadBatch(updateBatchId, allProviders, allVenues, result);
                    indexedIds.AddRange(indexed);
                    indexedCount += indexed.Count;
                }

                if (deleteZombies)
                {
                    // Any records that haven't just been updated are zombies - go forth and reap
                    await searchServiceWrapper.RemoveSearchRecordsNotUpdatedInBatch(
                        updateBatchId,
                        indexedIds);
                }
            }

            log.LogInformation($"Indexed {indexedCount} records.");
        }
    }
}
