using System;
using System.Collections.Generic;
using Dfc.ProviderPortal.ChangeFeedListener.Models;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ICourse
    {
        Guid id { get; }
        int? CourseId { get; }
        string QualificationCourseTitle { get; }
        string LearnAimRef { get; }
        string NotionalNVQLevelv2 { get; }
        string AwardOrgCode { get; }
        string QualificationType { get; }
        int ProviderUKPRN { get; }
        string CourseDescription { get; }
        string EntryRequirements { get; }
        string WhatYoullLearn { get; }
        string HowYoullLearn { get; }
        string WhatYoullNeed { get; }
        string HowYoullBeAssessed { get; }
        string WhereNext { get; }
        bool AdultEducationBudget { get; }
        bool AdvancedLearnerLoan { get; }
       
        RecordStatus CourseStatus { get; }
        DateTime CreatedDate { get; }
        string CreatedBy { get; }
        DateTime? UpdatedDate { get; }
        string UpdatedBy { get; }

        bool IsValid { get; }
        IEnumerable<CourseRun> CourseRuns { get; }
        IEnumerable<BulkUploadError> BulkUploadErrors { get; set; }
    }
}