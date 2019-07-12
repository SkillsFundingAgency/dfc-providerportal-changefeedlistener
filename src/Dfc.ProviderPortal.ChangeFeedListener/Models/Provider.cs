using System;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models
{
    public enum Status
    {
        Registered = 0,
        Onboarded = 1,
        Unregistered = 2
    }

    public class Provider 
    {
        public Guid id { get; set; }
        public string UnitedKingdomProviderReferenceNumber { get; set; }
        public string ProviderName { get; set; }
        public string ProviderStatus { get; set; }
        public Providercontact[] ProviderContact { get; set; }
        public DateTime ProviderVerificationDate { get; set; }
        public bool ProviderVerificationDateSpecified { get; set; }
        public bool ExpiryDateSpecified { get; set; }
        public object ProviderAssociations { get; set; }
        public Provideralias[] ProviderAliases { get; set; }
        public Verificationdetail[] VerificationDetails { get; set; }
        public Status Status { get; set; }
        public DateTime DateDownloaded { get; set; }
        public DateTime DateOnboarded { get; set; }
        public DateTime DateUpdated { get; set; }
        public string UpdatedBy { get; set; }

        // Apprenticeship related
        public int? ProviderId { get; set; }
        public int? UPIN { get; set; } // Needed to get LearnerSatisfaction & EmployerSatisfaction from FEChoices
        public string TradingName { get; set; }
        public bool NationalApprenticeshipProvider { get; set; }
        public string MarketingInformation { get; set; }
        public string Alias { get; set; }
        public ProviderType ProviderType { get; set; }


        public Provider(Providercontact[] providercontact, Provideralias[] provideraliases, Verificationdetail[] verificationdetails)
        {
            ProviderContact = providercontact;
            ProviderAliases = provideraliases;
            VerificationDetails = verificationdetails;
        }

    }

    public class Providercontact 
    {
        public string ContactType { get; set; }
        public Contactaddress ContactAddress { get; set; }
        public Contactpersonaldetails ContactPersonalDetails { get; set; }
        public object ContactRole { get; set; }
        public string ContactTelephone1 { get; set; }
        public object ContactTelephone2 { get; set; }
        public string ContactFax { get; set; }
        public string ContactWebsiteAddress { get; set; }
        public string ContactEmail { get; set; }
        public DateTime LastUpdated { get; set; }

        public Providercontact(Contactaddress contactaddress, Contactpersonaldetails contactpersonaldetails)
        {
            ContactAddress = contactaddress;
            ContactPersonalDetails = contactpersonaldetails;
        }
    }

    public class Contactaddress 
    {
        public SAON SAON { get; set; }
        public PAON PAON { get; set; }
        public string StreetDescription { get; set; }
        public object UniqueStreetReferenceNumber { get; set; }
        public object Locality { get; set; }
        public string[] Items { get; set; }
        public int[] ItemsElementName { get; set; }
        public object PostTown { get; set; }
        public string PostCode { get; set; }
        public object UniquePropertyReferenceNumber { get; set; }

    }

    public class SAON
    {
        public object Description { get; set; }

    }

    public class PAON
    {
        public string Description { get; set; }

    }

    public class Contactpersonaldetails 
    {
        public string[] PersonNameTitle { get; set; }
        public string[] PersonGivenName { get; set; }
        public string PersonFamilyName { get; set; }
        public object PersonNameSuffix { get; set; }
        public object PersonRequestedName { get; set; }
    }

    public class Provideralias 
    {
        public object ProviderAlias { get; set; }
        public object LastUpdated { get; set; }
        public Provideralias()
        {
        }
    }

    public class Verificationdetail 
    {
        public string VerificationAuthority { get; set; }
        public string VerificationID { get; set; }
    }
}
