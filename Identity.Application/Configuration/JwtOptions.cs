using Microsoft.IdentityModel.Tokens;

namespace Identity.Application.Configuration
{
    public class JwtOptions
    {

        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenExpiration { get; set; }
        public string SecurityKey { get; set; }
        public SigningCredentials SigningCredentials { get; set; }
    }
}
