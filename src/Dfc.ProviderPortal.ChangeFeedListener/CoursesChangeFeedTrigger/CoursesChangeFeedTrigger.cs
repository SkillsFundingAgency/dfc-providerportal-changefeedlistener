using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

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
                LeaseCollectionPrefix = LeaseCollectionPrefix,
                CreateLeaseCollectionIfNotExists = true
            )]
            IReadOnlyList<Document> documents,
            ILogger log,
            [Inject] IReportGenerationService reportGenerationService)
        {
            Course course = null;

            foreach (var document in documents)
            {
                try
                {
                    course = (Course)((dynamic)document);

                    await reportGenerationService.UpdateReport(course.ProviderUKPRN);

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
