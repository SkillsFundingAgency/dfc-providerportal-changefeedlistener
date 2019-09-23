using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Microsoft.Spatial;
using System;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class AzureSearchCourse : IAzureSearchCourse
    {
        public Guid? id { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? CourseRunId { get; set; }
        public string QualificationCourseTitle { get; set; }
        public string LearnAimRef { get; set; }
        public string NotionalNVQLevelv2 { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string VenueName { get; set; }
        public string VenueAddress { get; set; }
        public GeographyPoint VenueLocation { get; set; }
        public string VenueAttendancePattern { get; set; }
        public string VenueAttendancePatternDescription { get; set; }
        public string UKPRN { get; set; }
        public string ProviderName { get; set; }
        public string Region { get; set; }
        //public string Weighting { get; set; }
        public decimal ScoreBoost { get; set; }
        public int? Status { get; set; }
        public string VenueStudyMode { get; set; }
        public string VenueStudyModeDescription { get; set; }
        public string DeliveryMode { get; set; }
        public string DeliveryModeDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public string VenueTown { get; set; }
        public int? Cost { get; set; }
        public string CostDescription { get; set; }
        public string CourseText { get; set; }
    }
}
