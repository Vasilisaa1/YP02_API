using CodeQuest.Context;
using CodeQuest.Model;

namespace CodeQuest.Services
{
    public class AchievementService
    {
        private readonly QuizContext _context;

        public AchievementService(QuizContext context)
        {
            _context = context;
        }

        public async Task CheckAndGrantAchievements(int userId)
        {
            try
            {
                var userProgress = _context.UserProgress
                    .Where(up => up.user_id == userId && up.is_completed == true)
                    .ToList();

                var completedQuizzesCount = userProgress.Count;
                var totalScore = userProgress.Sum(up => up.score);

                await CheckQuizCountAchievements(userId, completedQuizzesCount);
                await CheckScoreAchievements(userId, totalScore);
                await CheckPerfectScoreAchievements(userId, userProgress);
                await CheckStreakAchievements(userId);
                await CheckTopicMasterAchievements(userId, userProgress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking achievements: {ex.Message}");
            }
        }

        private async Task CheckQuizCountAchievements(int userId, int completedCount)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            // Сохраняем русские названия
            if (completedCount >= 3 && !existingAchievements.Contains("Первые 3 теста"))
            {
                await GrantAchievement(userId, "Первые 3 теста");
            }

            if (completedCount >= 10 && !existingAchievements.Contains("Мастер тестов"))
            {
                await GrantAchievement(userId, "Мастер тестов");
            }

            if (completedCount >= 25 && !existingAchievements.Contains("Эксперт тестов"))
            {
                await GrantAchievement(userId, "Эксперт тестов");
            }

            if (completedCount >= 50 && !existingAchievements.Contains("Легенда тестов"))
            {
                await GrantAchievement(userId, "Легенда тестов");
            }
        }

        private async Task CheckScoreAchievements(int userId, int totalScore)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            if (totalScore >= 100 && !existingAchievements.Contains("100 очков"))
            {
                await GrantAchievement(userId, "100 очков");
            }

            if (totalScore >= 500 && !existingAchievements.Contains("500 очков"))
            {
                await GrantAchievement(userId, "500 очков");
            }

            if (totalScore >= 1000 && !existingAchievements.Contains("1000 очков"))
            {
                await GrantAchievement(userId, "1000 очков");
            }
        }

        private async Task CheckPerfectScoreAchievements(int userId, List<UserProgress> progress)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            // Исправляем подсчет идеальных результатов
            var perfectScores = progress.Count(up => {
                // Предполагаем, что total_questions - это максимальное количество баллов
                // Или возможно у вас есть отдельное поле max_score
                return up.score == up.total_questions;
            });

            if (perfectScores >= 1 && !existingAchievements.Contains("Первый идеальный результат"))
            {
                await GrantAchievement(userId, "Первый идеальный результат");
            }

            if (perfectScores >= 5 && !existingAchievements.Contains("5 идеальных результатов"))
            {
                await GrantAchievement(userId, "5 идеальных результатов");
            }

            if (perfectScores >= 10 && !existingAchievements.Contains("Перфекционист"))
            {
                await GrantAchievement(userId, "Перфекционист");
            }
        }

        private async Task CheckStreakAchievements(int userId)
        {
            var todayProgress = _context.UserProgress
                .Where(up => up.user_id == userId &&
                       up.completed_at.HasValue &&
                       up.completed_at.Value.Date == DateTime.Today &&
                       up.is_completed == true)
                .ToList();

            if (todayProgress.Count >= 3)
            {
                // Используем русское название
                var existingAchievement = _context.Achievements
                    .FirstOrDefault(a => a.user_id == userId && a.achievement_type == "3 теста за один день");

                if (existingAchievement == null)
                {
                    await GrantAchievement(userId, "3 теста за один день");
                }
            }

            // Проверка последовательных дней
            var last7DaysProgress = _context.UserProgress
                .Where(up => up.user_id == userId &&
                       up.completed_at.HasValue &&
                       up.completed_at.Value.Date >= DateTime.Today.AddDays(-7) &&
                       up.is_completed == true)
                .Select(up => up.completed_at.Value.Date)
                .Distinct()
                .Count();

            if (last7DaysProgress >= 7)
            {
                // Используем русское название
                var existingAchievement = _context.Achievements
                    .FirstOrDefault(a => a.user_id == userId && a.achievement_type == "Тесты всю неделю");

                if (existingAchievement == null)
                {
                    await GrantAchievement(userId, "Тесты всю неделю");
                }
            }
        }

        private async Task CheckTopicMasterAchievements(int userId, List<UserProgress> progress)
        {
            var distinctTopics = progress
                .Select(up => up.topic_id)
                .Distinct()
                .Count();

            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            if (distinctTopics >= 3 && !existingAchievements.Contains("Исследователь"))
            {
                await GrantAchievement(userId, "Исследователь");
            }

            if (distinctTopics >= 5 && !existingAchievements.Contains("Универсальный ученик"))
            {
                await GrantAchievement(userId, "Универсальный ученик");
            }

            if (distinctTopics >= 10 && !existingAchievements.Contains("Искатель знаний"))
            {
                await GrantAchievement(userId, "Искатель знаний");
            }
        }

        private async Task GrantAchievement(int userId, string achievementType)
        {
            var achievement = new Achievement
            {
                user_id = userId,
                achievement_type = achievementType, // Сохраняем русское название
                unlocked_at = DateTime.Now
            };

            _context.Achievements.Add(achievement);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Достижение получено: Пользователь {userId} - {achievementType}");
        }

        public List<Achievement> GetUserAchievements(int userId)
        {
            return _context.Achievements
                .Where(a => a.user_id == userId)
                .OrderByDescending(a => a.unlocked_at)
                .ToList();
        }

        public AchievementStats GetAchievementStats(int userId)
        {
            var achievements = GetUserAchievements(userId);
            var progress = _context.UserProgress
                .Where(up => up.user_id == userId && up.is_completed == true)
                .ToList();

            return new AchievementStats
            {
                TotalAchievements = achievements.Count,
                TotalCompletedQuizzes = progress.Count,
                TotalScore = progress.Sum(up => up.score),
                PerfectScores = progress.Count(up => up.score == up.total_questions),
                DistinctTopics = progress.Select(up => up.topic_id).Distinct().Count()
            };
        }
    }

    public class AchievementStats
    {
        public int TotalAchievements { get; set; }
        public int TotalCompletedQuizzes { get; set; }
        public int TotalScore { get; set; }
        public int PerfectScores { get; set; }
        public int DistinctTopics { get; set; }
    }
}