using Dfc.ProviderPortal.ChangeFeedListener.Interfaces;
using Dfc.ProviderPortal.ChangeFeedListener.Models;
using Dfc.ProviderPortal.Packages;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class BlobStorageHelper : IBlobStorageHelper
    {
        private readonly IBlobStorageSettings _blobStorageSettings;

        public BlobStorageHelper(IBlobStorageSettings settings)
        {
            Throw.IfNull(settings, nameof(settings));

            _blobStorageSettings = settings;
        }

        public async Task UploadFile(CloudBlobContainer container, string fileName, byte[] data)
        {
            var blob = container.GetBlockBlobReference(fileName);
            using (var stream = new MemoryStream(data))
            {
                await blob.UploadFromStreamAsync(stream);
            }
        }

        public CloudBlobContainer GetBlobContainer(string containerName)
        {
            if (CloudStorageAccount.TryParse(_blobStorageSettings.ConnectionString, out CloudStorageAccount storageAccount))
            {
                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference(containerName);

                return container;
            }
            else
            {
                throw new InvalidOperationException("Unable to access storage account.");
            }
        }

        public async Task<string> ReadFileAsync(CloudBlobContainer container, string fileName)
        {
            var text = string.Empty;
            var blob = container.GetBlockBlobReference(fileName);

            using (var memoryStream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(memoryStream);
                text = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            return text;
        }

        public async Task MoveFile(CloudBlobContainer container, string fileName, string targetPath)
        {
            CloudBlob existBlob = container.GetBlobReference(fileName);
            CloudBlob newBlob = container.GetBlobReference(targetPath);
            await newBlob.StartCopyAsync(existBlob.Uri);
            await existBlob.DeleteAsync();
        }
    }
}