// CodeQuest/Model/Achievement.cs
namespace CodeQuest.Model
{
    public class Achievement
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string achievement_type { get; set; }
        public DateTime unlocked_at { get; set; }
    }
}