namespace Dfc.ProviderPortal.ChangeFeedListener.Settings
{
    public class CosmosDbCollectionSettings
    {
        public string ProviderCollectionId { get; set; }
        public string CoursesCollectionId { get; set; }
        public string ApprenticeshipCollectionId { get; set; }
        public string ApprenticeshipMigrationReportCollectionId { get; set; }
        public string CoursesMigrationReportCollectionId { get; set; }
        public string DfcReportCollectionId { get; set; }
        public string ApprenticeshipDfcReportCollection { get; set; }
        public string AuditCollectionId { get; set; }
    }
}
