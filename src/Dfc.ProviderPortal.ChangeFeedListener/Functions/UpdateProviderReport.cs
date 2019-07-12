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

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public static class UpdateProviderReport
    {
        [FunctionName("UpdateProviderReport")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            [Inject] IReportGenerationService reportGenerationService)
        {
            string fromQuery = req.Query["UKPRN"];

            if (!int.TryParse(fromQuery, out int UKPRN))
                return new BadRequestObjectResult($"Invalid UKPRN value, expected a valid integer");

            try
            {
                await reportGenerationService.GenerateReportForProvider(UKPRN);

                return new OkResult();

            }
            catch (Exception e)
            {
                return new InternalServerErrorObjectResult(new Exception($"Unable to update provider report for UKPRN : {UKPRN}", e));
            }
        }
    }
}
