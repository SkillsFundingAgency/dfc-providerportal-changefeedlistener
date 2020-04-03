using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public static class UpdateApprenticeshipMigrationReports
    {
        [Disable]
        [FunctionName("UpdateApprenticeshipMigrationReports")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            [Inject] ReportGenerationServiceResolver reportGenerationServiceResolver)
        {
            var reportGenerationService = reportGenerationServiceResolver(ProcessType.Apprenticeship);

            try
            {
                await reportGenerationService.GenerateAllReports(log);

                return new OkResult();

            }
            catch (Exception e)
            {
                return new InternalServerErrorObjectResult(new Exception($"Failed to update all apprenticeship reports", e));
            }
        }
    }
}
