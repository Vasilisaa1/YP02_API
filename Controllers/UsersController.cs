using CodeQuest.Context;
using CodeQuest.Services;
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
        private readonly GigaChatImageService _imageService;

        public UsersController(GigaChatImageService imageService)
        {
            _imageService = imageService;
        }

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
        public async Task<ActionResult> Register([FromForm] string username, [FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return StatusCode(400, "Имя пользователя, email и пароль обязательны для заполнения");

            try
            {
                using var context = new UsersContext();

                if (context.Users.Any(x => x.email == email))
                    return StatusCode(400, "Пользователь с таким email уже существует");

                // Генерируем иконку профиля
                byte[]? profileIcon = await _imageService.GenerateProfileIconAsync(username);

                var newUser = new Model.Users
                {
                    username = username,
                    email = email,
                    passwordhash = HashPassword(password),
                    created_at = DateTime.Now,
                    ProfileIcon = profileIcon, // Может быть null
                    ProfileIconMimeType = profileIcon != null ? "image/png" : null
                };

                context.Users.Add(newUser);
                context.SaveChanges();

                return Ok(new
                {
                    message = "Пользователь успешно зарегистрирован",
                    userId = newUser.id,
                    hasProfileIcon = profileIcon != null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при регистрации пользователя: {ex.Message}");
            }
        }

        // Добавляем метод для получения иконки профиля
        [HttpGet("ProfileIcon/{userId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetProfileIcon(int userId)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == userId);

                if (user == null)
                    return NotFound("Пользователь не найден");

                if (user.ProfileIcon == null || user.ProfileIcon.Length == 0)
                {
                    return NotFound("Иконка профиля не найдена");
                }

                return File(user.ProfileIcon, user.ProfileIconMimeType ?? "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении иконки: {ex.Message}");
            }
        }

        // Добавляем метод для обновления иконки профиля
        [HttpPost("UpdateProfileIcon")]
        [ApiExplorerSettings(GroupName = "v3")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateProfileIcon([FromQuery] int userId)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == userId);

                if (user == null)
                    return NotFound("Пользователь не найден");

                // Генерируем новую иконку
                var newIcon = await _imageService.GenerateProfileIconAsync(user.username);

                user.ProfileIcon = newIcon;
                user.ProfileIconMimeType = newIcon != null ? "image/png" : null;

                context.SaveChanges();

                return Ok(new
                {
                    message = "Иконка профиля успешно обновлена",
                    hasProfileIcon = newIcon != null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обновлении иконки: {ex.Message}");
            }
        }

        // Обновляем метод GetUserById для включения информации об иконке
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
                    user.created_at,
                    hasProfileIcon = user.ProfileIcon != null && user.ProfileIcon.Length > 0,
                    profileIconUrl = user.ProfileIcon != null
                        ? $"/api/Users/ProfileIcon/{user.id}"
                        : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Обновляем метод GetAllUsers
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
                    u.created_at,
                    hasProfileIcon = u.ProfileIcon != null && u.ProfileIcon.Length > 0,
                    profileIconUrl = u.ProfileIcon != null
                        ? $"/api/Users/ProfileIcon/{u.id}"
                        : null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Остальные методы оставляем без изменений
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
                    user.created_at,
                    hasProfileIcon = user.ProfileIcon != null && user.ProfileIcon.Length > 0,
                    profileIconUrl = user.ProfileIcon != null
                        ? $"/api/Users/ProfileIcon/{user.id}"
                        : null
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