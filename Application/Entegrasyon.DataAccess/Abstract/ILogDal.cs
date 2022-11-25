using Entegrasyon.Entity.Logs;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ILogDal:IEntityRepository<ApplicationLog>
{
    
}