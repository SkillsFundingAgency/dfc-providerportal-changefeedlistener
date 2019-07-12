using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class CourseMigrationReport: Document
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public IList<Course> LarslessCourses { get; set; }
        public int? PreviousLiveCourseCount { get; set; }
        public int ProviderUKPRN { get; set; }
    }
}
