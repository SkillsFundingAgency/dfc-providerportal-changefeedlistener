using FluentValidation;

namespace Dfc.ProviderPortal.ChangeFeedListener.Models.Foac
{
    public class FaocValidator : AbstractValidator<FaocEntry>
    {
        public FaocValidator()
        {
            RuleFor(entry => entry.id).NotNull();
            RuleFor(entry => entry.NotionalNVQLevelv2).NotNull();
        }
    }
}
