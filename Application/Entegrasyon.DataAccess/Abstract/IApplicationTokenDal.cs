using Entegrasyon.Entity.Token;
using Shared;
using Shared.User.Token;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationTokenDal : IEntityRepository<RootJwtToken>
{
}