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
        private readonly IWebHostEnvironment _env;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);

        public ProfileIconGeneratorWorker(
            IServiceProvider serviceProvider,
            ILogger<ProfileIconGeneratorWorker> logger,
            IWebHostEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _env = env;
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

                        string? fileName = null;

                        if (iconBytes != null)
                        {
                            // Сохраняем в папку img
                            fileName = await SaveIconToFolderAsync(userId, iconBytes);

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                // Сохраняем название файла в БД
                                await SaveFileNameToDatabaseAsync(userId, fileName);
                            }
                        }

                        // Помечаем как сгенерированную в кэше
                        queue.MarkAsGenerated(userId, fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в обработчике очереди генерации иконок");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }
        }

        private async Task<string?> SaveIconToFolderAsync(int userId, byte[] iconBytes)
        {
            try
            {
                // Создаем папку img если её нет
                var imgFolder = Path.Combine(_env.WebRootPath, "img");
                if (!Directory.Exists(imgFolder))
                {
                    Directory.CreateDirectory(imgFolder);
                }

                // Генерируем уникальное имя файла
                var fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(imgFolder, fileName);

                // Сохраняем файл
                await System.IO.File.WriteAllBytesAsync(filePath, iconBytes);

                _logger.LogInformation($"Иконка сохранена в файл: {fileName}, путь: {filePath}");

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка сохранения иконки в папку для пользователя ID: {userId}");
                return null;
            }
        }

        private async Task SaveFileNameToDatabaseAsync(int userId, string fileName)
        {
            try
            {
                using var context = new UsersContext();
                var user = await context.Users.FindAsync(userId);

                if (user != null)
                {
                    user.ProfileIconFileName = fileName;
                    user.IsIconGenerated = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation($"Название файла {fileName} сохранено в БД для пользователя ID: {userId}");
                }
                else
                {
                    _logger.LogWarning($"Пользователь ID: {userId} не найден при сохранении названия файла");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка сохранения названия файла в БД для пользователя ID: {userId}");
            }
        }
    }
}