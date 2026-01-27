using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeQuest.Model
{/// <summary>
 /// Класс пользователей
 /// </summary>
    public class Users
    {
        [Key]
        /// <summary>
        /// Код пользователя
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id {  get; set; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        
        public string username { get; set; }

        /// <summary>
        /// Почта
        /// </summary>
       
        public string email { get; set; }

        /// <summary>
        /// Пароль
        /// </summary>
        
        public string passwordhash { get; set; }

        /// <summary>
        /// Дата регистрации
        /// </summary>
        /// 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime created_at { get; set; } = DateTime.Now;


        public byte[]? ProfileIcon { get; set; }

        // Добавляем поле для хранения MIME типа изображения
        public string? ProfileIconMimeType { get; set; }

        public bool? IsIconGenerated {  get; set; }
    }
}
