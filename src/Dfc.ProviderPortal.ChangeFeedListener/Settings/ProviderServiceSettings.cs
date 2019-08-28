using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;

namespace Dfc.ProviderPortal.ChangeFeedListener.Settings
{
    public class ProviderServiceSettings : IProviderServiceSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
