namespace Identity.Application.Dtos
{
    public class PutUserRequest
    {
        public string IdentityUserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
    }
}
