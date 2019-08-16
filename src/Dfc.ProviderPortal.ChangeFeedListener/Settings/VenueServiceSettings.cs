using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;

namespace Dfc.ProviderPortal.ChangeFeedListener.Settings
{
    public class VenueServiceSettings : IVenueServiceSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
