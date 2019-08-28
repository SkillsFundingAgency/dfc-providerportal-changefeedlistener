using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.Packages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class ProviderServiceWrapper : IProviderServiceWrapper
    {
        private readonly IProviderServiceSettings _settings;
        private readonly HttpClient _client;

        public ProviderServiceWrapper(IProviderServiceSettings settings, HttpClient client)
        {
            Throw.IfNull(settings, nameof(settings));
            _settings = settings;

            Throw.IfNull(client, nameof(client));
            _client = client;
        }

        public dynamic GetByPRN(int PRN)
        {
            // Call service to get data
            //HttpClient client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var criteria = new { PRN };
            StringContent content = new StringContent(JsonConvert.SerializeObject(criteria), Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> taskResponse = _client.PostAsync($"{_settings.ApiUrl}GetProviderByPRN", content);
            taskResponse.Wait();
            Task<string> taskJSON = taskResponse.Result.Content.ReadAsStringAsync();
            taskJSON.Wait();
            string json = taskJSON.Result;

            // Return data as model object
            return JsonConvert.DeserializeObject<dynamic>(json);
        }

        public IEnumerable<AzureSearchProviderModel> GetLiveProvidersForAzureSearch()
        {
            // Call service to get data
            //HttpClient client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
            var criteria = new object();
            StringContent content = new StringContent(JsonConvert.SerializeObject(criteria), Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> taskResponse = _client.PostAsync($"{_settings.ApiUrl}GetLiveProvidersForAzureSearch", content);
            taskResponse.Wait();
            Task<string> taskJSON = taskResponse.Result.Content.ReadAsStringAsync();
            taskJSON.Wait();
            string json = taskJSON.Result;

            // Return data as model objects
            if (!json.StartsWith("["))
                json = "[" + json + "]";
            return JsonConvert.DeserializeObject<IEnumerable<AzureSearchProviderModel>>(json);
        }
    }
}
