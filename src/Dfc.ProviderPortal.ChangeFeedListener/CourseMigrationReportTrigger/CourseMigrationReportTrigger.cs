using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Services;


namespace Dfc.ProviderPortal.ChangeFeedListener.CourseMigrationReportTrigger
{
    public class CourseMigrationReportTrigger
    {
        private const string DatabaseName = "%CosmosDatabaseId%";
        private const string CollectionName = "%CoursesMigrationReportCollectionId%";
        private const string ConnectionString = "CosmosDBConnectionString";
        private const string LeaseCollectionName = "%CoursesMigrationReportLeaseCollectionName%";
        private const string LeaseCollectionPrefix = "%CoursesMigrationReportLeaseCollectionPrefix%";
        private readonly IReportGenerationService _reportGenerationService;

        public CourseMigrationReportTrigger(ReportGenerationServiceResolver reportGenerationServiceResolver)
        {
            _reportGenerationService = reportGenerationServiceResolver(ProcessType.Course);
        }

        [FunctionName("CourseMigrationReportChangeFeedTrigger")]
        public async Task Run([CosmosDBTrigger(
                DatabaseName,
                CollectionName,
                ConnectionStringSetting = ConnectionString,
                LeaseCollectionName = LeaseCollectionName,
                LeaseCollectionPrefix = LeaseCollectionPrefix,
                CreateLeaseCollectionIfNotExists = true
            )]
            IReadOnlyList<Document> documents,
            ILogger log,
            [Inject] ReportGenerationServiceResolver reportGenerationServiceResolver)
        {
            Course course = null;

            foreach (var document in documents)
            {
                try
                {
                    var courseMigrationReport = (CourseMigrationReport)((dynamic)document);

                    await _reportGenerationService.UpdateReport(courseMigrationReport.ProviderUKPRN);

                }
                catch (Exception e)
                {
                    log.LogError($"Unable to Process document with id:  {GetResourceId(course, document)} ");
                }
            }
        }

        private string GetResourceId(Course course, Document document)
        {
            return course != null ? course.ProviderUKPRN.ToString() : document.ResourceId;
        }
    }
}
