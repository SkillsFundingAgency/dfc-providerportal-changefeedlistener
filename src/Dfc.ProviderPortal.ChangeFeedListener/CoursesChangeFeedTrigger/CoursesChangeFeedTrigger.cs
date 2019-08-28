using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.CoursesChangeFeedTrigger
{
    public class CoursesChangeFeedTrigger
    {
        private const string DatabaseName = "%CosmosDatabaseId%";
        private const string CollectionName = "%CoursesCollectionId%";
        private const string ConnectionString = "CosmosDBConnectionString";
        private const string LeaseCollectionName = "%CoursesLeaseCollectionName%";
        private const string LeaseCollectionPrefix = "%CoursesLeaseCollectionPrefix%";

        [FunctionName("CourseChangeFeedTrigger")]
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
            [Inject] IReportGenerationService reportGenerationService,
            [Inject] ICourseAuditService courseAuditService)
        {
            Course course = null;

            try
            {  // Index documents
                log.LogInformation("Entered FindACourseSearchCosmosTrigger");
                log.LogInformation($"Processing {documents.Count} documents for indexing to Azure search");
                IEnumerable<IndexingResult> results = await courseAuditService.UploadCoursesToSearch(log, documents);

                // Audit changes to documents
                CourseAudit ca = null;
                log.LogInformation($"Auditing changes to {documents.Count} documents");
                foreach (var document in documents)
                {
                    ca = await courseAuditService.Audit(log, document);

                    course = (Course)((dynamic)document);
                    await reportGenerationService.UpdateReport(course.ProviderUKPRN);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Indexing error in FindACourseSearchCosmosTrigger");
            }

        }

        private string GetResourceId(Course course, Document document)
        {
            return course != null ? course.ProviderUKPRN.ToString() : document.ResourceId;
        }
    }
}
