namespace Entegrasyon.Entity.Dtos.Brand;

public record GetCategoryDetailsPageDto(string CategoryName=null,int PageIndex=0,int ItemCount=50);