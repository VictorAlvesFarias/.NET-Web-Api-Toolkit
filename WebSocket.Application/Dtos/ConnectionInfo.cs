namespace Web.Api.Toolkit.Ws.Application.Dtos
{
    public class ConnectionInfo
    {
        public string Url { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}