
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dfc.ProviderPortal.ChangeFeedListener;
using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Services;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Dfc.ProviderPortal.Packages.AzureFunctions.DependencyInjection;
using Microsoft.Extensions.Logging;


[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "Web Jobs Extension Startup")]
namespace Dfc.ProviderPortal.ChangeFeedListener
{
    public class WebJobsExtensionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddDependencyInjection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            BuildCosmosDbSettings(builder.Services, configuration);

            var blobStorageSettings = new BlobStorageSettings();
            configuration.Bind("BlobStorageSettings", blobStorageSettings);
            builder.Services.AddSingleton<IBlobStorageSettings>(blobStorageSettings);
            var ifaocSearchSettings = new FaocSearchServiceSettings();
            configuration.Bind("FaocSearchServiceSettings", ifaocSearchSettings);
            builder.Services.AddSingleton<IFaocSearchServiceSettings>(ifaocSearchSettings);

            builder.Services.AddSingleton<IConfiguration>(configuration);          
            //builder.Services.Configure<CosmosDbCollectionSettings>(configuration.GetSection(nameof(CosmosDbCollectionSettings)));
            builder.Services.Configure<SearchServiceSettings>(configuration.GetSection(nameof(SearchServiceSettings)));
            builder.Services.Configure<VenueServiceSettings>(configuration.GetSection(nameof(VenueServiceSettings)));
            builder.Services.Configure<ProviderServiceSettings>(configuration.GetSection(nameof(ProviderServiceSettings)));
            builder.Services.AddScoped<ICosmosDbHelper, CosmosDbHelper>();
            builder.Services.AddTransient<ICourseAuditService, CourseAuditService>();
            builder.Services.AddTransient<ICourseArchiveService, CourseArchiveService>();
            builder.Services.AddTransient<ISearchServiceWrapper, SearchServiceWrapper>();
            builder.Services.AddTransient<IFaocSearchServiceWrapper, FaocSearchServiceWrapper>();
            builder.Services.AddTransient<IVenueServiceWrapper, VenueServiceWrapper>();
            builder.Services.AddTransient<IProviderServiceWrapper, ProviderServiceWrapper>();
            builder.Services.AddTransient<IBlobStorageHelper, BlobStorageHelper>();
            builder.Services.AddTransient((provider) => new HttpClient());
            ReportGenerationResolver(builder.Services);

            var serviceProvider = builder.Services.BuildServiceProvider();
        }

        private void BuildCosmosDbSettings(IServiceCollection services, IConfigurationRoot configuration)
        {
            var cosmosSettings = new CosmosDbSettings
            {
                DatabaseId = configuration["CosmosDatabaseId"],
                EndpointUri = configuration["EndpointUri"],
                PrimaryKey = configuration["PrimaryKey"]
            };

            var cosmosCollectionSettings = new CosmosDbCollectionSettings
            {
                CoursesCollectionId = configuration["CoursesCollectionId"],
                CoursesMigrationReportCollectionId = configuration["CoursesMigrationReportCollectionId"],
                ProviderCollectionId = configuration["ProviderCollectionId"],
                DfcReportCollectionId = configuration["DfcReportCollectionId"],
                AuditCollectionId = configuration["AuditCollectionId"],
                ApprenticeshipMigrationReportCollectionId = configuration["ApprenticeshipMigrationReportCollectionId"],
                ApprenticeshipCollectionId = configuration["ApprenticeshipCollectionId"],
                ApprenticeshipDfcReportCollection = configuration["ApprenticeshipDfcReportCollectionId"]
            };

            services.AddSingleton<CosmosDbSettings>(cosmosSettings);
            services.AddSingleton<CosmosDbCollectionSettings>(cosmosCollectionSettings);

        }

        private void ReportGenerationResolver(IServiceCollection services)
        {
            services.AddTransient<ReportGenerationServiceResolver>(serviceProvider => type =>
            {
                switch (type)
                {
                    case ProcessType.Apprenticeship:

                        return new ApprenticeshipReportGenerationService(
                            serviceProvider.GetService<ICosmosDbHelper>(),
                            serviceProvider.GetService<CosmosDbCollectionSettings>());
                    case ProcessType.Course:
                        return new CourseReportGenerationService(
                            serviceProvider.GetService<ICosmosDbHelper>(),
                            serviceProvider.GetService<CosmosDbCollectionSettings>());
                    default:
                        throw new KeyNotFoundException();
                }
            });
        }
    }
}
