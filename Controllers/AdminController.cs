using CodeQuest.Context;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {

        /// <summary>
        /// Авторизация администратора
        /// </summary>
        [HttpPost("Login")]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public ActionResult Login([FromForm] string login, [FromForm] string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Логин и пароль обязательны для заполнения");
            }

            try
            {
                using var context = new AdminContext();


                var admin = context.Admins.FirstOrDefault(x => x.login == login && x.password == password);

                if (admin == null)
                {
                    return Unauthorized("Неверный логин или пароль");
                }

                return Ok(new { message = "Авторизация успешна", id = admin.id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при авторизации: {ex.Message}");
            }
        }
    }
}

