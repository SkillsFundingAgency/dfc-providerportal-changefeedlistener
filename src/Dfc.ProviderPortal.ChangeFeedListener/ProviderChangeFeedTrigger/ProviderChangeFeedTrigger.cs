
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Document = Microsoft.Azure.Documents.Document;


namespace Dfc.ProviderPortal.ChangeFeedListener.ProviderChangeFeedTrigger
{
    public class ProviderChangeFeedTrigger
    {
        private const string DatabaseName = "%CosmosDatabaseId%";
        private const string CollectionName = "%ProviderCollectionId%";
        private const string ConnectionString = "CosmosDBConnectionString";
        private const string LeaseCollectionName = "%ProviderLeaseCollectionName%";
        private const string LeaseCollectionPrefix = "%ProviderLeaseCollectionPrefix%";

        [FunctionName("ProviderChangeFeedTrigger")]
        [Disable]
        public async Task Run([CosmosDBTrigger(
                DatabaseName,
                CollectionName,
                ConnectionStringSetting = ConnectionString,
                LeaseCollectionName = LeaseCollectionName,
                //LeaseCollectionPrefix = LeaseCollectionPrefix,
                CreateLeaseCollectionIfNotExists = true
            )]
            IReadOnlyList<Document> documents,
            ILogger log,
            [Inject] ICourseArchiveService courseArchiveService)
        {
            try {
                // Archive courses for deactivated providers
                log.LogInformation("Entered ProviderChangeFeedTrigger");
                log.LogInformation($"Processing {documents.LongCount()} provider documents for course archiving");
                IEnumerable<Document> deactivatedDocs = documents.Where(d => d.GetPropertyValue<string>("ProviderStatus") == "PD1"
                                                                          || d.GetPropertyValue<string>("ProviderStatus") == "PD2");
                log.LogInformation($"Processing {deactivatedDocs.LongCount()} provider documents with status PD1/PD2");

                IEnumerable<Document> results = new List<Document>();
                if (deactivatedDocs.Any())
                    results = await courseArchiveService.ArchiveAllCourses(log, deactivatedDocs);

            } catch (Exception e) {
                log.LogError(e, "Indexing error in ProviderChangeFeedTrigger");
            }
        }
    }
}
