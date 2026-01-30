// CodeQuest/Controllers/AchievementController.cs
using CodeQuest.Context;
using CodeQuest.Model;
using CodeQuest.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [ApiController]
    [Route("api/Achievements")]
    public class AchievementController : Controller
    {
        private readonly AchievementService _achievementService;

        public AchievementController()
        {
            var context = new QuizContext();
            _achievementService = new AchievementService(context);
        }

        [Route("GetUserAchievements")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetUserAchievements(int userId)
        {
            try
            {
                var achievements = _achievementService.GetUserAchievements(userId);
                return Json(new
                {
                    success = true,
                    data = achievements,
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

        [Route("CheckAchievements")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult> CheckAchievements(int userId)
        {
            try
            {
                await _achievementService.CheckAndGrantAchievements(userId);

                var achievements = _achievementService.GetUserAchievements(userId);

                return Json(new
                {
                    success = true,
                    message = "Achievements checked successfully",
                    data = achievements
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

        [Route("GrantCustomAchievement")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        public async Task<ActionResult> GrantCustomAchievement([FromForm] int userId, [FromForm] string achievementType)
        {
            try
            {
                using var context = new QuizContext();

                // Проверяем, есть ли уже такое достижение
                var existing = context.Achievements
                    .FirstOrDefault(a => a.user_id == userId && a.achievement_type == achievementType);

                if (existing != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Achievement already granted"
                    });
                }

                var achievement = new Achievement
                {
                    user_id = userId,
                    achievement_type = achievementType,
                    unlocked_at = DateTime.Now
                };

                context.Achievements.Add(achievement);
                await context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Achievement '{achievementType}' granted to user {userId}",
                    achievement = achievement
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

        [Route("RecentAchievements")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult RecentAchievements(int limit = 10)
        {
            try
            {
                using var context = new QuizContext();

                var recentAchievements = context.Achievements
                    .OrderByDescending(a => a.unlocked_at)
                    .Take(limit)
                    .Join(context.Users,
                        a => a.user_id,
                        u => u.id,
                        (a, u) => new
                        {
                            achievement_id = a.id,
                            user_id = a.user_id,
                            username = u.username,
                            achievement_type = a.achievement_type,
                            unlocked_at = a.unlocked_at
                        })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = recentAchievements,
                    count = recentAchievements.Count
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
    }
}