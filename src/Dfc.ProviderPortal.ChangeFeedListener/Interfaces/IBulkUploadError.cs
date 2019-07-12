namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IBulkUploadError
    {
        int LineNumber { get; }
        string Header { get; }
        string Error { get; }
    }
}
