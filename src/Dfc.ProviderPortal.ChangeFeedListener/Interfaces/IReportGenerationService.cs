using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IReportGenerationService
    {
        Task UpdateReport(int ukprn);
        Task GenerateAllReports(ILogger log);
        Task GenerateReportForProvider(int ukprn);
    }
}
