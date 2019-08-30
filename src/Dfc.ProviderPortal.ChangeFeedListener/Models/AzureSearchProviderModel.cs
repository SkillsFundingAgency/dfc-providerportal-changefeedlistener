using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Newtonsoft.Json;
using System;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{

    [JsonConverter(typeof(AzureSearchProviderModelJsonConverter))]
    public class AzureSearchProviderModel
    {
        public Guid? id { get; set; }
        public int UnitedKingdomProviderReferenceNumber { get; set; }
        public string ProviderName { get; set; }
    }
}
