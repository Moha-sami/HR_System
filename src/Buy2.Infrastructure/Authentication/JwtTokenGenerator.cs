using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Buy2.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Buy2.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string email, string role)
    {
        // 1. Read secret key from configuration (or use default if missing)
        string? secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            secretKey = "Buy2HRMS_SuperSecretKey_ForJWTTokenGeneration_2026";
        }

        // 2. Read issuer and audience from configuration
        string? issuer = _configuration["JwtSettings:Issuer"];
        if (string.IsNullOrEmpty(issuer))
        {
            issuer = "Buy2.Api";
        }

        string? audience = _configuration["JwtSettings:Audience"];
        if (string.IsNullOrEmpty(audience))
        {
            audience = "Buy2.Client";
        }

        // 3. Read token expiration in minutes (default 8 hours)
        int expiryMinutes = 480;
        string? expiryConfig = _configuration["JwtSettings:ExpiryMinutes"];
        if (!string.IsNullOrEmpty(expiryConfig))
        {
            int.TryParse(expiryConfig, out expiryMinutes);
        }

        // 4. Create list of user claims (identity information)
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        claims.Add(new Claim(ClaimTypes.Email, email));
        claims.Add(new Claim(ClaimTypes.Role, role));
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        // 5. Convert secret key string to bytes and build security credentials
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(keyBytes);
        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // 6. Create the token descriptor with all details
        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor();
        tokenDescriptor.Subject = new ClaimsIdentity(claims);
        tokenDescriptor.Issuer = issuer;
        tokenDescriptor.Audience = audience;
        tokenDescriptor.Expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
        tokenDescriptor.SigningCredentials = credentials;

        // 7. Generate and write the JWT token string
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string jwtTokenString = tokenHandler.WriteToken(token);

        return jwtTokenString;
    }
}
