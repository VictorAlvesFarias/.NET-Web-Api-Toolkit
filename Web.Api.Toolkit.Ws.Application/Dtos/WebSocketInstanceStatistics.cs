namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class WebSocketInstanceStatistics
    {
        public int TotalInstances { get; set; }
        public int ActiveInstances { get; set; }
        public int TotalClients { get; set; }
        public int PendingInvites { get; set; }
        public IEnumerable<object> Instances { get; set; }
    }
}