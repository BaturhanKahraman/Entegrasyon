using Entegrasyon.Entity.Sales;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ISaleDal : IEntityRepository<Sale>
{
}