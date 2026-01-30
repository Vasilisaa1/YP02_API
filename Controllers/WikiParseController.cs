// CodeQuest/Controllers/WikiParseController.cs
using CodeQuest.Context;
using CodeQuest.Model;
using CodeQuest.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeQuest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WikiParseController : ControllerBase
    {
        private readonly WikiParserService _parserService;
        private readonly ILogger<WikiParseController> _logger;

        public WikiParseController(
            WikiParserService parserService,
            ILogger<WikiParseController> logger)
        {
            _parserService = parserService;
            _logger = logger;
        }

        [HttpPost("ParseDotNetCSharp")]
        [ApiExplorerSettings(GroupName = "v2")]
        public async Task<ActionResult> ParseDotNetCSharp()
        {
            try
            {
                _logger.LogInformation("Начинаем парсинг страницы C# от Microsoft");

                var parseResult = await _parserService.ParseCSharpDotNetPageAsync();

                using var context = new WikiParseContext();
                context.WikiParseResults.Add(parseResult);
                await context.SaveChangesAsync();

                // Логируем в системный журнал
                using var logContext = new LogContext();
                var log = new Log
                {
                    idUser = 0,
                    whatDo = $"Распарсена страница Microsoft C#: {parseResult.Title}",
                    created_At = DateTime.Now
                };
                logContext.Log.Add(log);
                await logContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Страница Microsoft C# успешно распарсена",
                    result = new
                    {
                        id = parseResult.Id,
                        title = parseResult.Title,
                        url = parseResult.Url,
                        siteType = parseResult.SiteType,
                        summaryLength = parseResult.ContentSummary.Length,
                        featuresCount = parseResult.KeyFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                        versionsFound = parseResult.VersionHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                        parsedAt = parseResult.ParsedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге страницы Microsoft C#");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Ошибка: {ex.Message}"
                });
            }
        }

        [HttpGet("GetLatestDotNetParse")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetLatestDotNetParse()
        {
            try
            {
                using var context = new WikiParseContext();
                var latestResult = context.WikiParseResults
                    .Where(r => r.SiteType == "dotnet.microsoft.com")
                    .OrderByDescending(r => r.ParsedAt)
                    .FirstOrDefault();

                if (latestResult == null)
                    return NotFound(new
                    {
                        message = "Нет данных парсинга Microsoft C#",
                        suggestion = "Используйте POST /api/WikiParse/ParseDotNetCSharp"
                    });

                return Ok(new
                {
                    latestResult.Id,
                    latestResult.Title,
                    latestResult.Url,
                    latestResult.SiteType,
                    ContentSummary = latestResult.ContentSummary.Length > 500
                        ? latestResult.ContentSummary.Substring(0, 500) + "..."
                        : latestResult.ContentSummary,
                    KeyFeatures = latestResult.KeyFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                    VersionHistory = latestResult.VersionHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                    ParsedAt = latestResult.ParsedAt,
                    Age = $"{(int)(DateTime.Now - latestResult.ParsedAt).TotalHours} часов назад"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении данных Microsoft C#");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetAllDotNetParses")]
        [ApiExplorerSettings(GroupName = "v1")]
        public ActionResult GetAllDotNetParses([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                using var context = new WikiParseContext();
                var query = context.WikiParseResults
                    .Where(r => r.SiteType == "dotnet.microsoft.com")
                    .OrderByDescending(r => r.ParsedAt);

                var totalCount = query.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var results = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.Url,
                        ShortSummary = r.ContentSummary.Length > 100
                            ? r.ContentSummary.Substring(0, 100) + "..."
                            : r.ContentSummary,
                        FeaturesCount = r.KeyFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                        r.ParsedAt
                    })
                    .ToList();

                return Ok(new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1,
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка парсингов Microsoft C#");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("TestDotNetParse")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult> TestDotNetParse()
        {
            try
            {
                var result = await _parserService.ParseCSharpDotNetPageAsync();

                return Ok(new
                {
                    Status = "Тест парсинга Microsoft C# успешен",
                    Url = result.Url,
                    Title = result.Title,
                    SiteType = result.SiteType,
                    SummaryPreview = result.ContentSummary.Length > 200
                        ? result.ContentSummary.Substring(0, 200) + "..."
                        : result.ContentSummary,
                    FeaturesPreview = result.KeyFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3),
                    VersionsFound = result.VersionHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                    HtmlLength = result.RawHtml.Length,
                    Timestamp = result.ParsedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Тест парсинга не удался",
                    Error = ex.Message
                });
            }
        }

        [HttpDelete("ClearDotNetParses")]
        [ApiExplorerSettings(GroupName = "v4")]
        public ActionResult ClearDotNetParses()
        {
            try
            {
                using var context = new WikiParseContext();
                var dotNetResults = context.WikiParseResults
                    .Where(r => r.SiteType == "dotnet.microsoft.com")
                    .ToList();

                var count = dotNetResults.Count;
                context.WikiParseResults.RemoveRange(dotNetResults);
                context.SaveChanges();

                _logger.LogInformation($"Удалено {count} записей парсинга Microsoft C#");

                return Ok(new
                {
                    success = true,
                    message = $"Удалено {count} записей Microsoft C#",
                    deletedCount = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке записей Microsoft C#");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Ошибка: {ex.Message}"
                });
            }
        }
    }
}