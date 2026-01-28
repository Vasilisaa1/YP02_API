using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CodeQuest.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку статических файлов
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Регистрируем сервисы
builder.Services.AddSingleton<GigaChatImageService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ProfileIconGenerationQueue>();
builder.Services.AddHostedService<ProfileIconGeneratorWorker>();

// Добавляем IWebHostEnvironment
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

// Настройка аутентификации
var key = Encoding.ASCII.GetBytes("SuperSecretKey12345!");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Настройка Swagger
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Пробная версия"
    });
    option.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v2",
        Title = "Пробная версия"
    });
    option.SwaggerDoc("v3", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v3",
        Title = "Пробная версия"
    });
    option.SwaggerDoc("v4", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v4",
        Title = "Пробная версия"
    });
});

var app = builder.Build();

// Настраиваем папку для статических файлов
app.UseStaticFiles();

// Создаем папку img если её нет
var imgFolder = Path.Combine(app.Environment.WebRootPath, "img");
if (!Directory.Exists(imgFolder))
{
    Directory.CreateDirectory(imgFolder);
}

// Middleware pipeline
app.UseSwagger();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Запросы GET");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "Запросы POST");
    c.SwaggerEndpoint("/swagger/v3/swagger.json", "Запросы PUT");
    c.SwaggerEndpoint("/swagger/v4/swagger.json", "Запросы DELETE");
});

app.Run();