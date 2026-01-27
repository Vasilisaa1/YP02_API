namespace CodeQuest.Model
{
    public class Quiz
    {
        public int id { get; set; }
        public int topic_id { get; set; }
        public string question_text { get; set; }
        public string options { get; set; }  // JSON строка
        public string correct_answer { get; set; }
    }
}
