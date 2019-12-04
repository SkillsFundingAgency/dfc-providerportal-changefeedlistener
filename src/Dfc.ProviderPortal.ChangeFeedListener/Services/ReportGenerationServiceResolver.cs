using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public delegate IReportGenerationService ReportGenerationServiceResolver(ProcessType type);
}
