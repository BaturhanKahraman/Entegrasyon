using Entegrasyon.Entity.Dtos.Shipping;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class TrackShipmentValidator : AbstractValidator<TrackShipmentDto>
{
    public TrackShipmentValidator()
    {
        RuleFor(x => x.TrackingNumber)
            .NotEmpty().WithMessage("Takip numarasi bos olamaz");

        RuleFor(x => x.CargoCompanyId)
            .GreaterThan(0).WithMessage("Gecerli bir kargo sirketi secilmelidir");
    }
}
