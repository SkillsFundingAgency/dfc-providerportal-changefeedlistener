using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public class ApprenticeshipReportGenerationService : IReportGenerationService
    {
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly CosmosDbCollectionSettings _settings;

        public ApprenticeshipReportGenerationService(ICosmosDbHelper cosmosDbHelper, CosmosDbCollectionSettings settings)
        {
            _cosmosDbHelper = cosmosDbHelper;
            _settings = settings;
        }

        public async Task UpdateReport(int ukprn)
        {
            using (var client = _cosmosDbHelper.GetClient())
            {
                var apprenticeships = await _cosmosDbHelper.GetCourseCollectionDocumentsByUKPRN<Apprenticeship>(client, _settings.ApprenticeshipCollectionId,
                      ukprn);
                var migrationReport = (await _cosmosDbHelper.GetCourseCollectionDocumentsByUKPRN<ApprenticeshipMigrationReport>(client,
                    _settings.ApprenticeshipMigrationReportCollectionId,
                    ukprn)).FirstOrDefault();
                var provider = await _cosmosDbHelper.GetProviderByUKPRN(client, _settings.ProviderCollectionId,
                     ukprn);


                if (provider == null || !HasValidReportOrApprenticeships(apprenticeships, migrationReport))
                {
                    throw new Exception($"Unable to generate report for Provider: {ukprn}");
                }

                var report = new ApprenticeshipDfcReportDocument()
                {
                    ProviderUKPRN = ukprn.ToString(),
                    MigrationPendingCount = apprenticeships.SelectMany(x => x.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.MigrationPending)).Count(),
                    MigrationReadyToGoLive = apprenticeships.SelectMany(x => x.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.MigrationReadyToGoLive)).Count(),
                    BulkUploadPendingcount = apprenticeships.SelectMany(x => x.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.BulkUloadPending)).Count(),
                    BulkUploadReadyToGoLiveCount = apprenticeships.SelectMany(x => x.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.BulkUploadReadyToGoLive)).Count(),
                    FailedMigrationCount = migrationReport?.NotTransferred,
                    LiveCount = apprenticeships.SelectMany(c => c.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.Live)).Count(),
                    MigratedCount = migrationReport?.ApprenticeshipsMigrated,
                    MigrationDate = migrationReport?.MigrationDate,
                    MigrationRate = decimal.Round(MigrationRate(apprenticeships), 2, MidpointRounding.AwayFromZero),
                    ProviderName = provider.ProviderName,
                    ProviderType = provider.ProviderType

                };

                await _cosmosDbHelper.UpdateDocumentAsync(client, _settings.ApprenticeshipDfcReportCollection, report);
            }
        }
        
        public async Task GenerateAllReports(ILogger log)
        {
            var reportsList = new List<ApprenticeshipMigrationReport>();
            using (var client = _cosmosDbHelper.GetClient())
            {
                var reports = await _cosmosDbHelper.GetAllDocuments<ApprenticeshipMigrationReport>(client,
                    _settings.ApprenticeshipMigrationReportCollectionId);

                reportsList.AddRange(reports);
            }

            foreach (var report in reportsList)
            {
                try
                {
                    await UpdateReport(int.Parse(report.ProviderUKPRN.ToString()));
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Unable to generate report for Provider"))
                    {

                        var t = ex;
                    }
                    log.LogWarning($"Unable to update Report for Provider{report.ProviderUKPRN}");
                }
            }
        }

        public async Task GenerateReportForProvider(int ukprn)
        {
            await UpdateReport(ukprn);
        }

        private decimal MigrationRate(IList<Apprenticeship> apprenticeships)
        {
            var statusList = new List<RecordStatus>
            {
                RecordStatus.Live,
                RecordStatus.MigrationPending,
                RecordStatus.MigrationReadyToGoLive
            };

            if (apprenticeships.SelectMany(c => c.ApprenticeshipLocations.Where(cr => statusList.Contains(cr.RecordStatus))).Any())
            {

                var liveCourses = (decimal)apprenticeships.SelectMany(c => c.ApprenticeshipLocations.Where(cr => cr.RecordStatus == RecordStatus.Live)).Count();
                var migratedDataValue = ((decimal)apprenticeships
                    .SelectMany(c => c.ApprenticeshipLocations.Where(cr => statusList.Contains(cr.RecordStatus))).Count());

                return ((liveCourses / migratedDataValue) * 100);
            }

            return 0;
        }

        private bool HasValidReportOrApprenticeships(IList<Apprenticeship> apprenticeships, ApprenticeshipMigrationReport report)
        {
            return (apprenticeships.Any() || report != null && report.ApprenticeshipsMigrated > 0);
        }
    }
}
