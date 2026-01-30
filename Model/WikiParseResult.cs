namespace CodeQuest.Model
{
    public class WikiParseResult
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = "https://dotnet.microsoft.com/ru-ru/languages/csharp";
        public string ContentSummary { get; set; } = string.Empty; // Основное описание
        public string KeyFeatures { get; set; } = string.Empty;    // Особенности C#
        public string VersionHistory { get; set; } = string.Empty; // Информация о версиях
        public DateTime ParsedAt { get; set; } = DateTime.Now;
        public string RawHtml { get; set; } = string.Empty; // Полный HTML
        public string SiteType { get; set; } = "dotnet.microsoft.com";
    }
}
