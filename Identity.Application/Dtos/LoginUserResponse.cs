namespace Application.Dtos.User
{
    public class LoginUserResponse
    {
        public int ExpirationTokenTime { get; set; }
        public DateTime ExpectedExpirationTokenDateTime { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
        public string Id { get; set; }
    }
}
