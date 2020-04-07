using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;

namespace Dfc.ProviderPortal.ChangeFeedListener.Settings
{
    public class FaocSearchServiceSettings : IFaocSearchServiceSettings
    {
        public string SearchService { get; set; }
        public string ApiUrl { get; set; }
        public string AdminKey { get; set; }
        public string Index { get; set; }
    }
}
