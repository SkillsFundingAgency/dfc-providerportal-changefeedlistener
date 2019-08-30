namespace Dfc.ProviderPortal.ChangeFeedListener.Interfaces
{
    public interface ISearchServiceSettings
    {
        string SearchService { get; }
        string ApiUrl { get; }
        string ApiVersion { get; }
        string QueryKey { get; }
        string AdminKey { get; }
        string Index { get; }
        string onspdIndex { get; }
        int DefaultTop { get; }
        decimal? RegionSearchBoost { get; }
        decimal? SubRegionSearchBoost { get; }
        //string RegionBoostScoringProfile { get; }
        int ThresholdVenueCount { get; }
    }
}
