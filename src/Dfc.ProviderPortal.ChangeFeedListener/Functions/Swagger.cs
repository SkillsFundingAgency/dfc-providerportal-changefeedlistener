using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Swagger;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{
    public static class Swagger
    {
        [FunctionName("Swagger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger.json")] HttpRequest req,
            ILogger log)
        {
            var url = req.GetDisplayUrl();
            if (req.Path.HasValue && !string.IsNullOrWhiteSpace(req.Path.Value)) url = url.Replace(req.Path, string.Empty);
            if (req.QueryString.HasValue && !string.IsNullOrWhiteSpace(req.QueryString.Value)) url = url.Replace(req.QueryString.Value, string.Empty);
            var host = new WebHostBuilder().UseStartup<Startup>().Build();
            var swaggerProvider = host.Services.GetRequiredService<ISwaggerProvider>();
            var swagger = swaggerProvider.GetSwagger("v1");
            swagger.Servers.Add(new OpenApiServer { Url = url });

            using (var writer = new StringWriter(new StringBuilder()))
            {
                var jsonWriter = new OpenApiJsonWriter(writer);
                //swagger.SerializeAsV3(jsonWriter);
                swagger.SerializeAsV2(jsonWriter);
                var obj = JObject.Parse(writer.ToString());
                return new JsonResult(obj);
            }
        }
    }
}
