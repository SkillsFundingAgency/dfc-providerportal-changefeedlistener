﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
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
        //private readonly IProviderServiceSettings _providerServiceSettings;
        //private readonly IVenueServiceSettings _venueServiceSettings;
        private static SearchServiceClient _queryService;
        private static SearchServiceClient _adminService;
        private static ISearchIndexClient _queryIndex;
        private static ISearchIndexClient _adminIndex;
        private static ISearchIndexClient _onspdIndex;
        private HttpClient _httpClient;
        private readonly Uri _uri;

        public SearchServiceWrapper(
            ILogger log,
            ISearchServiceSettings settings)
        {
            Throw.IfNull(log, nameof(log));
            Throw.IfNull(settings, nameof(settings));

            _log = log;
            _settings = settings;

            _queryService = new SearchServiceClient(settings.SearchService, new SearchCredentials(settings.QueryKey));
            _adminService = new SearchServiceClient(settings.SearchService, new SearchCredentials(settings.AdminKey));
            _queryIndex = _queryService?.Indexes?.GetClient(settings.Index);
            _adminIndex = _adminService?.Indexes?.GetClient(settings.Index);
            _onspdIndex = _queryService?.Indexes?.GetClient(settings.onspdIndex);

            _httpClient = new HttpClient(); 
            _httpClient.DefaultRequestHeaders.Add("api-key", settings.QueryKey);
            _httpClient.DefaultRequestHeaders.Add("api-version", settings.ApiVersion);
            _httpClient.DefaultRequestHeaders.Add("indexes", settings.Index);
            _uri = new Uri($"{settings.ApiUrl}?api-version={settings.ApiVersion}");
        }

        public IEnumerable<IndexingResult> UploadBatch(
            IEnumerable<AzureSearchProviderModel> providers,
            IEnumerable<AzureSearchVenueModel> venues,
            IReadOnlyList<Document> documents,
            out int succeeded)
        {
            try
            {
                succeeded = 0;
                if (documents.Any())
                {

                    IEnumerable<Course> courses = documents.Select(d => new Course()
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
                                                            from CourseRun r in c?.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.ClassroomBased) ?? new List<CourseRun>()
                                                            from AzureSearchVenueModel v in venues.Where(x => r.VenueId == x.id)
                                                                                                  .DefaultIfEmpty()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = (SubRegionItemModel)null, Venue = v };
                    _log.LogInformation($"{classroom.Count()} classroom courses (with VenueId and no regions)");


                    // Work-based courses have regions instead
                    IEnumerable<LINQComboClass> workbased = from Course c in courses
                                                            from CourseRun r in c?.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.WorkBased && !x.National.GetValueOrDefault(false)) ?? new List<CourseRun>()
                                                            from SubRegionItemModel subregion in r.SubRegions ?? new List<SubRegionItemModel>()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = subregion, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{workbased.Count()} regional work-based courses (with regions but no VenueId)");

                    // Except for ones with National flag set, where Subregions is null
                    IEnumerable<LINQComboClass> national =  from Course c in courses
                                                            from CourseRun r in c?.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.WorkBased && x.National.GetValueOrDefault(false)) ?? new List<CourseRun>()
                                                            //from SubRegionItemModel subregion in ???
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = new SubRegionItemModel() { SubRegionName = "National" }, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{national.Count()} national work-based courses (with no regions or VenueId)");


                    // Online courses have neither
                    IEnumerable<LINQComboClass> online =    from Course c in courses
                                                            from CourseRun r in c?.CourseRuns?.Where(x => x.DeliveryMode == DeliveryMode.Online) ?? new List<CourseRun>()
                                                            select new LINQComboClass() { Course = c, Run = r, SubRegion = (SubRegionItemModel)null, Venue = (AzureSearchVenueModel)null };
                    _log.LogInformation($"{online.Count()} online courses (with no region or VenueId)");


                    decimal regionBoost = _settings.RegionSearchBoost ?? 2.3M;
                    decimal subregionBoost = _settings.SubRegionSearchBoost ?? 4.5M;

                    IEnumerable<AzureSearchCourse> batchdata = from LINQComboClass x in classroom.Union(workbased)
                                                                                                 .Union(national)
                                                                                                 .Union(online)
                                                               join AzureSearchProviderModel p in providers
                                                               on x.Course?.ProviderUKPRN equals p.UnitedKingdomProviderReferenceNumber
                                                               where (x.Run?.RecordStatus != RecordStatus.Pending && (x.Run?.DeliveryMode == DeliveryMode.Online || x.Venue != null || x.SubRegion != null))
                                                               select new AzureSearchCourse()
                                                               {
                                                                   id = ((x.Run?.DeliveryMode == DeliveryMode.WorkBased && x.Run?.SubRegions?.Count() > 1)
                                                                                ? Guid.NewGuid()
                                                                                : (x.Run?.id ?? Guid.NewGuid())
                                                                        ),
                                                                   CourseId = x.Course?.id,
                                                                   CourseRunId = x.Run?.id,
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
                                                                   VenueAttendancePattern = ((int)x.Run?.AttendancePattern).ToString(),
                                                                   VenueAttendancePatternDescription = x.Run?.AttendancePattern.Description(),
                                                                   VenueLocation = GeographyPoint.Create(x.Venue?.Latitude ?? 0, x.Venue?.Longitude ?? 0),
                                                                   UKPRN = x.Course?.ProviderUKPRN.ToString(),
                                                                   ProviderName = p?.ProviderName,
                                                                   Region = x.SubRegion?.SubRegionName ?? "",
                                                                   Status = (int?)x.Run?.RecordStatus,
                                                                   //Weighting = "",
                                                                   ScoreBoost = (x.SubRegion == null || x.Run.National.GetValueOrDefault(false) || x.SubRegion?.Weighting == SearchResultWeightings.Low ? 1
                                                                                   : (x.SubRegion?.Weighting == SearchResultWeightings.High ? subregionBoost : regionBoost)
                                                                                ),
                                                                   //VenueEmail = ???,
                                                                   //VenuePhoneNo = ???,
                                                                   //SupportFacilities = ???,
                                                                   //VenueLat = ???,
                                                                   //VenueLong = ???,
                                                                   //VenueFax = ???,
                                                                   VenueTown = x.Venue?.TOWN,
                                                                   VenueStudyMode = ((int)x.Run?.StudyMode).ToString(),
                                                                   VenueStudyModeDescription = x.Run?.StudyMode.Description(),
                                                                   DeliveryMode = ((int)x.Run?.DeliveryMode).ToString(),
                                                                   DeliveryModeDescription = x.Run?.DeliveryMode.Description(),
                                                                   //OpportunityId = ???,
                                                                   //SubjectCategory = ???,
                                                                   //EquipmentRequired = ???,
                                                                   //AssessmentMethod = ???,
                                                                   Cost = (x.Run?.Cost == null ? (int?)null : Convert.ToInt32(x.Run.Cost)),
                                                                   CostDescription = x.Run?.CostDescription,
                                                                   StartDate = x.Run?.StartDate,
                                                                   CourseText = x.Course?.CourseDescription,
                                                                   UpdatedOn = x.Run?.UpdatedDate ?? x.Run?.CreatedDate,
                                                                   CourseDescription = x.Course?.CourseDescription,
                                                                   CourseName = x.Run?.CourseName,
                                                                   FlexibleStartDate = x.Run.FlexibleStartDate,
                                                                   DurationUnit = x.Run.DurationUnit,
                                                                   DurationValue = x.Run.DurationValue
                                                               };

                    if (batchdata.Any())
                    {
                        IndexBatch<AzureSearchCourse> batch = IndexBatch.MergeOrUpload(batchdata);

                        _log.LogInformation("Merging docs to azure search index: course");
                        Task<DocumentIndexResult> task = _adminIndex.Documents.IndexAsync(batch);
                        task.Wait();
                        succeeded = batchdata.Count();
                    }
                    _log.LogInformation($"*** Successfully merged {succeeded} docs into Azure search index: course");
                }

            } catch (IndexBatchException ex) {
                IEnumerable<IndexingResult> failed = ex.IndexingResults.Where(r => !r.Succeeded);
                _log.LogError(ex, string.Format("Failed to index some of the documents: {0}",
                                                string.Join(", ", failed)));
                //_log.LogError(ex.ToString());
                succeeded = ex.IndexingResults.Count(x => x.Succeeded);
                return failed;

            } catch (Exception e) {
                throw e;
            }

            // Return empty list of failed IndexingResults
            return new List<IndexingResult>();
        }

    }
}
