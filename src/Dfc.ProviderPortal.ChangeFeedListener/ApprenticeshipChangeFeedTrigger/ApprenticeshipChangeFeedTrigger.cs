using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.ApprenticeshipChangeFeedTrigger
{
    public class ApprenticeshipChangeFeedTrigger
    {
        private const string DatabaseName = "%CosmosDatabaseId%";
        private const string CollectionName = "%ApprenticeshipCollectionId%";
        private const string ConnectionString = "CosmosDBConnectionString";
        private const string LeaseCollectionName = "%ApprenticeshipLeaseCollectionName%";
        private const string LeaseCollectionPrefix = "%ApprenticeshipsLeaseCollectionPrefix%";

        [FunctionName("ApprenticeshipChangeFeedTrigger")]
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
            [Inject] ReportGenerationServiceResolver reportGenerationServiceResolver,
            [Inject] ICourseAuditService courseAuditService)
        {
            try
            {
                var reportGenerationService = reportGenerationServiceResolver(Models.ProcessType.Apprenticeship);

                // Index documents
                log.LogInformation("Entered ApprenticeshipChangeFeedTrigger");

                // Audit changes to documents
                // Generate report data
                foreach (string UKPRN in documents.Select(d => d.GetPropertyValue<string>("ProviderUKPRN"))
                                                  .Distinct())
                {
                    log.LogInformation($"Generating report data for provider {UKPRN}");
                    await reportGenerationService.UpdateReport(int.Parse(UKPRN));
                }

            }
            catch (Exception e)
            {
                log.LogError(e, "Indexing error in ApprenticeshipChangeFeedTrigger");
            }

        }
    }
}
