namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface IFaocSearchServiceSettings
    {
        string SearchService { get; }
        string ApiUrl { get; }
        string AdminKey { get; }
        string Index { get; }
    }
}
