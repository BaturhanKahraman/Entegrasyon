using Entegrasyon.Entity.Labels;

namespace Entegrasyon.Entity.Dtos.Label;

public sealed record LabelTemplateDto(
    Guid Id,
    string Name,
    LabelType Type,
    int WidthMm,
    int HeightMm,
    int Dpi,
    List<LabelElement> Elements,
    bool IsDefault);

public sealed record SaveLabelTemplateDto(
    Guid? Id,
    string Name,
    LabelType Type,
    int WidthMm,
    int HeightMm,
    int Dpi,
    List<LabelElement> Elements,
    bool IsDefault);
