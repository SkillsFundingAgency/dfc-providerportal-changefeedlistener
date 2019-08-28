using Dfc.ProviderPortal.ChangeFeedListener.Models;
using System.Collections.Generic;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IProviderServiceWrapper
    {
        IEnumerable<AzureSearchProviderModel> GetLiveProvidersForAzureSearch();
    }
}
