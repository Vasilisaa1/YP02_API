namespace CodeQuest.Model
{
    public class Topics
    {
        /// <summary>
        /// Код темы
        /// </summary>
        
        public int id {  get; set; }

        /// <summary>
        /// Название темы
        /// </summary>
       
        public string title { get; set; }

        /// <summary>
        /// Короткое описание
        /// </summary>
        
        public string short_description { get; set; }

        /// <summary>
        /// Полное описание
        /// </summary>
       
        public string full_description { get; set; }


        /// <summary>
        /// Индекс
        /// </summary>
         
        public int order_index { get; set; }


    }
}
