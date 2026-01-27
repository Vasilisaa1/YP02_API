namespace CodeQuest.Model
{
    public class UserProgress
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int topic_id { get; set; }
        public bool is_completed { get; set; }
        public int score { get; set; }
        public int total_questions { get; set; } // Добавьте это поле
        public DateTime? completed_at { get; set; }
    }
}