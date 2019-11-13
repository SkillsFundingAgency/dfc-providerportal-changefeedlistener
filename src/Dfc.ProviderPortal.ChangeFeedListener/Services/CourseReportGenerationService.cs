using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public class CourseReportGenerationService : IReportGenerationService
    {
        private readonly ICosmosDbHelper _cosmosDbHelper;
        private readonly CosmosDbCollectionSettings _settings;

        public CourseReportGenerationService(ICosmosDbHelper cosmosDbHelper, CosmosDbCollectionSettings settings)
        {
            _cosmosDbHelper = cosmosDbHelper;
            _settings = settings;
        }

        public async Task UpdateReport(int ukprn)
        {
            using (var client = _cosmosDbHelper.GetClient())
            {
                var courses = await _cosmosDbHelper.GetCourseCollectionDocumentsByUKPRN<Course>(client, _settings.CoursesCollectionId,
                      ukprn);
                var migrationReport = (await _cosmosDbHelper.GetCourseCollectionDocumentsByUKPRN<CourseMigrationReport>(client,
                    _settings.CoursesMigrationReportCollectionId,
                    ukprn)).FirstOrDefault();
                var provider = await _cosmosDbHelper.GetProviderByUKPRN(client, _settings.ProviderCollectionId,
                     ukprn);


                if (provider == null || !HasValidReportOrCourses(courses, migrationReport))
                {
                    throw new Exception($"Unable to generate report for Provider: {ukprn}");
                }

                var report = new CourseReportDocument
                {
                    ProviderUKPRN = ukprn.ToString(),
                    MigrationPendingCount = courses.SelectMany(x => x.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.MigrationPending)).Count(),
                    MigrationReadyToGoLive = courses.SelectMany(x => x.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.MigrationReadyToGoLive)).Count(),
                    BulkUploadPendingcount = courses.SelectMany(x => x.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.BulkUloadPending)).Count(),
                    BulkUploadReadyToGoLiveCount = courses.SelectMany(x => x.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.BulkUploadReadyToGoLive)).Count(),
                    FailedMigrationCount = migrationReport?.LarslessCourses?.SelectMany(cr => cr.CourseRuns)?.Count(),
                    LiveCount = courses.SelectMany(c => c.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.Live)).Count(),
                    MigratedCount = migrationReport?.PreviousLiveCourseCount,
                    MigrationDate = migrationReport?.Timestamp,
                    MigrationRate = decimal.Round(MigrationRate(courses), 2, MidpointRounding.AwayFromZero),
                    ProviderName = provider.ProviderName,
                    ProviderType = provider.ProviderType

                };

                await _cosmosDbHelper.UpdateDocumentAsync(client, _settings.DfcReportCollectionId, report);
            }
        }
        
        public async Task GenerateAllReports(ILogger log)
        {
            var reportsList = new List<CourseMigrationReport>();

            using (var client = _cosmosDbHelper.GetClient())
            {
                var reports = await _cosmosDbHelper.GetAllDocuments<CourseMigrationReport>(client,
                    _settings.ApprenticeshipMigrationReportCollectionId);

                reportsList.AddRange(reports);
            }


            foreach (var report in reportsList)
            {
                try
                {
                    await UpdateReport(int.Parse(report.ProviderUKPRN.ToString()));
                }
                catch(Exception ex)
                {
                    if (!ex.Message.Contains("Unable to generate report for Provider"))
                    {

                        var t = ex;
                    }
                    log.LogWarning($"Unable to update Report for Provider{report.ProviderUKPRN.ToString()}");
                } 
            }
        }

        public async Task GenerateReportForProvider(int ukprn)
        {
            await UpdateReport(ukprn);
        }

        private decimal MigrationRate(IList<Course> courses)
        {
            var statusList = new List<RecordStatus>
            {
                RecordStatus.Live,
                RecordStatus.MigrationPending,
                RecordStatus.MigrationReadyToGoLive
            };

            if (courses.SelectMany(c => c.CourseRuns.Where(cr => statusList.Contains(cr.RecordStatus))).Any())
            {
                
                var liveCourses = (decimal)courses.SelectMany(c => c.CourseRuns.Where(cr => cr.RecordStatus == RecordStatus.Live)).Count();
                var migratedDataValue = ((decimal) courses
                    .SelectMany(c => c.CourseRuns.Where(cr => statusList.Contains(cr.RecordStatus))).Count());

                return ((liveCourses / migratedDataValue) * 100);
            }

            return 0;
        }

        private bool HasValidReportOrCourses(IList<Course> courses, CourseMigrationReport report)
        {
            return  (courses.Any() || report != null && report.LarslessCourses.Any());
        }
    }
}
