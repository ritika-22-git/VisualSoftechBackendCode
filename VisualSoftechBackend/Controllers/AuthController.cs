using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VisualSoftechBackend.DAO;
using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _config;
        public AuthController(IUserRepository users, IConfiguration config) { _users = users; _config = config; }

        public class LoginRequest { public string Username { get; set; } = null!; public string Password { get; set; } = null!; }
        public class RegisterRequest { public string Username { get; set; } = null!; public string Password { get; set; } = null!; public string? DisplayName { get; set; } }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("username and password required");

            var existing = await _users.GetByUsernameAsync(req.Username);
            if (existing != null) return Conflict("username already exists");

            // hash password with bcrypt
            string hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);
            var user = new User { Username = req.Username, PasswordHash = hashed, DisplayName = req.DisplayName };
            var id = await _users.CreateAsync(user);
            return Ok(new { id, username = req.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("username and password required");

            var user = await _users.GetByUsernameAsync(req.Username);
            if (user == null) return Unauthorized("invalid username or password");

            // verify bcrypt
            bool ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!ok) return Unauthorized("invalid username or password");

            // generate JWT
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.GetValue<string>("Key")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim("displayName", user.DisplayName ?? string.Empty)
        };

            var token = new JwtSecurityToken(
                issuer: jwt.GetValue<string>("Issuer"),
                audience: jwt.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwt.GetValue<int>("ExpiryMinutes")),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { token = tokenString, expires = token.ValidTo, username = user.Username });
        }
    }
}
