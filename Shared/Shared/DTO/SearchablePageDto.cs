namespace Shared.DTO;

public record SearchablePageDto(string FullTextSearchKey,int PageIndex = 0,int PageSize = 50);