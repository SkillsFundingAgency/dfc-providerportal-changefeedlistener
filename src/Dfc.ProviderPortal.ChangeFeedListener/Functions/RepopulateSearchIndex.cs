using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public static class RepopulateSearchIndex
    {
        [Disable]
        [FunctionName("RepopulateSearchIndex")]
        public static Task Run(
            [TimerTrigger("0 0 0 */1 * *")]TimerInfo timer,
            ILogger log,
            [Inject] ICourseAuditService courseAuditService)
        {
            return courseAuditService.RepopulateSearchIndex(log);
        }
    }
}
