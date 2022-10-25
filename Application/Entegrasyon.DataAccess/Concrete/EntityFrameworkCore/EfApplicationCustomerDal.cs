using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Customers;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using static Amazon.S3.Util.S3EventNotification;
using System.Linq.Expressions;
using Shared.Extensions;
using Entegrasyon.Entity.Customers;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationCustomerDal:EfEntityRepository<Customer,IntegrationDbContext>,IApplicationCustomerDal
{
    private readonly IntegrationDbContext _dbContext;
    public EfApplicationCustomerDal(IntegrationDbContext ctx) : base(ctx)
    {
        _dbContext = ctx;
    }
    
}