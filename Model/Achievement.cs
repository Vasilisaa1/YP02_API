namespace CodeQuest.Model
{
    public class Achievement
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string criteria_type { get; set; }
        public string criteria_value { get; set; }
        public int points_reward { get; set; }
        public int order_index { get; set; }
        public DateTime created_at { get; set; }
    }

    public class UserAchievement
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int achievement_id { get; set; }
        public DateTime? unlocked_at { get; set; }
        public int progress_current { get; set; }
        public int progress_target { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class AchievementProgressRequest
    {
        public int user_id { get; set; }
        public string criteria_type { get; set; }
        public int increment_value { get; set; } = 1;
        public Dictionary<string, object>? additional_data { get; set; }
    }

    public class AchievementDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string criteria_type { get; set; }
        public string criteria_value { get; set; }
        public int points_reward { get; set; }
        public int order_index { get; set; }
        public bool is_unlocked { get; set; }
        public DateTime? unlocked_at { get; set; }
        public int progress_current { get; set; }
        public int progress_target { get; set; }
        public double progress_percentage { get; set; }
    }
}