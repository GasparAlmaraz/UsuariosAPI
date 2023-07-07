using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UsuariosAPI.Context;
using UsuariosAPI.Models;

namespace UsuariosAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public LoginController (ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var currentUser = GetCurrentUser();
            return Ok("Saludos " + currentUser.Rol + " " + currentUser.Name);
        }

        // POST: api/Login
        [HttpPost]
        public IActionResult Login(LoginUser userLogin)
        {
            var user = Aunthenticate(userLogin);

            if(user != null)
            {
                var token = Generate(user);

                return Ok("Usuario Logueado, token: " + token);
            }

            return NotFound("Usuario no Encontrado");
        }

        private Usuario Aunthenticate(LoginUser userLogin)
        {
            var user = _context.Usuarios.FirstOrDefault(u => u.Correo.ToLower() == userLogin.Correo.ToLower() 
                && u.Password.ToLower() == userLogin.Password.ToLower());
            return user;
        }

        private string Generate(Usuario user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Name),
                new Claim(ClaimTypes.Email, user.Correo),
                new Claim(ClaimTypes.Role, user.Rol),
            };

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private Usuario GetCurrentUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if(identity != null)
            {
                var userClaims = identity.Claims;

                return new Usuario
                {
                    Name = userClaims.FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier)?.Value,
                    Correo = userClaims.FirstOrDefault(o => o.Type == ClaimTypes.Email)?.Value,
                    Rol = userClaims.FirstOrDefault(o => o.Type == ClaimTypes.Role)?.Value
                };
            }
            return null;
        }
    }
}
