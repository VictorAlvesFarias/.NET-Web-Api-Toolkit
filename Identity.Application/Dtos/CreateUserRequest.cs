namespace Application.Dtos.User
{
    public class CreateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordConfirm { get; set; }
        public string Password { get; set; }
    }
}
