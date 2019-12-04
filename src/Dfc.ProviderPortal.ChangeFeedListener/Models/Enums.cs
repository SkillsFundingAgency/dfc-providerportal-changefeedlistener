using System.ComponentModel;


namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public enum RecordStatus
    {
        [Description("Undefined")]
        Undefined = 0,
        [Description("Live")]
        Live = 1,
        [Description("Pending")]
        Pending = 2,
        [Description("Archived")]
        Archived = 4,
        [Description("Deleted")]
        Deleted = 8,
        [Description("BulkUload Pending")]
        BulkUloadPending = 16,
        [Description("BulkUpload Ready To Go Live")]
        BulkUploadReadyToGoLive = 32,
        [Description("API Pending")]
        APIPending = 64,
        [Description("API Ready To Go Live")]
        APIReadyToGoLive = 128,
        [Description("Migration Pending")]
        MigrationPending = 256,
        [Description("Migration Ready To Go Live")]
        MigrationReadyToGoLive = 512,
        [Description("Migration Deleted")]
        MigrationDeleted = 1024,
    }

    public enum ApprenticeshipType
    {
        [Description("Undefined")]
        Undefined = 0,
        [Description("Standard Code")]
        StandardCode = 1,
        [Description("Framework Code")]
        FrameworkCode = 2
    }

    public enum ApprenticeshipLocationType
    {
        [Description("Undefined")]
        Undefined = 0,
        [Description("Classroom based")] // Venue
        ClassroomBased = 1,
        [Description("Employer based")] // Region
        EmployerBased = 2,
        [Description("Classroom based and employer based")] // Venue with added 
        ClassroomBasedAndEmployerBased = 3
    }

    public enum LocationType
    {
        [Description("Undefined")]
        Undefined = 0,
        [Description("Venue")]
        Venue = 1,
        [Description("Region")]
        Region = 2,
        [Description("SubRegion")]
        SubRegion = 3
    }

    public enum ProcessType
    {
        Course,
        Apprenticeship
    }
}
