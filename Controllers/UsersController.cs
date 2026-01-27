using CodeQuest.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CodeQuest.Controllers
{
    [ApiController] 
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private const string SECRET_KEY = "SuperSecretKey12345!";

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        [HttpPost("Register")]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public ActionResult Register([FromForm] string username, [FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return StatusCode(400, "Имя пользователя, email и пароль обязательны для заполнения");

            try
            {
                using var context = new UsersContext();

         
                if (context.Users.Any(x => x.email == email))
                    return StatusCode(400, "Пользователь с таким email уже существует");

   
                var newUser = new Model.Users
                {
                    username = username,
                    email = email,
                    passwordhash = HashPassword(password),
                    created_at = DateTime.Now
                };

                context.Users.Add(newUser);
                context.SaveChanges();

                return Ok("Пользователь успешно зарегистрирован");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при регистрации пользователя");
            }
        }


        [HttpPost("Login")]
        [ApiExplorerSettings(GroupName = "v2")]
        public ActionResult Login(string email, string password)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(x => x.email == email);
                if (user == null)
                    return Unauthorized("Пользователь не найден");

                if (user.passwordhash != HashPassword(password))
                    return Unauthorized("Неверный пароль");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(SECRET_KEY);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                        new Claim(ClaimTypes.Email, user.email)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new { token = tokenHandler.WriteToken(token) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult<IEnumerable<Model.Users>> GetAllUsers()
        {
            try
            {
                using var context = new UsersContext();
                var users = context.Users.ToList();


                var result = users.Select(u => new
                {
                    u.id,
                    u.username,
                    u.email,
                    u.created_at
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetUserById(int id)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == id);
                if (user == null)
                    return NotFound("Пользователь не найден");

                return Ok(new
                {
                    user.id,
                    user.username,
                    user.email,
                    user.created_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("Update")]
        [ApiExplorerSettings(GroupName = "v3")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult UpdateUser(
       [FromQuery] int id,
       [FromForm] string? username = null,
       [FromForm] string? email = null,
       [FromForm] string? password = null)
        {
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password))
                return BadRequest("Не указаны данные для обновления");

            try
            {
                using var context = new UsersContext();
                var existingUser = context.Users.FirstOrDefault(u => u.id == id);
                if (existingUser == null)
                    return NotFound("Пользователь не найден");

        
                if (!string.IsNullOrEmpty(email) && email != existingUser.email)
                {
                    if (context.Users.Any(u => u.email == email && u.id != id))
                        return BadRequest("Пользователь с таким email уже существует");
                }

        
                if (!string.IsNullOrEmpty(username))
                    existingUser.username = username;

                if (!string.IsNullOrEmpty(email))
                    existingUser.email = email;

                if (!string.IsNullOrEmpty(password))
                    existingUser.passwordhash = HashPassword(password);

                context.SaveChanges();
                return Ok("Данные пользователя успешно обновлены");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обновлении пользователя: {ex.Message}");
            }
        }
        [HttpGet("GetCurrentUser")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetCurrentUser()
        {
            try
            {
                // Получаем ID пользователя из JWT токена
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized("Пользователь не авторизован");

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    return BadRequest("Неверный формат ID пользователя");

                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == userId);
                if (user == null)
                    return NotFound("Пользователь не найден");

                return Ok(new
                {
                    user.id,
                    user.username,
                    user.email,
                    user.created_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении данных пользователя: {ex.Message}");
            }
        }

        [HttpDelete("Delete")]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public ActionResult DeleteUser([FromQuery] int id)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == id);
                if (user == null)
                    return NotFound("Пользователь не найден");

                context.Users.Remove(user);
                context.SaveChanges();
                return Ok("Пользователь успешно удален");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении пользователя: {ex.Message}");
            }
        }
    }
}
