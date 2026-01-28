using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeQuest.Model
{
    /// <summary>
    /// Класс пользователей
    /// </summary>
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public string username { get; set; }
        public string email { get; set; }
        public string passwordhash { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime created_at { get; set; } = DateTime.Now;

        // Храним только название файла (например: "avatar_123.png")
        public string? ProfileIconFileName { get; set; }

        public bool? IsIconGenerated { get; set; }
    }
}