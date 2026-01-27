using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CodeQuest.Services
{
    public class GigaChatImageService
    {
        private readonly string _clientId;
        private readonly string _authorizationKey;
        private readonly HttpClient _httpClient;
        private string? _cachedToken;
        private DateTime _tokenExpiry;

        public GigaChatImageService(IConfiguration configuration)
        {
            _clientId = configuration["GigaChat:ClientId"] ?? throw new ArgumentNullException("GigaChat:ClientId");
            _authorizationKey = configuration["GigaChat:AuthorizationKey"] ?? throw new ArgumentNullException("GigaChat:AuthorizationKey");

            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            });
            _httpClient.Timeout = TimeSpan.FromSeconds(180);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // Проверяем, действителен ли кэшированный токен
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
            var rqUID = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("RqUID", rqUID);
            request.Headers.Add("Authorization", $"Bearer {_authorizationKey}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ошибка получения токена: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (tokenResponse?.access_token == null)
            {
                throw new Exception("Не удалось получить access token");
            }

            _cachedToken = tokenResponse.access_token;
            _tokenExpiry = DateTime.UtcNow.AddHours(23); // Токен обычно действителен 24 часа

            return _cachedToken;
        }

        public async Task<byte[]?> GenerateProfileIconAsync(string username)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                var animalPrompt = GetRandomAnimalPrompt(username);

                // Генерация изображения с небольшим таймаутом
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var fileId = await GenerateImageAsync(token, animalPrompt, cts.Token);

                if (string.IsNullOrEmpty(fileId))
                {
                    // Возвращаем null если не удалось получить file_id
                    return null;
                }

                // Скачивание изображения
                var imageBytes = await DownloadImageAsync(token, fileId, cts.Token);

                return imageBytes;
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем null
                return null;
            }
        }

        private async Task<string?> GenerateImageAsync(string token, string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {token}");

                // ОРИГИНАЛЬНЫЙ промпт, только убрал "Сделай за 15 секунд" - это лишнее
                var requestBody = new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "Ты — генератор иконок для профилей." +
                            " Создай стилизованное изображение животного в круглой рамке, " +
                            "подходящее для аватара профиля. " +
                            "Изображение должно быть простым, стилизованным и узнаваемым." +
                            " Верни только тег img с изображением."
                        },
                        new
                        {
                            role = "user",
                            content = $"Создай иконку профиля с изображением {prompt}." +
                            $" Иконка должна быть круглой," +
                            $" минималистичной и подходить для аватара пользователя."
                        }
                    },
                    function_call = "auto",
                    max_tokens = 100 // Добавил ограничение токенов
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (responseData?.choices != null && responseData.choices.Count > 0)
                {
                    string apiContent = responseData.choices[0].message.content?.ToString();

                    if (!string.IsNullOrEmpty(apiContent) && apiContent.Contains("img src="))
                    {
                        int startIndex = apiContent.IndexOf("src=\"") + 5;
                        int endIndex = apiContent.IndexOf("\"", startIndex);

                        if (startIndex >= 5 && endIndex > startIndex)
                        {
                            return apiContent.Substring(startIndex, endIndex - startIndex);
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<byte[]?> DownloadImageAsync(string token, string fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "image/jpeg,image/png");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private string GetRandomAnimalPrompt(string username)
        {
            // ОРИГИНАЛЬНЫЙ список животных
            var animals = new[]
            {
                "кота", "собаки", "лисы", "волка", "медведя", "тигра", "льва",
                "панды", "енота", "зайца", "белки", "совы", "орла", "дельфина",
                "кита", "акулы", "дракона", "единорога", "феникса", "пегаса"
            };

            // Используем имя пользователя как seed для детерминированной генерации
            var seed = username.GetHashCode();
            var random = new Random(seed);
            var animal = animals[random.Next(animals.Length)];

            // ОРИГИНАЛЬНЫЕ списки стилей и цветов
            var styles = new[]
            {
                "минималистичный", "геометрический", "плоский дизайн", "пиксель арт",
                "мультяшный", "реалистичный", "абстрактный", "акварельный"
            };

            var colors = new[]
            {
                "синий", "зеленый", "красный", "фиолетовый", "оранжевый", "розовый",
                "бирюзовый", "золотой", "серебряный", "градиент"
            };

            var style = styles[random.Next(styles.Length)];
            var color = colors[random.Next(colors.Length)];

            return $"{style} {color} {animal}";
        }
    }
}