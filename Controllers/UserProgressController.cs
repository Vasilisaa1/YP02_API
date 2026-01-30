using System;
using CodeQuest.Context;
using CodeQuest.Model;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [Route("api/UserProgress")]
    public class UserProgressController : Controller
    {
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

                // Если total_questions не указан, устанавливаем значение по умолчанию
                if (progress.total_questions <= 0)
                {
                    progress.total_questions = 5; // или любое другое значение по умолчанию
                }

                context.UserProgress.Add(progress);
                context.SaveChanges();
                
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = progress.user_id,
                    whatDo = "Полльзователь с Id " + progress.user_id + " прошёл тест " + progress.topic_id,
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                await contextLog.SaveChangesAsync();
                return Ok("Прогресс добавлен");
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

                    existing.user_id = progress.user_id;
                    existing.topic_id = progress.topic_id;
                    existing.is_completed = progress.is_completed;
                    existing.score = progress.score;
                    existing.total_questions = progress.total_questions; // Добавьте эту строку
                    existing.completed_at = progress.completed_at;

                    context.SaveChanges();

                    using var contextLog = new LogContext();
                    var log = new Model.Log
                    {
                        idUser = progress.user_id,
                        whatDo = "Полльзователь с Id " + progress.user_id + " повторно прошёл тест " + progress.topic_id,
                        created_At = DateTime.Now
                    };
                    contextLog.Log.Add(log);
                    await contextLog.SaveChangesAsync();
                    return Ok("Прогресс обновлен");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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
        public async Task<ActionResult> DeleteById(int id)
        {
            try
            {
                UserProgressContext context = new UserProgressContext();
                var record = context.UserProgress.FirstOrDefault(x => x.id == id);
                if (record == null)
                    return NotFound($"Запись с ID {id} не найдена");

                context.UserProgress.Remove(record);
                context.SaveChanges();
                using var contextLog = new LogContext();
                var log = new Model.Log
                {
                    idUser = record.user_id,
                    whatDo = "Полльзователь с Id " + record.user_id + " удалил прохождене теста " + record.topic_id,
                    created_At = DateTime.Now
                };
                contextLog.Log.Add(log);
                await contextLog.SaveChangesAsync();
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
