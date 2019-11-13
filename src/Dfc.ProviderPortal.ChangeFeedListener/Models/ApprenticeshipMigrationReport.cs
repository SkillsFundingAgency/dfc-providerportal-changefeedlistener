using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class ApprenticeshipMigrationReport
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public int ProviderUKPRN { get; set; }
        public DateTime MigrationDate { get; set; }
        public int ApprenticeshipsMigrated { get; set; }
        public int Live { get; set; }
        public int MigrationsPending { get; set; }
        public int NotTransferred { get; set; }
    }
}
