// CodeQuest/Controllers/UserProgressController.cs
using CodeQuest.Context;
using CodeQuest.Model;
using CodeQuest.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [Route("api/UserProgress")]
    public class UserProgressController : Controller
    {
        private readonly AchievementService _achievementService;

        public UserProgressController()
        {
            var context = new QuizContext();
            _achievementService = new AchievementService(context);
        }

        /// <summary>
        /// Получение списка прогресса пользователей
        /// </summary>
        [Route("List")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(List<Model.UserProgress>), 200)]
        [ProducesResponseType(500)]
        public ActionResult List()
        {
            try
            {
                IEnumerable<Model.UserProgress> progress = new UserProgressContext().UserProgress;
                return Json(progress);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение записи прогресса по ID
        /// </summary>
        [Route("Item")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(Model.UserProgress), 200)]
        [ProducesResponseType(500)]
        public ActionResult Item(int id)
        {
            try
            {
                Model.UserProgress record = new UserProgressContext().UserProgress.FirstOrDefault(x => x.id == id);
                if (record == null)
                    return NotFound($"Запись с ID {id} не найдена");

                return Json(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Добавление нового прогресса
        /// </summary>
        [Route("Add")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> Add([FromForm] Model.UserProgress progress)
        {
            try
            {
                using var context = new UserProgressContext();

                // Всегда устанавливаем дату на сервере
                if (progress.is_completed)
                {
                    progress.completed_at = DateTime.Now;
                }
                else
                {
                    progress.completed_at = null;
                }

                // Устанавливаем created_at, если не указан
                if (!progress.completed_at.HasValue)
                {
                    progress.completed_at = DateTime.Now;
                }

                // Если total_questions не указан, устанавливаем значение по умолчанию
                if (progress.total_questions <= 0)
                {
                    progress.total_questions = 5;
                }

                context.UserProgress.Add(progress);
                context.SaveChanges();

                // Логирование
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = progress.user_id,
                    whatDo = $"Пользователь с Id {progress.user_id} прошёл тест {progress.topic_id}",
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                contextLog.SaveChangesAsync();

                // Проверяем и выдаем достижения
                if (progress.is_completed)
                {
                    var quizContext = new QuizContext();
                    var achievementService = new AchievementService(quizContext);
                    await achievementService.CheckAndGrantAchievements(progress.user_id);
                }

                return Ok(new
                {
                    message = "Прогресс добавлен",
                    progressId = progress.id,
                    achievementsChecked = progress.is_completed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// Обновление прогресса
        /// </summary>
        [Route("Update")]
        [HttpPut]
        [ApiExplorerSettings(GroupName = "v3")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> Update(int id, [FromForm] Model.UserProgress progress)
        {
            try
            {
                using (UserProgressContext context = new UserProgressContext())
                {
                    var existing = context.UserProgress.FirstOrDefault(x => x.id == id);
                    if (existing == null)
                        return NotFound($"Прогресс с ID {id} не найден");

                    var wasCompleted = existing.is_completed;

                    existing.user_id = progress.user_id;
                    existing.topic_id = progress.topic_id;
                    existing.is_completed = progress.is_completed;
                    existing.score = progress.score;
                    existing.total_questions = progress.total_questions;

                    // Обновляем completed_at при завершении
                    if (progress.is_completed && !existing.completed_at.HasValue)
                    {
                        existing.completed_at = DateTime.Now;
                    }
                    else if (!progress.is_completed)
                    {
                        existing.completed_at = null;
                    }

                    context.SaveChanges();

                    // Проверяем достижения, если статус изменился на завершенный
                    if (progress.is_completed && !wasCompleted)
                    {
                        var quizContext = new QuizContext();
                        var achievementService = new AchievementService(quizContext);
                        await achievementService.CheckAndGrantAchievements(progress.user_id);
                    }

                    return Ok(new
                    {
                        message = "Прогресс обновлен",
                        achievementsChecked = (progress.is_completed && !wasCompleted)
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение прогресса пользователя
        /// </summary>
        [Route("GetUserProgress")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetUserProgress(int userId)
        {
            try
            {
                using var context = new UserProgressContext();

                var progress = context.UserProgress
                    .Where(up => up.user_id == userId)
                    .OrderByDescending(up => up.completed_at)
                    .ToList();

                var summary = new
                {
                    total_completed = progress.Count(up => up.is_completed),
                    total_score = progress.Where(up => up.is_completed).Sum(up => up.score),
                    average_score = progress.Where(up => up.is_completed).Any()
                        ? progress.Where(up => up.is_completed).Average(up => up.score)
                        : 0,
                    last_completed = progress.FirstOrDefault(up => up.is_completed)?.completed_at
                };

                return Json(new
                {
                    success = true,
                    progress = progress,
                    summary = summary,
                    count = progress.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Получение достижений пользователя
        /// </summary>
        [Route("GetUserAchievements")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(500)]
        public ActionResult GetUserAchievements(int userId)
        {
            try
            {
                var achievements = _achievementService.GetUserAchievements(userId);
                var stats = _achievementService.GetAchievementStats(userId);

                return Json(new
                {
                    success = true,
                    achievements = achievements,
                    stats = stats,
                    count = achievements.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Ручная проверка достижений пользователя
        /// </summary>
        [Route("CheckAchievements")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CheckAchievements(int userId)
        {
            try
            {
                await _achievementService.CheckAndGrantAchievements(userId);

                var achievements = _achievementService.GetUserAchievements(userId);

                return Json(new
                {
                    success = true,
                    message = "Достижения проверены успешно",
                    achievements = achievements,
                    newAchievements = achievements.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Удаление прогресса по ID
        /// </summary>
        [Route("DeleteById")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult DeleteById(int id)
        {
            try
            {
                UserProgressContext context = new UserProgressContext();
                var record = context.UserProgress.FirstOrDefault(x => x.id == id);
                if (record == null)
                    return NotFound($"Запись с ID {id} не найдена");

                context.UserProgress.Remove(record);
                context.SaveChanges();
                return Ok("Прогресс удален");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Очистка всех записей прогресса
        /// </summary>
        [Route("ClearAll")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v4")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public ActionResult ClearAll()
        {
            try
            {
                UserProgressContext context = new UserProgressContext();
                context.UserProgress.RemoveRange(context.UserProgress);
                context.SaveChanges();
                return Ok("Все записи прогресса удалены");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}