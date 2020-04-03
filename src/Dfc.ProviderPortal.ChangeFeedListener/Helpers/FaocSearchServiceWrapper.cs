using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models.Foac;
using Dfc.ProviderPortal.Packages;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
            _adminService = new SearchServiceClient(_settings.SearchService, new SearchCredentials(_settings.AdminKey));
            _adminIndex = _adminService?.Indexes?.GetClient(_settings.Index);

            var batch = IndexBatch.MergeOrUpload(documents);
            var indexResult = await _adminIndex.Documents.IndexAsync(batch);
            return indexResult.Results;
        }
    }
}
