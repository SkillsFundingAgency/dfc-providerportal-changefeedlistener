using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public class RepopulateSearchIndex
    {
        private readonly ICourseAuditService _courseAuditService;
        private readonly ILogger<RepopulateSearchIndex> _logger;

        public RepopulateSearchIndex(
            ICourseAuditService courseAuditService,
            ILogger<RepopulateSearchIndex> logger)
        {
            _courseAuditService = courseAuditService;
            _logger = logger;
        }

        [FunctionName("RepopulateSearchIndex")]
        public Task Run([TimerTrigger("0 0 0 */1 * *")] TimerInfo timer)
        {
            return _courseAuditService.RepopulateSearchIndex(_logger, deleteZombies: false);
        }

        [FunctionName("RepopulateSearchIndexAndDeleteZombies")]
        [NoAutomaticTrigger]
        public Task RepopulateSearchIndexAndDeleteZombies(string input)
        {
            return _courseAuditService.RepopulateSearchIndex(_logger, deleteZombies: true);
        }
    }
}
