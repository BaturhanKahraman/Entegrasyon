using Entegrasyon.Entity.Logs;

namespace Entegrasyon.Entity.Dtos.Log
{
    public class ApplicationLogDetailDto
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public LogType LogType { get; set; }
        public LogAction LogAction { get; set; }
        public string IpAddress { get; set; }
        public string UserInfos { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
