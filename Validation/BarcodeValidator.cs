using FluentValidation;

namespace GiftCertificateService.Validation
{
    public class BarcodesValidator : AbstractValidator<List<string>?>
    {
        public BarcodesValidator()
        {
            //only latin symbols and numbers, length is only 11
            RuleForEach(x => x).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Cert's barcode can't be empty")
                .Length(11).WithMessage("Cert's barcode should be 11 symbols length")
                .Matches("^[A-Za-z0-9]+$").WithMessage("Cert's barcode is in wrong format - only latin symbols and digits are allowed");
        }
    }
}
