using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.ApprenticeshipMigrationReportTrigger
{
    public class ApprenticeshipMigrationReportTrigger
    {
        private const string DatabaseName = "%CosmosDatabaseId%";
        private const string CollectionName = "%ApprenticeshipMigrationReportCollectionId%";
        private const string ConnectionString = "CosmosDBConnectionString";
        private const string LeaseCollectionName = "%ApprenticeshipMigrationReportLeaseCollectionName%";
        private const string LeaseCollectionPrefix = "%ApprenticeshipMigrationReportLeaseCollectionPrefix%";
        private IReportGenerationService _reportGenerationService;
        
        [FunctionName("ApprenticeshipMigrationReportChangeFeedTrigger")]
        [Disable]
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
            _reportGenerationService = reportGenerationServiceResolver(ProcessType.Apprenticeship);
            
            ApprenticeshipMigrationReport apprenticeshipMigrationReport = null;

            foreach (var document in documents)
            {
                try
                {
                    apprenticeshipMigrationReport = (ApprenticeshipMigrationReport)((dynamic)document);

                    await _reportGenerationService.UpdateReport(apprenticeshipMigrationReport.ProviderUKPRN);

                }
                catch (Exception e)
                {
                    log.LogError($"Unable to Process document with id:  {GetResourceId(apprenticeshipMigrationReport, document)} ");
                }
            }
        }

        private string GetResourceId(ApprenticeshipMigrationReport apprenticeshipMigrationReport, Document document)
        {
            return apprenticeshipMigrationReport != null ? apprenticeshipMigrationReport.ProviderUKPRN.ToString() : document.ResourceId;
        }
    }
}
