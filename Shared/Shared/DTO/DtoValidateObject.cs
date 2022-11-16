namespace Shared.DTO
{
    public class DtoValidateObject
    {
        public int Id { get; set; }
        public string DtoName { get; set; }
        public ICollection<DtoProperty> DtoProperties { get; set; }
    }
}
