namespace Web.Api.Toolkit.Helpers.Domain.Interfaces
{
    public interface IBaseUserOwnedEntity<TUser> : IBaseEntity
    {
        public TUser User { get; set; }
        public string UserId { get; set; }
    }
}
