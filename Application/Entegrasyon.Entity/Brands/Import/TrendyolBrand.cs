namespace Entegrasyon.Entity.Brands.Import;

public class TrendyolBrandRoot
{
    public List<TrendyolBrand> Brands { get; set; }
}

public class TrendyolBrand
{
    public int Id { get; set; }
    public string Name { get; set; }
}