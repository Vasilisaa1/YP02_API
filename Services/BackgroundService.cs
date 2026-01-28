using CodeQuest.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeQuest.Services
{
    public class ProfileIconGeneratorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProfileIconGeneratorWorker> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);

        public ProfileIconGeneratorWorker(
            IServiceProvider serviceProvider,
            ILogger<ProfileIconGeneratorWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис генерации иконок запущен");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var queue = scope.ServiceProvider.GetRequiredService<ProfileIconGenerationQueue>();
                    var imageService = scope.ServiceProvider.GetRequiredService<GigaChatImageService>();

                    if (queue.TryDequeue(out var item))
                    {
                        var (userId, username) = item;

                        _logger.LogInformation($"Начинаю генерацию иконки для пользователя: {username} (ID: {userId})");

                        // Генерируем иконку
                        var iconBytes = await imageService.GenerateProfileIconAsync(username);

                        // Помечаем как сгенерированную
                        queue.MarkAsGenerated(userId, iconBytes);

                        // Сохраняем в базу данных
                        await SaveIconToDatabaseAsync(scope, userId, iconBytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в обработчике очереди генерации иконок");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }
        }

        private async Task SaveIconToDatabaseAsync(IServiceScope scope, int userId, byte[]? iconBytes)
        {
            try
            {
                using var context = new UsersContext();
                var user = await context.Users.FindAsync(userId);

                if (user != null)
                {
                    // Создаем новый контекст для гарантированного отслеживания
                    using var freshContext = new UsersContext();
                    var freshUser = await freshContext.Users.FindAsync(userId);

                    if (freshUser != null)
                    {
                        freshUser.ProfileIcon = iconBytes;
                        freshUser.ProfileIconMimeType = iconBytes != null ? "image/png" : null;
                        freshUser.IsIconGenerated = iconBytes != null;

                        await freshContext.SaveChangesAsync();
                        _logger.LogInformation($"Иконка сохранена в БД для пользователя ID: {userId}, размер: {iconBytes?.Length ?? 0} байт");
                    }
                    else
                    {
                        _logger.LogWarning($"Пользователь ID: {userId} не найден при сохранении иконки");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка сохранения иконки в БД для пользователя ID: {userId}");
            }
        }
    }
}