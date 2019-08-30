using System;
using System.Collections.Generic;
using System.Text;
using Dfc.ProviderPortal.ChangeFeedListener.Models;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IVenueServiceWrapper
    {
        IEnumerable<AzureSearchVenueModel> GetVenues();
        T GetById<T>(Guid id);
    }
}
