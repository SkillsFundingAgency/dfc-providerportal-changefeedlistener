using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class BulkUploadError : IBulkUploadError
    {
        public int LineNumber { get; set; }
        public string Header { get; set; }
        public string Error { get; set; }
    }
}
