using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.Packages;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Spatial;
using Document = Microsoft.Azure.Documents.Document;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class SearchServiceWrapper : ISearchServiceWrapper
    {
        public class LINQComboClass
        {
            public Course Course { get; set; }
            public CourseRun Run { get; set; }
            public SubRegionItemModel SubRegion { get; set; }
            public AzureSearchVenueModel Venue { get; set; }
        }

        private readonly ILogger _log;
        private readonly ISearchServiceSettings _settings;
        private static SearchServiceClient _adminService;
        private static ISearchIndexClient _adminIndex;

        public SearchServiceWrapper(
            ILogger log,
            ISearchServiceSettings settings)
        {
            Throw.IfNull(log, nameof(log));
            Throw.IfNull(settings, nameof(settings));

            _log = log;
            _settings = settings;

            _adminService = new SearchServiceClient(settings.SearchService, new SearchCredentials(settings.AdminKey));
            _adminIndex = _adminService?.Indexes?.GetClient(settings.Index);
        }

        public async Task<IReadOnlyList<string>> UploadBatch(
            Guid updateBatchId,
            IDictionary<int, AzureSearchProviderModel> providers,
            IDictionary<Guid, AzureSearchVenueModel> venues,
            IEnumerable<Document> courseDocuments)
        {
            try
            {
                if (courseDocuments.Any())
                {
                    IEnumerable<Course> courses = courseDocuments.Select(d => new Course()
                    {
                        id = d.GetPropertyValue<Guid>("id"),
                        QualificationCourseTitle = d.GetPropertyValue<string>("QualificationCourseTitle"),
                        LearnAimRef = d.GetPropertyValue<string>("LearnAimRef"),
                        NotionalNVQLevelv2 = d.GetPropertyValue<string>("NotionalNVQLevelv2"),
                        UpdatedDate = d.GetPropertyValue<DateTime?>("UpdatedDate"),
                        ProviderUKPRN = int.Parse(d.GetPropertyValue<string>("ProviderUKPRN")),
                        CourseDescription = d.GetPropertyValue<string>("CourseDescription"),
                        CourseRuns = d.GetPropertyValue<IEnumerable<CourseRun>>("CourseRuns")
                    });

                    _log.LogInformation("Creating batch of course data to index");

                    // Classroom courses have an associated venue
                    IEnumerable<LINQComboClass> classroom = from Course c in courses
                                                            from CourseRun r in c.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.ClassroomBased) ?? new List<CourseRun>()
                                                            let v = r.VenueId.HasValue && venues.ContainsKey(r.VenueId.Value) ? venues[r.VenueId.Value] : null
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = (SubRegionItemModel)null, Venue = v };
                    _log.LogInformation($"{classroom.Count()} classroom courses (with VenueId and no regions)");


                    // Work-based courses have regions instead
                    IEnumerable<LINQComboClass> workbased = from Course c in courses
                                                            from CourseRun r in c.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.WorkBased && !x.National.GetValueOrDefault(false)) ?? new List<CourseRun>()
                                                            from SubRegionItemModel subregion in r.SubRegions ?? new List<SubRegionItemModel>()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = subregion, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{workbased.Count()} regional work-based courses (with regions but no VenueId)");

                    // Except for ones with National flag set, where Subregions is null
                    IEnumerable<LINQComboClass> national =  from Course c in courses
                                                            from CourseRun r in c.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.WorkBased && x.National.GetValueOrDefault(false)) ?? new List<CourseRun>()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = new SubRegionItemModel() { SubRegionName = "National" }, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{national.Count()} national work-based courses (with no regions or VenueId)");


                    // Online courses have neither
                    IEnumerable<LINQComboClass> online =    from Course c in courses
                                                            from CourseRun r in c.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.Online) ?? new List<CourseRun>()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = (SubRegionItemModel)null, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{online.Count()} online courses (with no region or VenueId)");


                    decimal regionBoost = _settings.RegionSearchBoost ?? 2.3M;
                    decimal subregionBoost = _settings.SubRegionSearchBoost ?? 4.5M;

                    var combined = classroom.Union(workbased).Union(national).Union(online).ToList();

                    var missingProvider = combined
                        .Select(x => x.Course.ProviderUKPRN)
                        .Distinct()
                        .Where(ukprn => !providers.ContainsKey(ukprn))
                        .ToList();
                    if (missingProvider.Count > 0)
                    {
                        _log.LogWarning($"Missing providers: ${string.Join(", ", missingProvider)}.");
                    }

                    IEnumerable<AzureSearchCourse> batchdata = (from LINQComboClass x in combined
                                                                where providers.ContainsKey(x.Course.ProviderUKPRN)
                                                                let p = providers[x.Course.ProviderUKPRN]
                                                                where (x.Run.RecordStatus == RecordStatus.Live && (x.Run.DeliveryMode == DeliveryMode.Online || x.Venue != null || x.SubRegion != null))
                                                                let id = x.Run.DeliveryMode == DeliveryMode.WorkBased ? $"{x.Run.id}--{x.SubRegion.Id}" : x.Run.id.ToString()
                                                                let venueLocation = !((x.Venue?.Latitude ?? 0) == 0 && (x.Venue?.Longitude ?? 0) == 0)?
                                                                    GeographyPoint.Create(x.Venue.Latitude.Value, x.Venue.Longitude.Value) :
                                                                    x.SubRegion != null ? GeographyPoint.Create(x.SubRegion.Latitude, x.SubRegion.Longitude) :
                                                                    null
                                                                select new AzureSearchCourse()
                                                                {
                                                                    id = id,
                                                                    CourseId = x.Course.id,
                                                                    CourseRunId = x.Run.id,
                                                                    QualificationCourseTitle = x.Course?.QualificationCourseTitle,
                                                                    LearnAimRef = x.Course?.LearnAimRef,
                                                                    NotionalNVQLevelv2 = x.Course?.NotionalNVQLevelv2,
                                                                    VenueName = x.Venue?.VENUE_NAME ?? "",
                                                                    VenueAddress = string.Format("{0}{1}{2}{3}{4}",
                                                                                   string.IsNullOrWhiteSpace(x.Venue?.ADDRESS_1) ? "" : x.Venue?.ADDRESS_1 + ", ",
                                                                                   string.IsNullOrWhiteSpace(x.Venue?.ADDRESS_2) ? "" : x.Venue?.ADDRESS_2 + ", ",
                                                                                   string.IsNullOrWhiteSpace(x.Venue?.TOWN) ? "" : x.Venue?.TOWN + ", ",
                                                                                   string.IsNullOrWhiteSpace(x.Venue?.COUNTY) ? "" : x.Venue?.COUNTY + ", ",
                                                                                   x.Venue?.POSTCODE) ?? "",
                                                                    VenueAttendancePattern = x.Run.DeliveryMode == DeliveryMode.ClassroomBased ? ((int)x.Run.AttendancePattern).ToString() : null,
                                                                    VenueAttendancePatternDescription = x.Run.DeliveryMode == DeliveryMode.ClassroomBased ? x.Run.AttendancePattern.Description() : null,
                                                                    VenueLocation = venueLocation,
                                                                    UKPRN = x.Course.ProviderUKPRN.ToString(),
                                                                    ProviderName = p.ProviderName,
                                                                    Region = x.SubRegion?.SubRegionName ?? "",
                                                                    Status = (int?)x.Run.RecordStatus,
                                                                    ScoreBoost = (x.SubRegion == null || x.Run.National.GetValueOrDefault(false) || x.SubRegion?.Weighting == SearchResultWeightings.Low ? 1
                                                                                    : (x.SubRegion?.Weighting == SearchResultWeightings.High ? subregionBoost : regionBoost)
                                                                                 ),
                                                                    VenueTown = x.Venue?.TOWN,
                                                                    VenueStudyMode = ((int)x.Run.StudyMode).ToString(),
                                                                    VenueStudyModeDescription = x.Run.StudyMode.Description(),
                                                                    DeliveryMode = ((int)x.Run.DeliveryMode).ToString(),
                                                                    DeliveryModeDescription = x.Run.DeliveryMode.Description(),
                                                                    Cost = (x.Run.Cost == null ? (int?)null : Convert.ToInt32(x.Run.Cost)),
                                                                    CostDescription = x.Run.CostDescription,
                                                                    StartDate = !x.Run.FlexibleStartDate ? x.Run.StartDate : null,
                                                                    CourseText = x.Course?.CourseDescription,
                                                                    UpdatedOn = x.Run.UpdatedDate ?? x.Run.CreatedDate,
                                                                    CourseDescription = x.Course?.CourseDescription,
                                                                    CourseName = x.Run.CourseName,
                                                                    FlexibleStartDate = x.Run.FlexibleStartDate,
                                                                    DurationUnit = x.Run.DurationUnit,
                                                                    National = x.Run.National,
                                                                    UpdateBatchId = updateBatchId
                                                                }).ToList();

                    IReadOnlyList<string> indexedIds;
                    if (batchdata.Any())
                    {
                        IndexBatch<AzureSearchCourse> batch = IndexBatch.MergeOrUpload(batchdata);

                        _log.LogInformation("Merging docs to azure search index: course");
                        var indexResult = await _adminIndex.Documents.IndexAsync(batch);

                        var succeeded = indexResult.Results.Count(r => r.Succeeded);
                        _log.LogInformation($"*** Successfully merged {succeeded} docs into Azure search index: course");

                        indexedIds = batchdata.Select(c => c.id).ToArray();
                    }
                    else
                    {
                        indexedIds = Array.Empty<string>();
                    }

                    var courseIds = courseDocuments.Select(d => d.GetPropertyValue<Guid>("id"));
                    var insertedIds = batchdata.Select(d => d.id);
                    await DeleteStaleDocumentsForCourses(updateBatchId, courseIds, insertedIds);

                    var coursesWithNoLiveCourseRuns = courseDocuments
                        .Where(d => (d.GetPropertyValue<int>("CourseStatus") & 1) == 0)
                        .Select(d => d.GetPropertyValue<Guid>("id"));
                    await DeleteDocumentsForCourses(coursesWithNoLiveCourseRuns);

                    return indexedIds;
                }
                else
                {
                    return Array.Empty<string>();
                }
            }
            catch (IndexBatchException ex)
            {
                IEnumerable<IndexingResult> failed = ex.IndexingResults.Where(r => !r.Succeeded);
                _log.LogError(ex, string.Format("Failed to index some of the documents: {0}",
                                                string.Join(", ", failed.Select(d => d.Key))));
                throw;
            }
        }

        private async Task DeleteDocumentsForCourses(IEnumerable<Guid> courseIds)
        {
            if (!courseIds.Any())
            {
                return;
            }

            var queryParams = new SearchParameters()
            {
                SearchMode = SearchMode.All,
                QueryType = QueryType.Full,
                Select = new List<string>() { "id" }
            };
            var docs = await _adminIndex.Documents.SearchAsync<dynamic>($"CourseId:({string.Join(" || ", courseIds)})", queryParams);

            while (docs.Results.Count > 0)
            {
                var toBeDeleted = docs.Results.Select(d => (string)d.Document.id).ToList();

                var deleteBatch = IndexBatch.Delete("id", toBeDeleted);
                var deleteResult = await _adminIndex.Documents.IndexAsync(deleteBatch);

                if (docs.ContinuationToken != null)
                {
                    docs = await _adminIndex.Documents.ContinueSearchAsync<dynamic>(docs.ContinuationToken);
                }
                else
                {
                    break;
                }
            }
        }

        private async Task DeleteStaleDocumentsForCourses(
            Guid thisBatchId,
            IEnumerable<Guid> courseIds,
            IEnumerable<string> insertedIds)
        {
            // We need to delete any records for the courses passed in that haven't been re-indexed above
            // e.g. course run(s) or region(s) have been removed.
            //
            // There's no delete-by-query method so we need to do a search then delete

            var queryParams = new SearchParameters()
            {
                Filter = $"UpdateBatchId ne '{thisBatchId}'",
                SearchMode = SearchMode.All,
                QueryType = QueryType.Full,
                Select = new List<string>() { "id" }
            };

            var courseIdList = courseIds.ToList();
            var index = 0;
            var chunkSize = 3000;  // Max filters per query supported by Azure Search

            while (index < courseIdList.Count)
            {
                var chunkIds = courseIdList.Skip(index).Take(chunkSize).ToList();
                index += chunkIds.Count;

                var docs = await _adminIndex.Documents.SearchAsync<dynamic>(
                    $"CourseId:({string.Join(" || ", chunkIds)})", queryParams);

                while (docs.Results.Count > 0)
                {
                    // Azure Search is eventually consistent so we occasionally see stale results here
                    // that have actually been updated in the current batch.
                    // We need to make sure we don't delete them.
                    var toBeDeleted = docs.Results.Select(d => (string)d.Document.id)
                        .Except(insertedIds)
                        .ToList();

                    if (toBeDeleted.Any())
                    {
                        var deleteBatch = IndexBatch.Delete("id", toBeDeleted);
                        var deleteResult = await _adminIndex.Documents.IndexAsync(deleteBatch);
                    }

                    if (docs.ContinuationToken != null)
                    {
                        docs = await _adminIndex.Documents.ContinueSearchAsync<dynamic>(docs.ContinuationToken);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public async Task RemoveSearchRecordsNotUpdatedInBatch(
            Guid updateBatchId,
            IReadOnlyCollection<string> indexedIds)
        {
            // There's no delete-by-query method so we need to do a search then delete

            var queryParams = new SearchParameters()
            {
                Filter = $"UpdateBatchId ne '{updateBatchId}'",
                SearchMode = SearchMode.All,
                QueryType = QueryType.Full,
                Select = new List<string>() { "id" }
            };

            var docs = await _adminIndex.Documents.SearchAsync<dynamic>($"*", queryParams);

            while (docs.Results.Count > 0)
            {
                // Azure Search is eventually consistent so we occasionally see stale results here
                // that have actually been updated in the current batch.
                // We need to make sure we don't delete them.
                var toBeDeleted = docs.Results.Select(d => (string)d.Document.id)
                    .Except(indexedIds)
                    .ToList();

                if (toBeDeleted.Any())
                {
                    var deleteBatch = IndexBatch.Delete("id", toBeDeleted);
                    var deleteResult = await _adminIndex.Documents.IndexAsync(deleteBatch);
                }

                if (docs.ContinuationToken != null)
                {
                    docs = await _adminIndex.Documents.ContinueSearchAsync<dynamic>(docs.ContinuationToken);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
