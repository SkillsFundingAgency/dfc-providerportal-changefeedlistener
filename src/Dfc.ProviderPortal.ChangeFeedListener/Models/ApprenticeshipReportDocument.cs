﻿using System;
using Newtonsoft.Json;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class ApprenticeshipDfcReportDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string ProviderUKPRN { get; set; }
        public string ProviderName { get; set; }
        public ProviderType ProviderType { get; set; }
        public DateTime? MigrationDate { get; set; }
        public int? MigratedCount { get; set; }
        public int? FailedMigrationCount { get; set; }
        public int LiveCount { get; set; }
        public int PendingCount { get; set; }
        public int BulkUploadPendingcount { get; set; }
        public int BulkUploadReadyToGoLiveCount { get; set; }
        public int MigrationPendingCount { get; set; }
        public int MigrationReadyToGoLive { get; set; }
        public decimal MigrationRate { get; set; }
    }
}
