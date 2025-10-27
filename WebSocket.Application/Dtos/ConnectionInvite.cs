namespace Ws.Application.Dtos
{
    public class ConnectionInvite
    {
        public string Token { get; set; }
        public string InstanceId { get; set; }
        public Guid ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}