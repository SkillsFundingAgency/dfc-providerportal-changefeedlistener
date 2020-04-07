using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class BlobStorageSettings : IBlobStorageSettings
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string Container { get; set; }
        public string ConnectionString { get; set; }
    }
}
