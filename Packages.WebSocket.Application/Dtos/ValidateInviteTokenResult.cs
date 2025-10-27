namespace Packages.Ws.Application.Dtos
{
    public class ValidateInviteTokenResult
    {
        public ValidateInviteTokenResult(bool valid, string message, ConnectionInvite? invite)
        {
            Valid = valid;
            Message = message;
            Invite = invite;
        }
        public bool Valid { get; set; }
        public string Message { get; set; }
        public ConnectionInvite Invite { get; set; }
    }
}