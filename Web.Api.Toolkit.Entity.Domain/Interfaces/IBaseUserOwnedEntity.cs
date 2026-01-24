namespace Web.Api.Toolkit.Entity.Domain.Interfaces
{
    public interface IBaseUserOwnedEntity<TUser> : IBaseEntity
    {
        public TUser User { get; set; }
        public string UserId { get; set; }
    }
}
