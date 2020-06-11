using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models.Foac;
using Dfc.ProviderPortal.Packages;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class FaocSearchServiceWrapper : IFaocSearchServiceWrapper
    {
        private readonly IFaocSearchServiceSettings _settings;
        private static SearchServiceClient _adminService;
        private static ISearchIndexClient _adminIndex;

        public FaocSearchServiceWrapper(
            [Inject] IFaocSearchServiceSettings settings)
        {
            Throw.IfNull(settings, nameof(settings));

            _settings = settings;

            _adminService = new SearchServiceClient(settings.SearchService, new SearchCredentials(settings.AdminKey));
            _adminIndex = _adminService?.Indexes?.GetClient(settings.Index);
        }

        public async Task<IEnumerable<IndexingResult>> UploadFaocBatch(IEnumerable<FaocEntry> documents)
        {
            var batch = IndexBatch.MergeOrUpload(documents);
            var indexResult = await _adminIndex.Documents.IndexAsync(batch);

            return indexResult.Results;
        }

        public async Task DeleteStaleDocuments(IEnumerable<FaocEntry> documents)
        {
            var queryParams = new SearchParameters()
            {
                SearchMode = SearchMode.All,
                QueryType = QueryType.Full,
                Select = new List<string>() { "id" }
            };

            var docs = await _adminIndex.Documents.SearchAsync<dynamic>(null, queryParams);
            var toBeDeleted = new List<string>();

            while (docs.Results.Count > 0)
            {
                var results = docs.Results.Select(d => (string)d.Document.id);
                toBeDeleted.AddRange(results.Where(p => !documents.ToList().Any(p2 => p2.id == p)));
                if (docs.ContinuationToken != null)
                {
                    docs = await _adminIndex.Documents.ContinueSearchAsync<dynamic>(docs.ContinuationToken);
                }
                else
                {
                    break;
                }
            }

            // delete all other online courses that are not in documents
            if (toBeDeleted.Count > 0)
            {
                var deleteBatch = IndexBatch.Delete("id", toBeDeleted);
                var deleteResult = await _adminIndex.Documents.IndexAsync(deleteBatch);
            }
        }
    }
}

