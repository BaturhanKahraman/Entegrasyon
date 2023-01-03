namespace Shared.DTO
{
    public class DtoValidateObject
    {
        public int Id { get; set; }
        public string DtoName { get; set; }
        public IEnumerable<DtoProperty> DtoProperties { get; set; }
    }
}
