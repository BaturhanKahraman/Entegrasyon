using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfNotificationDal : EfEntityRepository<Notification,IntegrationDbContext>, INotificationDal
{
    public EfNotificationDal(IntegrationDbContext ctx) : base(ctx)
    {
    }


}