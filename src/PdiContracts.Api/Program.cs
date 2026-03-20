using PdiContracts.Domain;
using DotNetEnv;

static string GetRequiredEnv(string name)
{
    return Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"{name} nao configurado no .env");
}

static int GetRequiredIntEnv(string name)
{
    var value = GetRequiredEnv(name);
    if (!int.TryParse(value, out var parsed))
    {
        throw new InvalidOperationException($"{name} deve ser um numero inteiro valido no .env");
    }

    return parsed;
}

// Carregar variáveis de ambiente do arquivo .env (na raiz do repositório)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
}
Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configurar Flipt Feature Flags usando variaveis de ambiente
builder.Services.AddFliptClient(settings =>
{
    settings.FliptUrl = GetRequiredEnv("FLIPT_URL");
    settings.ApiToken = GetRequiredEnv("FLIPT_API_TOKEN");
    settings.NamespaceKey = GetRequiredEnv("FLIPT_NAMESPACE_KEY");
    settings.TimeoutSeconds = GetRequiredIntEnv("FLIPT_TIMEOUT_SECONDS");
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "PDI Contracts API",
        Version = "v1",
        Description = "API para processamento de contratos com suporte a idempotência"
    });
});

// Configurar CORS (se necessário)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PDI Contracts API v1");
    c.RoutePrefix = string.Empty; // Swagger UI na raiz (http://localhost:5000)
    c.DocumentTitle = "PDI Contracts API";
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
