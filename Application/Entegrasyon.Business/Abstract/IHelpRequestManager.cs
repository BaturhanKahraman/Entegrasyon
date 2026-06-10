using Entegrasyon.Entity.Dtos.Help;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IHelpRequestManager
{
    /// <summary>Kullanıcı yardım formundan talep oluşturur.</summary>
    Task<IResult> CreateAsync(CreateHelpRequestDto dto, Guid userId);

    /// <summary>Admin paneli — geçerli tenant'in tüm yardım taleplerini listeler (yeni → eski).</summary>
    Task<List<HelpRequestListDto>> GetAllAsync();

    /// <summary>Admin paneli — tek talep detayı.</summary>
    Task<IDataResult<HelpRequestDetailDto>> GetDetailAsync(int id);

    /// <summary>Admin paneli — talebi çözüldü olarak işaretler.</summary>
    Task<IResult> MarkResolvedAsync(int id);
}
