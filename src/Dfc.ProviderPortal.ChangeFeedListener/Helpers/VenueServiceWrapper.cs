using Dfc.ProviderPortal.Packages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class VenueServiceWrapper : IVenueServiceWrapper
    {
        private readonly IVenueServiceSettings _settings;

        public VenueServiceWrapper(IVenueServiceSettings settings)
        {
            Throw.IfNull(settings, nameof(settings));
            _settings = settings;
        }

        public async Task<T> GetById<T>(Guid id)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);

                var response = await client.GetAsync($"{_settings.ApiUrl}GetVenueById?id={id}");

                if ((int)response.StatusCode == 404)
                {
                    return default(T);
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public async Task<IEnumerable<AzureSearchVenueModel>> GetVenues()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);

                var response = await client.GetAsync($"{_settings.ApiUrl}GetAllVenues");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<IEnumerable<AzureSearchVenueModel>>(json);
            }
        }
    }
}
