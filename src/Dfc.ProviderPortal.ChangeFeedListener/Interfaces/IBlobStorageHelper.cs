using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IBlobStorageHelper
    {
        CloudBlobContainer GetBlobContainer(string containerName);
        Task<string> ReadFileAsync(CloudBlobContainer container, string fileName);
        Task UploadFile(CloudBlobContainer container, string fileName, byte[] data);
        Task MoveFile(CloudBlobContainer container, string fileName, string targetPath);
    }
}
