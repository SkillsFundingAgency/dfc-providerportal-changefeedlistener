
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dfc.ProviderPortal.ChangeFeedListener.Helpers;
using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.ChangeFeedListener.Settings;
using Dfc.ProviderPortal.Packages;
using Document = Microsoft.Azure.Documents.Document;
using Microsoft.Azure.Documents;
using System.Net;

namespace Dfc.ProviderPortal.ChangeFeedListener.Services
{
    public class CourseArchiveService : ICourseArchiveService
    {
       private readonly ICosmosDbHelper _cosmosDbHelper;
       private readonly CosmosDbCollectionSettings _settings;
       private readonly CosmosDbSettings _dbSettings;

       public CourseArchiveService(ICosmosDbHelper cosmosDbHelper, CosmosDbSettings dbSettings, CosmosDbCollectionSettings settings)
       {
           _cosmosDbHelper = cosmosDbHelper;
            _dbSettings = dbSettings;
            _settings = settings;
       }

       public async Task<IEnumerable<Document>> ArchiveAllCourses(ILogger log, IEnumerable<Document> documents)
        {
            Throw.IfNull(log, nameof(log));
            Throw.IfNullOrEmpty(documents, nameof(documents));

            List<Document> responseList = new List<Document>();

            if (documents.Any())
            {
                using (var client = _cosmosDbHelper.GetClient())
                {
                    Uri uri = UriFactory.CreateDocumentCollectionUri(_dbSettings.DatabaseId, _settings.CoursesCollectionId);
                    FeedOptions options = new FeedOptions { EnableCrossPartitionQuery = true, MaxItemCount = -1 };

                    foreach (var doc in documents)
                    {
                        int UKPRN = doc.GetPropertyValue<int?>("UnitedKingdomProviderReferenceNumber") ?? 0;
                        if (UKPRN > 10000000)
                        {
                            log.LogInformation($"Processing document with UKPRN {UKPRN}");
                            IEnumerable<Course> courseDocs = client.CreateDocumentQuery<Course>(uri, options)
                                                                   .Where(x => x.ProviderUKPRN == UKPRN);
                            foreach (Course courseDoc in courseDocs)
                            {
                                log.LogInformation($"Archiving course with id {courseDoc.id}");
                                Uri docUri = UriFactory.CreateDocumentUri(_dbSettings.DatabaseId, _settings.CoursesCollectionId, courseDoc.id.ToString());

                                //var result = await client.DeleteDocumentAsync(docUri, new RequestOptions() { PartitionKey = new PartitionKey(doc.ProviderUKPRN) });
                                Document d = client.ReadDocumentAsync(docUri, new RequestOptions() { PartitionKey = new PartitionKey(UKPRN) })
                                                  ?.Result
                                                  ?.Resource;
                                if (d == null)
                                    log.LogInformation($"** Course with id {courseDoc.id} and Title {courseDoc.QualificationCourseTitle} wasn't archived");
                                else {
                                    d.SetPropertyValue("CourseStatus", (int)RecordStatus.Archived);
                                    //result = await client.UpsertDocumentAsync(docUri, result.Resource);
                                    d = await _cosmosDbHelper.UpdateDocumentAsync(client, _settings.CoursesCollectionId, d);
                                    responseList.Add(d);
                                    log.LogInformation($"  archived successfully");
                                }
                            }
                        }
                    }
                }
            }
            return responseList;
        }
    }
}
