using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class ApprenticeshipQaCompliance
    {
        public int ApprenticeshipQaComplianceId { get; set; }

        public int ApprenticeshipId { get; set; }

        public string CreatedByUserEmail { get; set; }

        public string CreatedDateTimeUtc { get; set; }

        public string TextQAd { get; set; }
        public string DetailsOfUnverifiableClaim { get; set; }
        public string DetailsOfComplianceFailure { get; set; }
        public bool Passed { get; set; }
        public List<string> FailureReasons { get; set; }
    }
}
