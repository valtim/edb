using Hangfire;
using Hangfire.Dashboard;
using DiarioBordo.API.Jobs;

namespace DiarioBordo.API.Configuration;

/// <summary>
/// Configuração do Hangfire para jobs em background
/// Sistema de Jobs para conformidade ANAC e manutenção do sistema
/// </summary>
public static class HangfireConfiguration
{
    /// <summary>
    /// Configura o Hangfire com configurações básicas
    /// </summary>
    public static void ConfigureHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        // NOTA: Para usar Hangfire em produção, instalar os pacotes:
        // - Hangfire.SqlServer (para SQL Server)
        // - Hangfire.InMemory (para desenvolvimento)
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();
        });

        // Adicionar servidor Hangfire
        services.AddHangfireServer(options =>
        {
            options.ServerName = Environment.MachineName + ":diario-bordo";
            options.WorkerCount = Environment.ProcessorCount;
            options.ServerTimeout = TimeSpan.FromMinutes(10);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        // Registrar serviços de jobs
        services.AddScoped<IPrazoAssinaturaJob, PrazoAssinaturaJob>();
        services.AddScoped<ICacheManutencaoJob, CacheManutencaoJob>();
        services.AddScoped<IAnacSincronizacaoJob, AnacSincronizacaoJob>();
    }

    /// <summary>
    /// Configurar o middleware do Hangfire
    /// </summary>
    public static void UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Dashboard do Hangfire com autenticação
        var dashboardOptions = new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            DashboardTitle = "Diário de Bordo Digital - Jobs ANAC",
            AppPath = "/",
            StatsPollingInterval = 2000,
            DisplayStorageConnectionString = false
        };

        app.UseHangfireDashboard("/hangfire", dashboardOptions);
    }

    /// <summary>
    /// Configurar jobs recorrentes do sistema
    /// </summary>
    public static void ConfigureRecurringJobs()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        // === JOBS CRÍTICOS DE CONFORMIDADE ANAC ===

        // 1. Verificação de prazos de assinatura (CRÍTICO)
        // Executado a cada hora para garantir conformidade regulatória
        RecurringJob.AddOrUpdate<IPrazoAssinaturaJob>(
            "prazo-assinatura-verificacao",
            job => job.ExecutarVerificacaoPrazosAsync(),
            Cron.Hourly,
            new RecurringJobOptions { TimeZone = timeZone });

        // 2. Notificação de prazos próximos (CRÍTICO)
        // Executado a cada 30 minutos durante horário comercial
        RecurringJob.AddOrUpdate<IPrazoAssinaturaJob>(
            "prazo-notificacao-proximos",
            job => job.NotificarPrazosProximosVencimentoAsync(),
            "*/30 8-18 * * 1-5", // A cada 30min, 8h-18h, seg-sex
            new RecurringJobOptions { TimeZone = timeZone });

        // 3. Sincronização com ANAC (CRÍTICO)
        // Executado a cada hora para manter conformidade
        RecurringJob.AddOrUpdate<IAnacSincronizacaoJob>(
            "anac-sincronizacao-principal",
            job => job.SincronizarRegistrosAsync(),
            Cron.Hourly,
            new RecurringJobOptions { TimeZone = timeZone });

        // === JOBS DE MANUTENÇÃO E PERFORMANCE ===

        // 4. Limpeza de cache (PERFORMANCE)
        // Executado diariamente às 02:00
        RecurringJob.AddOrUpdate<ICacheManutencaoJob>(
            "cache-limpeza-diaria",
            job => job.LimparCacheExpiradoAsync(),
            Cron.Daily(2),
            new RecurringJobOptions { TimeZone = timeZone });

        // 5. Preaquecimento do cache 30 dias (ANAC)
        // Executado diariamente às 05:00 (baixo uso)
        RecurringJob.AddOrUpdate<ICacheManutencaoJob>(
            "cache-preaquecimento-30dias",
            job => job.PreaquecerCache30DiasAsync(),
            Cron.Daily(5),
            new RecurringJobOptions { TimeZone = timeZone });

        // === JOBS DE MONITORAMENTO E RELATÓRIOS ===

        // 6. Verificação de status ANAC
        // Executado a cada 4 horas
        RecurringJob.AddOrUpdate<IAnacSincronizacaoJob>(
            "anac-status-verificacao",
            job => job.VerificarStatusSincronizacaoAsync(),
            "0 */4 * * *", // A cada 4 horas
            new RecurringJobOptions { TimeZone = timeZone });

        // 7. Relatório diário de assinaturas
        // Executado diariamente às 08:00
        RecurringJob.AddOrUpdate<IPrazoAssinaturaJob>(
            "relatorio-assinaturas-diario",
            job => job.GerarRelatorioStatusAssinaturasAsync(),
            Cron.Daily(8),
            new RecurringJobOptions { TimeZone = timeZone });

        // 8. Relatório de sincronização ANAC
        // Executado diariamente às 23:00
        RecurringJob.AddOrUpdate<IAnacSincronizacaoJob>(
            "relatorio-sincronizacao-anac",
            job => job.GerarRelatorioSincronizacaoAsync(),
            Cron.Daily(23),
            new RecurringJobOptions { TimeZone = timeZone });

        // === JOBS SEMANAIS DE MANUTENÇÃO ===

        // 9. Verificação de integridade do cache
        // Executado semanalmente aos domingos às 03:00
        RecurringJob.AddOrUpdate<ICacheManutencaoJob>(
            "cache-integridade-semanal",
            job => job.VerificarIntegridadeCacheAsync(),
            Cron.Weekly(DayOfWeek.Sunday, 3),
            new RecurringJobOptions { TimeZone = timeZone });

        // 10. Validação de conformidade regulatória completa
        // Executado semanalmente aos domingos às 04:00
        RecurringJob.AddOrUpdate<IAnacSincronizacaoJob>(
            "conformidade-regulatoria-semanal",
            job => job.ValidarConformidadeRegulatoria(),
            Cron.Weekly(DayOfWeek.Sunday, 4),
            new RecurringJobOptions { TimeZone = timeZone });

        // === JOBS DE RECUPERAÇÃO ===

        // 11. Reenvio de registros com falha
        // Executado diariamente às 06:00
        RecurringJob.AddOrUpdate<IAnacSincronizacaoJob>(
            "anac-reenvio-falhas",
            job => job.ReenviarRegistrosFalhadosAsync(),
            Cron.Daily(6),
            new RecurringJobOptions { TimeZone = timeZone });

        // 12. Relatório de performance do cache
        // Executado diariamente às 23:30
        RecurringJob.AddOrUpdate<ICacheManutencaoJob>(
            "cache-performance-relatorio",
            job => job.GerarRelatorioPerformanceCacheAsync(),
            "30 23 * * *", // 23:30 todos os dias
            new RecurringJobOptions { TimeZone = timeZone });
    }

    /// <summary>
    /// Enfileirar jobs imediatos (executar apenas uma vez na inicialização)
    /// </summary>
    public static void EnqueueImmediateJobs()
    {
        // Job de inicialização para preaquecimento do cache
        BackgroundJob.Enqueue<ICacheManutencaoJob>(job => job.PreaquecerCache30DiasAsync());

        // Verificação inicial de status ANAC
        BackgroundJob.Enqueue<IAnacSincronizacaoJob>(job => job.VerificarStatusSincronizacaoAsync());
    }
}

/// <summary>
/// Filtro de autorização para o dashboard do Hangfire
/// Permite acesso apenas para usuários autenticados com role apropriada
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Verificar autenticação
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Verificar roles apropriadas (Administrador, DiretorOperacoes, Fiscalizacao)
        var allowedRoles = new[] { "Administrador", "DiretorOperacoes", "Fiscalizacao" };
        return allowedRoles.Any(role => httpContext.User.IsInRole(role));
    }
}

/// <summary>
/// Extensões para configuração simplificada
/// </summary>
public static class HangfireJobExtensions
{
    /// <summary>
    /// Configurar monitoramento de jobs críticos
    /// </summary>
    public static void ConfigureJobMonitoring(this IServiceCollection services)
    {
        // Configurar filtros globais para monitoramento
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });
    }
}