using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public static class UpdateCourseMigrationReports
    {
        [Disable]
        [FunctionName("UpdateCourseMigrationReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            [Inject] ReportGenerationServiceResolver reportGenerationServiceResolver)
        {
            var reportGenerationService = reportGenerationServiceResolver(ProcessType.Course);

            try
            {
                await reportGenerationService.GenerateAllReports(log);

                return new OkResult();

            }
            catch (Exception e)
            {
                return new InternalServerErrorObjectResult(new Exception($"Failed to update all reports", e));
            }
        }
    }
}
