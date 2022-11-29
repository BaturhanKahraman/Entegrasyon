using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore
{
    public class EfAttributeKeyValueDal : EfEntityRepository<AttributeKeyValue, IntegrationDbContext>, IAttributeKeyValueDal
    {
        private readonly IntegrationDbContext _ctx;
        public EfAttributeKeyValueDal(IntegrationDbContext ctx) : base(ctx)
        {
            _ctx= ctx;
        }
        //maybe needs optimisation ?
        public async Task<IEnumerable<string>> ValidateKeyValues(IEnumerable<AttributeKeyValue> attributeKeyValues)
        {
            var attrkv = attributeKeyValues.ToHashSet();
            HashSet<string> results = new HashSet<string>();
            var categoryIds = attrkv.Select(x => x.CategoryAttributeId);
            var attrs =await _ctx.CategoryAttributes
                .AsNoTracking()
                .AsSplitQuery()
                .Where(ca => categoryIds.Contains(ca.Id) && ca.Required)
                .Select(ca => new ValueTuple<int,string>(ca.Id,ca.CategoryAttributeKey))
                .ToArrayAsync();
            foreach (var item in attrkv.Where(x=>attrs.Any(z=>z.Item1==x.CategoryAttributeId)))
            {
                if((item.AttributeValueId.HasValue && item.AttributeValueId!=0) || !string.IsNullOrEmpty(item.CustomValue))
                    continue;
                results.Add(attrs.First(a=>a.Item1==item.CategoryAttributeId).Item2);
            }
            return results;
        }
    }
}
