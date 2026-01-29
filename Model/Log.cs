namespace CodeQuest.Model
{
    public class Log
    {
        public int id { get; set; }
        public int idUser { get; set; }
        public string whatDo { get; set; }
        public DateTime created_At { get; set; } = DateTime.Now;
    }
}
