using Entegrasyon.Entity.Dtos.Reports;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class NotifyDelayedShipmentsDtoValidator : AbstractValidator<NotifyDelayedShipmentsDto>
{
    public NotifyDelayedShipmentsDtoValidator()
    {
        RuleFor(x => x.ShipmentTrackingIds)
            .NotNull().WithMessage("Lütfen en az bir gönderi seçin.")
            .Must(ids => ids is { Count: > 0 }).WithMessage("Lütfen en az bir gönderi seçin.");
    }
}
