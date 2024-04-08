namespace Entegrasyon.Entity.Dtos.Users
{
    public sealed class EditRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> Claims { get; set; }
    }
}
