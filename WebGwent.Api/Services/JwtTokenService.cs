using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebGwent.Api.Accounts;

namespace WebGwent.Api.Services;

public sealed class JwtTokenService(IConfiguration config)
{
    public const string DisplayNameClaim = "display_name";
    public const string AvatarClaim = "avatar";

    public string CreateToken(PlayerAccount account)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = config["Jwt:Issuer"] ?? "WebGwent";
        var audience = config["Jwt:Audience"] ?? "WebGwent";
        var days = int.TryParse(config["Jwt:Days"], out var d) ? d : 30;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(DisplayNameClaim, account.DisplayName),
            new(AvatarClaim, account.AvatarId)
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(days),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
