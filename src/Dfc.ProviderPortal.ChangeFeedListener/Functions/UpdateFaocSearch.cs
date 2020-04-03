using CsvHelper;
using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models.Foac;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Functions
{

    public static class UpdateFaocSearch
    {
        [FunctionName(nameof(UpdateFaocSearch))]
        [NoAutomaticTrigger]
        public static async Task Run(
            string input,  // Work around https://github.com/Azure/azure-functions-vs-build-sdk/issues/168
            [Inject] IBlobStorageHelper blobhelper,
            [Inject] ILoggerFactory loggerFactory,
            [Inject] IConfiguration configuration,
            [Inject] IFaocSearchServiceWrapper searchService
            )
        {
            var logger = loggerFactory.CreateLogger(typeof(UpdateFaocSearch));
            var blobContainername = configuration["BlobStorageSettings:Container"];
            var blobContainer = blobhelper.GetBlobContainer(blobContainername);
            var faocEntries = await GetFaocList();
            var validator = new FaocValidator();
            var validationErrors = new List<FaocValidationError>();
            var validFaocEntries = new List<FaocEntry>();

            foreach (var onlineCourse in faocEntries)
            {
                var isValid = validator.Validate(onlineCourse);
                if (!isValid.IsValid)
                {
                    validationErrors.Add(new FaocValidationError { ID = onlineCourse.id, Error = isValid.Errors.Select(x => x.ErrorMessage).ToList() });
                    continue;
                }
                else
                    validFaocEntries.Add(onlineCourse);
            }

            if (validFaocEntries.Count() > 0)
            {
                //upload coureses
                var results = searchService.UploadFaocBatch(validFaocEntries);

                logger.LogWarning($"{ validationErrors.Count } Courses Failed To Import");

                if (validationErrors.Count() == 0)
                    await blobhelper.MoveFile(blobContainer, "Courses.csv", $"ProcessedCourses/courses-{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}.csv");
                else
                    await blobhelper.MoveFile(blobContainer, "Courses.csv", $"Errors/courses-{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}.csv");
            }

            var totalImported = faocEntries.Count() - validationErrors.Count();
            logger.LogWarning($"Finished Importing {totalImported} of {faocEntries.Count()} online courses.");
            
            async Task<IList<FaocEntry>> GetFaocList()
            {
                var blob = blobhelper.GetBlobContainer(blobContainername).GetBlockBlobReference("Courses.csv");

                var ms = new MemoryStream();
                await blob.DownloadToStreamAsync(ms);
                ms.Seek(0L, SeekOrigin.Begin);

                var results = new HashSet<int>();
                using (var reader = new StreamReader(ms))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        //incase header is lower case
                        csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower();
                        var records = csv.GetRecords<FaocEntry>();
                        return records.ToList();
                    }
                }
            }
        }
    }
}
