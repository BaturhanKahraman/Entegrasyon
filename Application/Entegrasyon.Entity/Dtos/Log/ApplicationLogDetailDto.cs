using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Dtos.Log
{
    public sealed class ApplicationLogDetailDto
    {
        public long Id { get; set; }
        public string Content { get; set; } = null!;
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
        public LogType LogType { get; set; }
        public LogAction LogAction { get; set; }
        public string IpAddress { get; set; } = null!;
        public string UserInfos { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
