﻿using Microsoft.Spatial;
using System;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IAzureSearchCourse
    {
        Guid? id { get; set; }
        Guid? CourseId { get; set; }
        string QualificationCourseTitle { get; set; }
        string LearnAimRef { get; set; }
        string NotionalNVQLevelv2 { get; set; }
        DateTime? UpdatedOn { get; set; }
        string VenueName { get; set; }
        string VenueAddress { get; set; }
        GeographyPoint VenueLocation { get; set; }
        string VenueAttendancePattern { get; set; }
        //string VenueAttendancePatternDescription { get; set; }
        string ProviderName { get; set; }
        string Region { get; set; }
        int? Status { get; set; }
        string VenueStudyMode { get; set; }
        //string VenueStudyModeDescription { get; set; }
        string DeliveryMode { get; set; }
        //string DeliveryModeDescription { get; set; }
        DateTime? StartDate { get; set; }
        string VenueTown { get; set; }
        int? Cost { get; set; }
        string CostDescription { get; set; }
        string CourseText { get; set; }
    }
}
