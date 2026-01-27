using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMvc(option => option.EnableEndpointRouting = true);
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
    //String PathFile = Path.Combine(System.AppContext.BaseDirectory, "CodeQuest.xml");
    //option.IncludeXmlComments(PathFile);
});
var app = builder.Build();

app.UseSwagger();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Запросы GET");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "Запросы POST");
    c.SwaggerEndpoint("/swagger/v3/swagger.json", "Запросы PUT");
    c.SwaggerEndpoint("/swagger/v4/swagger.json", "Запросы DELETE");
});
app.Run();