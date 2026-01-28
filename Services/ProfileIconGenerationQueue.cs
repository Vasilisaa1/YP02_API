using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;

namespace CodeQuest.Services
{
    public class ProfileIconGenerationQueue
    {
        private readonly Queue<(int userId, string username)> _pendingGenerations = new();
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ProfileIconGenerationQueue> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly object _lock = new();

        public ProfileIconGenerationQueue(
            IMemoryCache memoryCache,
            ILogger<ProfileIconGenerationQueue> logger,
            IWebHostEnvironment env)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _env = env;
        }

        public void EnqueueGeneration(int userId, string username)
        {
            lock (_lock)
            {
                _pendingGenerations.Enqueue((userId, username));
            }

            var cacheKey = GetCacheKey(userId);
            _memoryCache.Set(cacheKey, new IconGenerationStatus
            {
                IsGenerating = true,
                Username = username,
                EnqueuedAt = DateTime.UtcNow
            }, TimeSpan.FromHours(1));

            _logger.LogInformation($"Пользователь {username} (ID: {userId}) добавлен в очередь генерации");
        }

        public bool TryDequeue(out (int userId, string username) item)
        {
            lock (_lock)
            {
                if (_pendingGenerations.Count > 0)
                {
                    item = _pendingGenerations.Dequeue();
                    return true;
                }
                item = default;
                return false;
            }
        }

        public bool IsGenerating(int userId)
        {
            var cacheKey = GetCacheKey(userId);
            return _memoryCache.TryGetValue(cacheKey, out IconGenerationStatus status) &&
                   status?.IsGenerating == true;
        }

        public void MarkAsGenerated(int userId, string? fileName)
        {
            var cacheKey = GetCacheKey(userId);
            var status = _memoryCache.Get<IconGenerationStatus>(cacheKey);

            if (status != null)
            {
                status.IsGenerating = false;
                status.IsGenerated = !string.IsNullOrEmpty(fileName);
                status.GeneratedAt = DateTime.UtcNow;
                status.FileName = fileName;

                _memoryCache.Set(cacheKey, status, TimeSpan.FromHours(24));
                _logger.LogInformation($"Иконка для пользователя ID: {userId} сгенерирована, файл: {fileName}");
            }
        }

        public string? GetGeneratedIconFileName(int userId)
        {
            var cacheKey = GetCacheKey(userId);
            if (_memoryCache.TryGetValue(cacheKey, out IconGenerationStatus status))
            {
                return status?.FileName;
            }
            return null;
        }

        private static string GetCacheKey(int userId) => $"icon_generation_{userId}";

        private class IconGenerationStatus
        {
            public bool IsGenerating { get; set; }
            public bool IsGenerated { get; set; }
            public string? Username { get; set; }
            public DateTime EnqueuedAt { get; set; }
            public DateTime? GeneratedAt { get; set; }
            public string? FileName { get; set; }
        }
    }
}