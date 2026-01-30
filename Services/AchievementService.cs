// CodeQuest/Services/AchievementService.cs
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

        // Метод для проверки и выдачи достижений при обновлении прогресса
        public async Task CheckAndGrantAchievements(int userId)
        {
            try
            {
                // Получаем прогресс пользователя
                var userProgress = _context.UserProgress
                    .Where(up => up.user_id == userId && up.is_completed == true)
                    .ToList();

                var completedQuizzesCount = userProgress.Count;
                var totalScore = userProgress.Sum(up => up.score);

                // Проверяем достижения
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

        // Достижения за количество пройденных тестов
        private async Task CheckQuizCountAchievements(int userId, int completedCount)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            if (completedCount >= 3 && !existingAchievements.Contains("FIRST_3_QUIZZES"))
            {
                await GrantAchievement(userId, "FIRST_3_QUIZZES", "Пройдено 3 теста");
            }

            if (completedCount >= 10 && !existingAchievements.Contains("QUIZ_MASTER"))
            {
                await GrantAchievement(userId, "QUIZ_MASTER", "Пройдено 10 тестов");
            }

            if (completedCount >= 25 && !existingAchievements.Contains("QUIZ_EXPERT"))
            {
                await GrantAchievement(userId, "QUIZ_EXPERT", "Пройдено 25 тестов");
            }

            if (completedCount >= 50 && !existingAchievements.Contains("QUIZ_LEGEND"))
            {
                await GrantAchievement(userId, "QUIZ_LEGEND", "Пройдено 50 тестов");
            }
        }

        // Достижения за общий счет
        private async Task CheckScoreAchievements(int userId, int totalScore)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            if (totalScore >= 100 && !existingAchievements.Contains("SCORE_100"))
            {
                await GrantAchievement(userId, "SCORE_100", "Набрано 100 очков");
            }

            if (totalScore >= 500 && !existingAchievements.Contains("SCORE_500"))
            {
                await GrantAchievement(userId, "SCORE_500", "Набрано 500 очков");
            }

            if (totalScore >= 1000 && !existingAchievements.Contains("SCORE_1000"))
            {
                await GrantAchievement(userId, "SCORE_1000", "Набрано 1000 очков");
            }
        }

        // Достижения за идеальные результаты
        private async Task CheckPerfectScoreAchievements(int userId, List<UserProgress> progress)
        {
            var existingAchievements = _context.Achievements
                .Where(a => a.user_id == userId)
                .Select(a => a.achievement_type)
                .ToList();

            var perfectScores = progress.Count(up => up.score == up.total_questions);

            if (perfectScores >= 1 && !existingAchievements.Contains("FIRST_PERFECT"))
            {
                await GrantAchievement(userId, "FIRST_PERFECT", "Первый идеальный результат");
            }

            if (perfectScores >= 5 && !existingAchievements.Contains("PERFECT_STREAK_5"))
            {
                await GrantAchievement(userId, "PERFECT_STREAK_5", "5 идеальных результатов");
            }

            if (perfectScores >= 10 && !existingAchievements.Contains("PERFECTIONIST"))
            {
                await GrantAchievement(userId, "PERFECTIONIST", "10 идеальных результатов");
            }
        }

        // Достижения за серии
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
                var existingAchievement = _context.Achievements
                    .FirstOrDefault(a => a.user_id == userId && a.achievement_type == "DAILY_STREAK_3");

                if (existingAchievement == null)
                {
                    await GrantAchievement(userId, "DAILY_STREAK_3", "3 теста за один день");
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
                var existingAchievement = _context.Achievements
                    .FirstOrDefault(a => a.user_id == userId && a.achievement_type == "WEEKLY_STREAK");

                if (existingAchievement == null)
                {
                    await GrantAchievement(userId, "WEEKLY_STREAK", "Тесты 7 дней подряд");
                }
            }
        }

        // Достижения за изучение разных тем
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

            if (distinctTopics >= 3 && !existingAchievements.Contains("EXPLORER"))
            {
                await GrantAchievement(userId, "EXPLORER", "Изучено 3 различные темы");
            }

            if (distinctTopics >= 5 && !existingAchievements.Contains("VERSATILE_LEARNER"))
            {
                await GrantAchievement(userId, "VERSATILE_LEARNER", "Изучено 5 различных тем");
            }

            if (distinctTopics >= 10 && !existingAchievements.Contains("KNOWLEDGE_SEEKER"))
            {
                await GrantAchievement(userId, "KNOWLEDGE_SEEKER", "Изучено 10 различных тем");
            }
        }

        // Метод выдачи достижения
        private async Task GrantAchievement(int userId, string achievementType, string description = "")
        {
            var achievement = new Achievement
            {
                user_id = userId,
                achievement_type = achievementType,
                unlocked_at = DateTime.Now
            };

            _context.Achievements.Add(achievement);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Achievement granted: User {userId} - {achievementType} ({description})");
        }

        // Получение всех достижений пользователя
        public List<Achievement> GetUserAchievements(int userId)
        {
            return _context.Achievements
                .Where(a => a.user_id == userId)
                .OrderByDescending(a => a.unlocked_at)
                .ToList();
        }

        // Получение статистики достижений
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

    // Класс для статистики
    public class AchievementStats
    {
        public int TotalAchievements { get; set; }
        public int TotalCompletedQuizzes { get; set; }
        public int TotalScore { get; set; }
        public int PerfectScores { get; set; }
        public int DistinctTopics { get; set; }
    }
}