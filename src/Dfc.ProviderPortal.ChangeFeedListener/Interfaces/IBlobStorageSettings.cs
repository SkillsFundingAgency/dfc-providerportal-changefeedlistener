namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IBlobStorageSettings
    {
        string AccountName { get; }
        string AccountKey { get; }
        string Container { get; }
        string ConnectionString { get; }
    }
}
