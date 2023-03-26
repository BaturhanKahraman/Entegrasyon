namespace Shared.DTO
{
    public class DtoProperty
    {
        public int Id { get; set; }
        public string PropertyName { get; set; }
        public bool IsRequired { get; set; }
        public int DtoValidateObjectId { get; set; }
        public DtoValidateObject DtoValidateObject { get; set; }
        public IEnumerable<RuleValidation> RuleValidations { get; set; }
    }
}
