using Entegrasyon.Entity.Token;
using Shared.Abstract;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationTokenDal : IEntityRepository<ApplicationJwtToken>
{
}