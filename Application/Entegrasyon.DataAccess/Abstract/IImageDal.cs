using Entegrasyon.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IImageDal : IEntityRepository<Image>
{
    Task AddRange(IEnumerable<Image> imgs);
}