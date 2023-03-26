namespace Entegrasyon.Entity.Dtos.Product;

public sealed record GetProductPageableDto(string FullTextSearchKey,int PageIndex=0,int PageSize=50);