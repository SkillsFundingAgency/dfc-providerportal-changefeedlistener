using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IVenueServiceWrapper
    {
        Task<IEnumerable<AzureSearchVenueModel>> GetVenues();
        Task<T> GetById<T>(Guid id);
    }
}
