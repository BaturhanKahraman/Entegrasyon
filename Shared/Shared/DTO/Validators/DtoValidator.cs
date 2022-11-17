using Microsoft.EntityFrameworkCore;
using Shared.DTO.Attributes;
using Shared.Extensions;
using System.Security.Cryptography.X509Certificates;

namespace Shared.DTO.Validators
{
    public class DtoValidator
    {
        private readonly DbContext _dbContext;
        //rules come from db
        public async Task<DtoValidationResult> Validate<T>(T dto)
            where T : IValidatebleDto
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            string dtoName = dto.GetType().GetClassName();
            var dtoValidateObject = await _dbContext.Set<DtoValidateObject>()
                .Where(x => string.Equals(x.DtoName, dtoName))
                .Include(x => x.DtoProperties)
                    .ThenInclude(x => x.RuleValidations)
                        .ThenInclude(x => x.ApplicableRule)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();
            var typeProperties = dto.GetType()
                .GetProperties()
                .Where(tp =>
                    dtoValidateObject.DtoProperties.Any(x => string.Equals(tp.Name, x.PropertyName,StringComparison.OrdinalIgnoreCase))
                    && !Attribute.IsDefined(tp, typeof(DtoIgnoreAttribute))
                );
            foreach (var dtoProperty in dtoValidateObject.DtoProperties)
            {
                var dtoOrginalProperty = dto.GetType().GetProperties().FirstOrDefault(x => string.Equals(x.Name,dtoProperty.PropertyName,StringComparison.OrdinalIgnoreCase));
                
                var rules = dtoProperty.RuleValidations.Select(x => x.ApplicableRule);
                foreach (var rule in rules)
                {
                    var value =dtoOrginalProperty.GetValue(dto);
                    if (value == null)
                        continue;
                    var valueType = value.GetType();//can this return object ? if it can gonna be write casting extension.
                    if (valueType.IsValueType && valueType is IComparable valueComparable)
                    {
                        //valueComparable.CompareTo(rule.);
                    }
                }
            }
            throw new NotImplementedException();
        }
    }
}
