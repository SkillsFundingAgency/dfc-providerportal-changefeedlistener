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

        public T GetById<T>(Guid id)
        {
            // Call service to get data
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var criteria = new { id };
            StringContent content = new StringContent(JsonConvert.SerializeObject(criteria), Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> taskResponse = client.PostAsync($"{_settings.ApiUrl}GetVenueById?code={_settings.ApiKey}", content);
            taskResponse.Wait();
            Task<string> taskJSON = taskResponse.Result.Content.ReadAsStringAsync();
            taskJSON.Wait();
            string json = taskJSON.Result;

            // Return data as model object
            return JsonConvert.DeserializeObject<T>(json);
        }

        public IEnumerable<AzureSearchVenueModel> GetVenues()
        {
            // Call service to get data
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var criteria = new object();
            StringContent content = new StringContent(JsonConvert.SerializeObject(criteria), Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> taskResponse = client.PostAsync($"{_settings.ApiUrl}GetAllVenues", content);
            taskResponse.Wait();
            Task<string> taskJSON = taskResponse.Result.Content.ReadAsStringAsync();
            taskJSON.Wait();
            string json = taskJSON.Result;

            // Return data as model objects
            if (!json.StartsWith("["))
                json = "[" + json + "]";
            return JsonConvert.DeserializeObject<IEnumerable<AzureSearchVenueModel>>(json);
        }
    }
}
