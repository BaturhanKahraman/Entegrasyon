using Entegrasyon.Entity.Labels;
using Entegrasyon.PrintAgent.Contracts.Labels;

namespace Entegrasyon.Business.Labels;

public interface ILabelGenerator
{
    string GenerateProductBarcode(ProductBarcodeLabelData data);
    string GenerateShelfLabel(ShelfLabelData data);
    string GenerateFromTemplate(List<LabelElement> elements, int widthDots, int heightDots, Dictionary<string, string> fieldValues);
}
