
using System.Collections.Generic;


namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public class RegionItemModel
    {
        public string Id { get; set; }
        public string RegionName { get; set; }
        public bool? Checked { get; set; }
        public List<SubRegionItemModel> SubRegion { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
