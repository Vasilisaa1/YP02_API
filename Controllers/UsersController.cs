using CodeQuest.Context;
using CodeQuest.Model;
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
        private readonly ProfileIconGenerationQueue _iconQueue;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            GigaChatImageService imageService,
            ProfileIconGenerationQueue iconQueue,
            IWebHostEnvironment env)
        {
            _imageService = imageService;
            _iconQueue = iconQueue;
            _env = env;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        [HttpPost("Register")]
        [ApiExplorerSettings(GroupName = "v2")]
        public async Task<ActionResult> Register([FromForm] string username, [FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return StatusCode(400, "Имя пользователя, email и пароль обязательны для заполнения");

            try
            {
                using var context = new UsersContext();
                using var contextLog = new LogContext();

                if (context.Users.Any(x => x.email == email))
                    return StatusCode(400, "Пользователь с таким email уже существует");

                var newUser = new Model.Users
                {
                    username = username,
                    email = email,
                    passwordhash = HashPassword(password),
                    created_at = DateTime.Now,
                    ProfileIconFileName = null,
                    IsIconGenerated = false
                };

                context.Users.Add(newUser);
                await context.SaveChangesAsync();

                // Добавляем в очередь для фоновой генерации
                _iconQueue.EnqueueGeneration(newUser.id, username);

                var log = new Model.Log
                {
                    idUser = newUser.id,
                    whatDo = "Полльзователь с Id " + newUser.id + " прошёл регистрацию.\n",
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                await contextLog.SaveChangesAsync();
                return Ok(new
                {
                    message = "Пользователь успешно зарегистрирован",
                    userId = newUser.id,
                    hasProfileIcon = false,
                    isIconGenerating = true,
                    note = "Иконка профиля генерируется в фоновом режиме"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при регистрации пользователя: {ex.Message}");
            }
        }

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

                // Если файл есть в кэше
                var cachedFileName = _iconQueue.GetGeneratedIconFileName(userId);
                if (!string.IsNullOrEmpty(cachedFileName))
                {
                    return GetPhysicalFileResult(cachedFileName);
                }

                // Если есть в БД
                if (!string.IsNullOrEmpty(user.ProfileIconFileName))
                {
                    return GetPhysicalFileResult(user.ProfileIconFileName);
                }

                // Если иконка еще генерируется
                if (_iconQueue.IsGenerating(userId))
                {
                    return Accepted(new
                    {
                        message = "Иконка профиля все еще генерируется",
                        retryAfter = 30
                    });
                }

                return NotFound("Иконка профиля не найдена");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении иконки: {ex.Message}");
            }
        }

        private FileResult GetPhysicalFileResult(string fileName)
        {
            var imgFolder = Path.Combine(_env.WebRootPath, "img");
            var filePath = Path.Combine(imgFolder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {fileName} не найден в папке img");
            }

            var contentType = GetContentType(fileName);
            return PhysicalFile(filePath, contentType);
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        [HttpGet("IconGenerationStatus/{userId}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetIconGenerationStatus(int userId)
        {
            using var context = new UsersContext();
            var user = context.Users.FirstOrDefault(u => u.id == userId);

            if (user == null)
                return NotFound("Пользователь не найден");

            var isGenerating = _iconQueue.IsGenerating(userId);
            var hasIcon = !string.IsNullOrEmpty(user.ProfileIconFileName);

            return Ok(new
            {
                userId,
                isGenerating,
                hasIcon,
                fileName = user.ProfileIconFileName,
                canRetry = !isGenerating && !hasIcon
            });
        }

        [HttpPost("RetryIconGeneration/{userId}")]
        [ApiExplorerSettings(GroupName = "v3")]
        public ActionResult RetryIconGeneration(int userId)
        {
            using var context = new UsersContext();
            var user = context.Users.FirstOrDefault(u => u.id == userId);

            if (user == null)
                return NotFound("Пользователь не найден");

            if (_iconQueue.IsGenerating(userId))
            {
                return BadRequest("Иконка уже генерируется");
            }

            // Удаляем старый файл если он существует
            if (!string.IsNullOrEmpty(user.ProfileIconFileName))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, "img", user.ProfileIconFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Очищаем поле в БД
                user.ProfileIconFileName = null;
                user.IsIconGenerated = false;
                context.SaveChanges();
            }

            _iconQueue.EnqueueGeneration(userId, user.username);

            return Ok(new
            {
                message = "Запущена повторная генерация иконки",
                userId,
                isGenerating = true
            });
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
                    u.created_at,
                    hasProfileIcon = !string.IsNullOrEmpty(u.ProfileIconFileName),
                    profileIconUrl = !string.IsNullOrEmpty(u.ProfileIconFileName)
                        ? $"/api/Users/ProfileIcon/{u.id}"
                        : null,
                    fileName = u.ProfileIconFileName
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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
                
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = user.id,
                    whatDo = "Полльзователь с Id " + user.id + " вошёл в приложение.",
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                contextLog.SaveChangesAsync();

                return Ok(new { token = tokenHandler.WriteToken(token) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("Update")]
        [ApiExplorerSettings(GroupName = "v3")]
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
                
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = existingUser.id,
                    whatDo = "Полльзователь с Id " + existingUser.id + " обновил данные.",
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                contextLog.SaveChangesAsync();
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
                    hasProfileIcon = !string.IsNullOrEmpty(user.ProfileIconFileName),
                    profileIconUrl = !string.IsNullOrEmpty(user.ProfileIconFileName)
                        ? $"/api/Users/ProfileIcon/{user.id}"
                        : null,
                    fileName = user.ProfileIconFileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении данных пользователя: {ex.Message}");
            }
        }

        [HttpDelete("Delete")]
        [ApiExplorerSettings(GroupName = "v4")]
        public ActionResult DeleteUser([FromQuery] int id)
        {
            try
            {
                using var context = new UsersContext();
                var user = context.Users.FirstOrDefault(u => u.id == id);
                if (user == null)
                    return NotFound("Пользователь не найден");

                // Удаляем файл иконки если он существует
                if (!string.IsNullOrEmpty(user.ProfileIconFileName))
                {
                    var filePath = Path.Combine(_env.WebRootPath, "img", user.ProfileIconFileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                context.Users.Remove(user);
                context.SaveChanges();
                
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = user.id,
                    whatDo = "Полльзователь с Id " + user.id + " удалил аккаунт.",
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                contextLog.SaveChangesAsync();
                return Ok("Пользователь успешно удален");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении пользователя: {ex.Message}");
            }
        }
    }
}