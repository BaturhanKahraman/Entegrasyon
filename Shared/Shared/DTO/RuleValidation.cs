namespace Shared.DTO
{
    public class RuleValidation
    {
        public int Id { get; set; }
        public int ApplicableRuleId { get; set; }
        public Rule ApplicableRule { get; set; }
        public int ValidationObjectId { get; set; }
        public DtoValidateObject ValidationObject { get; set; }
        public string RuleValue { get; set; }
    }
}
