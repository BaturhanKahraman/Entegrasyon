namespace Shared.DTO
{
    public class Rule
    {
        public int Id { get; set; }
        public string RuleName { get; set; }//translate afterwards
        public bool IsActive { get; set; }
        public string RuleSymbol { get; set; }
        public int ComparisionValue { get; set; }
        public ICollection<RuleValidation> RuleValidations { get; set; }
    }
}
