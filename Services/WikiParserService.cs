// CodeQuest/Services/WikiParserService.cs
using CodeQuest.Model;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace CodeQuest.Services
{
    public class WikiParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WikiParserService> _logger;

        public WikiParserService(ILogger<WikiParserService> logger)
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true
            });

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
            _logger = logger;
        }

        public async Task<WikiParseResult> ParseCSharpDotNetPageAsync()
        {
            var url = "https://dotnet.microsoft.com/ru-ru/languages/csharp";
            var result = new WikiParseResult
            {
                Url = url,
                SiteType = "dotnet.microsoft.com",
                ParsedAt = DateTime.Now
            };

            try
            {
                _logger.LogInformation($"Начинаем парсинг страницы: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                result.RawHtml = html;

                // 1. Парсим заголовок
                ParseTitle(htmlDoc, result);

                // 2. Парсим описание
                ParseDescription(htmlDoc, result);

                // 3. Парсим раздел "Почему C#?" или "Особенности"
                ParseWhyCSharpSection(htmlDoc, result);

                // 4. Парсим информацию о версиях
                ParseVersionInfo(htmlDoc, result);

                _logger.LogInformation($"Успешно распарсена страница: {result.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при парсинге страницы {url}");
                result.ContentSummary = $"Ошибка: {ex.Message}";
            }

            return result;
        }

        private void ParseTitle(HtmlDocument htmlDoc, WikiParseResult result)
        {
            // Ищем заголовок C# на странице Microsoft
            // Пробуем разные селекторы, которые используются на сайте Microsoft
            var titleSelectors = new[]
            {
                "//h1[contains(@class, 'display')]",
                "//h1[contains(@class, 'hero')]",
                "//main//h1",
                "//h1",
                "//title"
            };

            foreach (var selector in titleSelectors)
            {
                var titleNode = htmlDoc.DocumentNode.SelectSingleNode(selector);
                if (titleNode != null)
                {
                    var titleText = CleanText(titleNode.InnerText);
                    if (!string.IsNullOrEmpty(titleText))
                    {
                        result.Title = titleText;
                        return;
                    }
                }
            }

            result.Title = "C# | .NET";
        }

        private void ParseDescription(HtmlDocument htmlDoc, WikiParseResult result)
        {
            var descriptionParts = new List<string>();

            // Ищем основной текст описания C# на странице Microsoft
            // Используем селекторы, характерные для структуры сайта Microsoft
            var descriptionSelectors = new[]
            {
                "//div[contains(@class, 'hero')]//p",
                "//section[contains(@class, 'intro')]//p",
                "//div[contains(@class, 'lead')]",
                "//p[contains(@class, 'lead')]",
                "//div[contains(@class, 'description')]//p",
                "//article//p[not(contains(@class, 'small'))]"
            };

            foreach (var selector in descriptionSelectors)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes(selector);
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var node in nodes.Take(5)) // Берем первые 5 абзацев
                    {
                        var text = CleanText(node.InnerText);
                        if (!string.IsNullOrEmpty(text) && text.Length > 30)
                        {
                            descriptionParts.Add(text);
                        }
                    }
                    if (descriptionParts.Count > 0) break;
                }
            }

            // Если не нашли через селекторы, ищем по структуре контента
            if (descriptionParts.Count == 0)
            {
                // Ищем первый значимый текст после заголовка
                var mainContent = htmlDoc.DocumentNode.SelectSingleNode("//main") ??
                                 htmlDoc.DocumentNode.SelectSingleNode("//article") ??
                                 htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'content')]");

                if (mainContent != null)
                {
                    var paragraphs = mainContent.SelectNodes(".//p");
                    if (paragraphs != null)
                    {
                        foreach (var p in paragraphs.Take(3))
                        {
                            var text = CleanText(p.InnerText);
                            if (!string.IsNullOrEmpty(text) && text.Length > 50)
                            {
                                descriptionParts.Add(text);
                            }
                        }
                    }
                }
            }

            result.ContentSummary = string.Join("\n\n", descriptionParts);
        }

        private void ParseWhyCSharpSection(HtmlDocument htmlDoc, WikiParseResult result)
        {
            var features = new List<string>();

            // Ищем раздел "Why C#?" или "Преимущества C#" на сайте Microsoft
            var sectionSelectors = new[]
            {
                "//section[.//h2[contains(text(), 'Why') or contains(text(), 'Почему') or contains(text(), 'Преимущества')]]",
                "//div[.//h2[contains(text(), 'Why') or contains(text(), 'Почему') or contains(text(), 'Преимущества')]]",
                "//div[contains(@class, 'features')]",
                "//div[contains(@class, 'benefits')]",
                "//section[contains(@class, 'why')]"
            };

            foreach (var sectionSelector in sectionSelectors)
            {
                var section = htmlDoc.DocumentNode.SelectSingleNode(sectionSelector);
                if (section != null)
                {
                    _logger.LogInformation($"Найден раздел с преимуществами: {sectionSelector}");

                    // Ищем пункты списка внутри раздела
                    var listItems = section.SelectNodes(".//li");
                    if (listItems != null && listItems.Count > 0)
                    {
                        foreach (var item in listItems.Take(10))
                        {
                            var text = CleanText(item.InnerText);
                            if (!string.IsNullOrEmpty(text))
                            {
                                features.Add($"• {text}");
                            }
                        }
                    }

                    // Или ищем карточки/блоки с преимуществами
                    var cards = section.SelectNodes(".//div[contains(@class, 'card')] | .//div[contains(@class, 'feature')]");
                    if (cards != null && cards.Count > 0)
                    {
                        foreach (var card in cards.Take(10))
                        {
                            var title = card.SelectSingleNode(".//h3") ?? card.SelectSingleNode(".//h4");
                            if (title != null)
                            {
                                var text = CleanText(title.InnerText);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    features.Add($"• {text}");
                                }
                            }
                        }
                    }

                    if (features.Count > 0) break;
                }
            }

            // Если не нашли структурированный раздел, ищем любые упоминания особенностей в тексте
            if (features.Count == 0)
            {
                _logger.LogInformation("Структурированный раздел не найден, ищем особенности в тексте");

                // Собираем весь текст страницы
                var allText = htmlDoc.DocumentNode.InnerText;
                var sentences = allText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

                // Ищем предложения, которые описывают особенности C#
                foreach (var sentence in sentences)
                {
                    var cleanSentence = CleanText(sentence);
                    if (cleanSentence.Length > 30 && cleanSentence.Length < 300)
                    {
                        // Проверяем, относится ли предложение к описанию C#
                        if (cleanSentence.Contains("C#", StringComparison.OrdinalIgnoreCase) ||
                            cleanSentence.Contains("язык", StringComparison.OrdinalIgnoreCase) ||
                            cleanSentence.Contains("программир", StringComparison.OrdinalIgnoreCase))
                        {
                            features.Add($"• {cleanSentence}");
                        }
                    }
                    if (features.Count >= 10) break;
                }
            }

            result.KeyFeatures = string.Join("\n", features.Take(15));
        }

        private void ParseVersionInfo(HtmlDocument htmlDoc, WikiParseResult result)
        {
            var versionsInfo = new List<string>();

            // Ищем информацию о версиях .NET/C# на сайте Microsoft
            // Часто эта информация находится внизу страницы или в специальных блоках
            var versionSelectors = new[]
            {
                "//div[contains(text(), '.NET')]",
                "//div[contains(text(), 'версия')]",
                "//span[contains(@class, 'version')]",
                "//div[contains(@class, 'version')]",
                "//footer//a[contains(@href, 'download')]",
                "//a[contains(text(), 'Download')]"
            };

            foreach (var selector in versionSelectors)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    foreach (var node in nodes.Take(5))
                    {
                        var text = CleanText(node.InnerText);
                        if (!string.IsNullOrEmpty(text))
                        {
                            // Фильтруем только релевантную информацию о версиях
                            if (text.Contains("NET", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("C#", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("версия", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("version", StringComparison.OrdinalIgnoreCase))
                            {
                                versionsInfo.Add(text);
                            }
                        }
                    }
                }
            }

            // Также ищем по паттернам версий
            var allText = htmlDoc.DocumentNode.InnerText;
            var versionPatterns = new[]
            {
                @"\.NET\s+\d+(\.\d+)+",
                @"C#\s+\d+(\.\d+)+",
                @"версия\s+\d+(\.\d+)+",
                @"version\s+\d+(\.\d+)+",
                @"\b\d+\.\d+(\.\d+)*\b"
            };

            foreach (var pattern in versionPatterns)
            {
                var matches = Regex.Matches(allText, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (!versionsInfo.Contains(match.Value))
                    {
                        versionsInfo.Add(match.Value);
                    }
                }
            }

            result.VersionHistory = string.Join("\n", versionsInfo.Distinct().Take(10));
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Удаляем лишние пробелы и переносы строк
            text = Regex.Replace(text, @"\s+", " ");
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            text = WebUtility.HtmlDecode(text);

            // Удаляем HTML теги
            text = Regex.Replace(text, "<.*?>", string.Empty);

            // Удаляем невидимые символы
            text = Regex.Replace(text, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]", "");

            return text.Trim();
        }
    }
}