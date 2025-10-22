using DiarioBordo.Application.Services;
using DiarioBordo.Application.Validators;
using DiarioBordo.Infrastructure.Data;
using DiarioBordo.Infrastructure.Repositories;
using DiarioBordo.Domain.Repositories;
using FluentValidation;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddControllers();

// Configuração do Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Diário de Bordo Digital ANAC",
        Version = "v1",
        Description = "API para Sistema de Diário de Bordo Digital conforme Resoluções ANAC 457/2017 e 458/2017",
        Contact = new OpenApiContact
        {
            Name = "Suporte Técnico",
            Email = "suporte@diariodebordo.com.br"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietário"
        }
    });

    // Configuração de autenticação JWT no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Incluir comentários XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Tags para organização
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});

// Configuração do NHibernate
builder.Services.AddSingleton<NHibernateHelper>();
builder.Services.AddScoped(provider =>
{
    return NHibernateHelper.GetSessionFactory();
});

// Configuração de repositórios
builder.Services.AddScoped<IRegistroVooRepository, RegistroVooRepository>();
builder.Services.AddScoped<IAeronaveRepository, AeronaveRepository>();
builder.Services.AddScoped<ITripulanteRepository, TripulanteRepository>();
builder.Services.AddScoped<IAssinaturaRegistroRepository, AssinaturaRegistroRepository>();
builder.Services.AddScoped<ILogAuditoriaRepository, LogAuditoriaRepository>();

// Configuração de serviços da aplicação
builder.Services.AddScoped<IRegistroVooService, RegistroVooService>();
builder.Services.AddScoped<IAssinaturaService, AssinaturaService>();
builder.Services.AddScoped<IAeronaveService, AeronaveService>();
builder.Services.AddScoped<ITripulanteService, TripulanteService>();

// Configuração de validadores FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegistroVooDtoValidator>();

// Configuração de CORS (ajustar para produção)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    options.AddPolicy("Production", builder =>
    {
        builder
            .WithOrigins("https://diariodebordo.anac.gov.br", "https://app.diariodebordo.com.br")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configuração de autenticação JWT (TODO: Implementar completamente)
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         // Configurações JWT
//     });

// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy("Piloto", policy => policy.RequireRole("Piloto"));
//     options.AddPolicy("Operador", policy => policy.RequireRole("Operador", "DiretorOperacoes"));
//     options.AddPolicy("Fiscalizacao", policy => policy.RequireRole("Fiscalizacao"));
// });

// Configuração de logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    // TODO: Adicionar logging para arquivo/syslog em produção
});

// Configuração de cache (TODO: Implementar Redis)
builder.Services.AddMemoryCache();

// Configuração de compressão
builder.Services.AddResponseCompression();

var app = builder.Build();

// Pipeline de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Diário de Bordo Digital ANAC v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
        c.DocumentTitle = "Diário de Bordo Digital - API Documentation";
        c.DefaultModelsExpandDepth(-1); // Não expandir modelos por padrão
        c.DisplayRequestDuration();
    });

    app.UseCors("Development");
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseCors("Production");
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// TODO: Habilitar quando autenticação estiver implementada
// app.UseAuthentication();
// app.UseAuthorization();

// Middleware personalizado para logging de auditoria
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var method = context.Request.Method;
    var path = context.Request.Path;
    var userAgent = context.Request.Headers.UserAgent.ToString();
    var ip = context.Connection.RemoteIpAddress?.ToString();

    logger.LogInformation("Request: {Method} {Path} from {IP} ({UserAgent})", method, path, ip, userAgent);

    await next();

    logger.LogInformation("Response: {StatusCode} for {Method} {Path}", context.Response.StatusCode, method, path);
});

app.MapControllers();

// Health checks endpoints
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Message = "Diário de Bordo Digital ANAC - Operacional"
}));

app.Run();

/// <summary>
/// Classe Program para testes de integração
/// </summary>
public partial class Program { }