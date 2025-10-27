namespace Ws.Application.Dtos
{
    public class ClientRegistry
    {
        public Guid ClientId { get; set; }
        public string InstanceId { get; set; }
        public string UserId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}