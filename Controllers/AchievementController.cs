using CodeQuest.Context;
using CodeQuest.Model;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [ApiController]
    [Route("api/Achievements")]
    public class AchievementController : Controller
    {
        [Route("List")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult List()
        {
            try
            {
                var achievements = new AchievementContext().Achievements
                    .OrderBy(a => a.order_index)
                    .ToList();
                return Json(achievements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("UserAchievements")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult UserAchievements(int userId)
        {
            try
            {
                using var context = new AchievementContext();

                // Получаем все достижения
                var allAchievements = context.Achievements
                    .OrderBy(a => a.order_index)
                    .ToList();

                // Получаем достижения пользователя
                var userAchievements = context.UserAchievements
                    .Where(ua => ua.user_id == userId)
                    .ToList();

                // Формируем DTO
                var result = new List<AchievementDto>();

                foreach (var achievement in allAchievements)
                {
                    var userAchievement = userAchievements
                        .FirstOrDefault(ua => ua.achievement_id == achievement.id);

                    var targetValue = int.TryParse(achievement.criteria_value, out var target) ? target : 0;
                    var currentProgress = userAchievement?.progress_current ?? 0;

                    result.Add(new AchievementDto
                    {
                        id = achievement.id,
                        name = achievement.name,
                        description = achievement.description,
                        criteria_type = achievement.criteria_type,
                        criteria_value = achievement.criteria_value,
                        points_reward = achievement.points_reward,
                        order_index = achievement.order_index,
                        is_unlocked = userAchievement?.unlocked_at != null,
                        unlocked_at = userAchievement?.unlocked_at,
                        progress_current = currentProgress,
                        progress_target = targetValue,
                        progress_percentage = targetValue > 0
                            ? Math.Min(100, Math.Round((double)currentProgress / targetValue * 100, 2))
                            : 0
                    });
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("UpdateProgress")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        public ActionResult UpdateProgress([FromBody] AchievementProgressRequest request)
        {
            try
            {
                Console.WriteLine("=== API: UPDATE PROGRESS START ===");
                Console.WriteLine($"User ID: {request.user_id}");
                Console.WriteLine($"Criteria Type: {request.criteria_type}");
                Console.WriteLine($"Increment Value: {request.increment_value}");

                using var context = new AchievementContext();

                // Получаем все достижения по типу критерия
                var achievements = context.Achievements
                    .Where(a => a.criteria_type == request.criteria_type)
                    .ToList();

                var unlockedAchievements = new List<AchievementDto>();

                foreach (var achievement in achievements)
                {
                    // Получаем или создаем запись пользовательского достижения
                    var userAchievement = context.UserAchievements
                        .FirstOrDefault(ua => ua.user_id == request.user_id &&
                                             ua.achievement_id == achievement.id);

                    if (userAchievement == null)
                    {
                        userAchievement = new UserAchievement
                        {
                            user_id = request.user_id,
                            achievement_id = achievement.id,
                            progress_current = 0,
                            progress_target = ParseCriteriaValue(achievement.criteria_value),
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        context.UserAchievements.Add(userAchievement);
                    }

                    // Если достижение уже разблокировано, пропускаем
                    if (userAchievement.unlocked_at != null)
                    {
                        continue;
                    }

                    // Обновляем прогресс
                    userAchievement.progress_current += request.increment_value;
                    userAchievement.updated_at = DateTime.Now;

                    // Проверяем, выполнены ли критерии для разблокировки
                    var targetValue = ParseCriteriaValue(achievement.criteria_value);

                    if (userAchievement.progress_current >= targetValue)
                    {
                        // Разблокируем достижение
                        userAchievement.unlocked_at = DateTime.Now;

                        // УДАЛИЛИ начисление очков пользователю
                        // var user = context.Users.First(u => u.id == request.user_id);
                        // user.points += achievement.points_reward; <- ЭТО УБРАНО

                        unlockedAchievements.Add(new AchievementDto
                        {
                            id = achievement.id,
                            name = achievement.name,
                            description = achievement.description,
                            criteria_type = achievement.criteria_type,
                            criteria_value = achievement.criteria_value,
                            points_reward = achievement.points_reward, // Можно убрать из модели
                            order_index = achievement.order_index,
                            is_unlocked = true,
                            unlocked_at = userAchievement.unlocked_at,
                            progress_current = userAchievement.progress_current,
                            progress_target = targetValue,
                            progress_percentage = 100
                        });

                        Console.WriteLine($"Achievement '{achievement.name}' unlocked for user {request.user_id}");
                    }
                }

                context.SaveChanges();

                Console.WriteLine($"=== API: PROGRESS UPDATED SUCCESSFULLY ===");

                if (unlockedAchievements.Any())
                {
                    return Json(new
                    {
                        success = true,
                        message = "Progress updated and achievements unlocked",
                        unlocked_achievements = unlockedAchievements
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Progress updated"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== API: ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Вспомогательный метод для парсинга значения критерия
        private int ParseCriteriaValue(string criteriaValue)
        {
            return int.TryParse(criteriaValue, out var result) ? result : 0;
        }

        [Route("Add")]
        [HttpPost]
        [ApiExplorerSettings(GroupName = "v2")]
        public ActionResult Add([FromForm] Achievement achievement)
        {
            try
            {
                using var context = new AchievementContext();

                // Проверка существования достижения с таким же именем
                var existing = context.Achievements
                    .FirstOrDefault(a => a.name == achievement.name);

                if (existing != null)
                {
                    return BadRequest($"Achievement with name '{achievement.name}' already exists");
                }

                context.Achievements.Add(achievement);
                context.SaveChanges();

                return Ok($"Achievement added successfully with ID: {achievement.id}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Route("ResetUserProgress")]
        [HttpDelete]
        [ApiExplorerSettings(GroupName = "v3")]
        public ActionResult ResetUserProgress(int userId)
        {
            try
            {
                using var context = new AchievementContext();
                var userAchievements = context.UserAchievements
                    .Where(ua => ua.user_id == userId)
                    .ToList();

                context.UserAchievements.RemoveRange(userAchievements);
                context.SaveChanges();

                return Ok($"All achievements reset for user {userId}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("UnlockedCount")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult UnlockedCount(int userId)
        {
            try
            {
                using var context = new AchievementContext();
                var unlockedCount = context.UserAchievements
                    .Count(ua => ua.user_id == userId && ua.unlocked_at != null);

                var totalCount = context.Achievements.Count();

                return Json(new
                {
                    user_id = userId,
                    unlocked_count = unlockedCount,
                    total_count = totalCount,
                    percentage = totalCount > 0 ? Math.Round((double)unlockedCount / totalCount * 100, 2) : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("RecentUnlocked")]
        [HttpGet]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult RecentUnlocked(int userId, int limit = 5)
        {
            try
            {
                using var context = new AchievementContext();

                var recentAchievements = (from ua in context.UserAchievements
                                          join a in context.Achievements on ua.achievement_id equals a.id
                                          where ua.user_id == userId && ua.unlocked_at != null
                                          orderby ua.unlocked_at descending
                                          select new
                                          {
                                              id = a.id,
                                              name = a.name,
                                              description = a.description,
                                              unlocked_at = ua.unlocked_at,
                                              days_ago = (DateTime.Now - ua.unlocked_at.Value).Days
                                          })
                                         .Take(limit)
                                         .ToList();

                return Json(recentAchievements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}