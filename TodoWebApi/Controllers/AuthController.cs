using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using TodoWebApi.DTOs;
using TodoWebApi.Models;
using TodoWebApi.Models.DbContexts;

namespace TodoWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {


        private readonly MasterDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration, MasterDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        //[Authorize]
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(LoginRequest request)
        {
            if (UserNameExists(request.Username))
            {
                return Problem("Username is Exists");
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            var user = new User
            {
                Username = request.Username,
                PasswordHash = Convert.ToBase64String(passwordHash),
                PasswordSlat = Convert.ToBase64String(passwordSalt),
            };
            
            if (_context.Users == null)
            {
                return Problem("Entity set 'DemoContext.Users'  is null.");
            }
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Register", new { id = user.Id }, user);
        }


        [HttpPost("login")]
        public async Task<ActionResult<BaseResponse<LoginResponse>>> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return BadRequest("User not found.");
            }
            if (user.Username != request.Username)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, Convert.FromBase64String(user.PasswordHash), Convert.FromBase64String(user.PasswordSlat)))
            {
                return BadRequest("Wrong password.");
            }

            string token = CreateToken(user);

            LoginResponse response = new LoginResponse
            {
                Token = token
            };
            return Ok(response);
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
                .GetBytes(_configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
        private bool UserNameExists(string Username)
        {
            return (_context.Users?.Any(e => e.Username == Username)).GetValueOrDefault();
        }
    }
}
