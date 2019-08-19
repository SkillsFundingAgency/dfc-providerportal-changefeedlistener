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

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public class CourseAuditService : ICourseAuditService
    {
       private readonly ISearchServiceSettings _searchServiceSettings;
       private readonly IVenueServiceSettings _venueServiceSettings;
       private readonly IProviderServiceSettings _providerServiceSettings;
       private readonly ICosmosDbHelper _cosmosDbHelper;
       private readonly CosmosDbCollectionSettings _settings;

       public CourseAuditService(IOptions<SearchServiceSettings> searchServiceSettings, IOptions<VenueServiceSettings> venueServiceSettings,
           IOptions<ProviderServiceSettings> providerServiceSettings, ICosmosDbHelper cosmosDbHelper, CosmosDbCollectionSettings settings)
       {
           _searchServiceSettings = searchServiceSettings.Value;
           _venueServiceSettings = venueServiceSettings.Value;
           _providerServiceSettings = providerServiceSettings.Value;
           _cosmosDbHelper = cosmosDbHelper;
           _settings = settings;
       }

       public async Task<IEnumerable<IndexingResult>> UploadCoursesToSearch(ILogger log, IReadOnlyList<Document> documents)
        {
            if (documents.Any())
            {

                log.LogInformation("Getting provider data");
                IEnumerable<AzureSearchProviderModel> providers = new ProviderServiceWrapper(_providerServiceSettings, new HttpClient()).GetLiveProvidersForAzureSearch();

                IEnumerable<AzureSearchVenueModel> venues = GetVenues(
                    log,
                    documents.Select(d => new Course() { CourseRuns = d.GetPropertyValue<IEnumerable<CourseRun>>("CourseRuns") })
                        .SelectMany(c => c.CourseRuns)
                );

                return new SearchServiceWrapper(log, _searchServiceSettings)
                    .UploadBatch(providers, venues, documents, out int succeeded);
            }
            else
            {
                // Return empty list of failed IndexingResults
                return new List<IndexingResult>();
            }
        }

        private IEnumerable<AzureSearchVenueModel> GetVenues(ILogger log, IEnumerable<CourseRun> runs = null)
        {
            IVenueServiceWrapper service = new VenueServiceWrapper(_venueServiceSettings);
            IEnumerable<CourseRun> venueruns = runs?.Where(x => x.VenueId != null);
            log.LogInformation($"Getting data for { venueruns?.Count().ToString() ?? "all" } venues");

            // Get all venues to save time & RUs if there's too many to get by Id
            if (venueruns == null || venueruns.Count() > _searchServiceSettings.ThresholdVenueCount)
                return service.GetVenues();
            else
            {
                List<AzureSearchVenueModel> venues = new List<AzureSearchVenueModel>();
                foreach (CourseRun r in venueruns)
                    if (!venues.Any(x => x.id == r.VenueId.Value))
                    {
                        AzureSearchVenueModel venue = service.GetById<AzureSearchVenueModel>(r.VenueId.Value);
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
    }
}
