using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Shared;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Abstract;

public interface IApplicationCustomerDal : IEntityRepository<Customer>
{
}