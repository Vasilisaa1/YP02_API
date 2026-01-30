// Controllers/LogsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeQuest.Context;
using CodeQuest.Model;

namespace CodeQuest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        [HttpGet("GetAll")]
        public ActionResult<IEnumerable<Log>> GetAllLogs()
        {
            try
            {
                using var context = new LogContext();
                var logs = context.Log
                    .OrderByDescending(l => l.created_At)
                    .ToList();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении логов: {ex.Message}");
            }
        }

        [HttpGet("GetByUserId/{userId}")]
        public ActionResult<IEnumerable<Log>> GetLogsByUserId(int userId)
        {
            try
            {
                using var context = new LogContext();
                var logs = context.Log
                    .Where(l => l.idUser == userId)
                    .OrderByDescending(l => l.created_At)
                    .ToList();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении логов: {ex.Message}");
            }
        }

        [HttpGet("GetByDateRange")]
        public ActionResult<IEnumerable<Log>> GetLogsByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var context = new LogContext();
                var logs = context.Log
                    .Where(l => l.created_At >= startDate && l.created_At <= endDate)
                    .OrderByDescending(l => l.created_At)
                    .ToList();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении логов: {ex.Message}");
            }
        }

        [HttpDelete("ClearOld")]
        public ActionResult ClearOldLogs(int daysToKeep = 30)
        {
            try
            {
                using var context = new LogContext();
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                var oldLogs = context.Log
                    .Where(l => l.created_At < cutoffDate)
                    .ToList();

                context.Log.RemoveRange(oldLogs);
                context.SaveChanges();

                return Ok(new
                {
                    message = $"Удалено {oldLogs.Count} логов старше {daysToKeep} дней",
                    deletedCount = oldLogs.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при очистке логов: {ex.Message}");
            }
        }

        [HttpGet("Statistics")]
        public ActionResult GetLogStatistics()
        {
            try
            {
                using var context = new LogContext();

                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                var weekAgo = today.AddDays(-7);

                var statistics = new
                {
                    TotalLogs = context.Log.Count(),
                    TodayLogs = context.Log.Count(l => l.created_At >= today),
                    YesterdayLogs = context.Log.Count(l => l.created_At >= yesterday && l.created_At < today),
                    Last7DaysLogs = context.Log.Count(l => l.created_At >= weekAgo),
                    TopUsers = context.Log
                        .GroupBy(l => l.idUser)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToList()
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении статистики: {ex.Message}");
            }
        }
    }
}