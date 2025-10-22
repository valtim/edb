using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using DiarioBordo.Infrastructure.Data.Mappings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiarioBordo.Infrastructure.Data;

/// <summary>
/// Configuração e factory do NHibernate para o sistema de Diário de Bordo
/// </summary>
public class NHibernateHelper
{
    private static ISessionFactory? _sessionFactory;
    private static readonly object _lock = new object();

    /// <summary>
    /// Obtém a SessionFactory singleton
    /// </summary>
    public static ISessionFactory SessionFactory
    {
        get
        {
            if (_sessionFactory == null)
            {
                throw new InvalidOperationException(
                    "SessionFactory não foi inicializada. Chame Initialize() primeiro.");
            }
            return _sessionFactory;
        }
    }

    /// <summary>
    /// Obtém a SessionFactory (método de compatibilidade)
    /// </summary>
    public static ISessionFactory GetSessionFactory()
    {
        return SessionFactory;
    }

    /// <summary>
    /// Inicializa a SessionFactory com a configuração fornecida
    /// </summary>
    public static void Initialize(string connectionString, bool isDevelopment = false)
    {
        if (_sessionFactory == null)
        {
            lock (_lock)
            {
                if (_sessionFactory == null)
                {
                    _sessionFactory = BuildSessionFactory(connectionString, isDevelopment);
                }
            }
        }
    }

    /// <summary>
    /// Constrói a SessionFactory com todas as configurações necessárias
    /// </summary>
    private static ISessionFactory BuildSessionFactory(string connectionString, bool isDevelopment)
    {
        return Fluently.Configure()
            .Database(MySQLConfiguration.Standard
                .ConnectionString(connectionString)
                .ShowSql() // Sempre mostrar SQL para auditoria
                .FormatSql()
                .AdoNetBatchSize(30)) // Otimização para batch operations
            .Mappings(m => m.FluentMappings
                .AddFromAssemblyOf<AeronaveMap>() // Adiciona todos os mappings da assembly
                .Conventions.Add(
                    // Convenções padrão
                    FluentNHibernate.Conventions.Helpers.Table.Is(x => x.EntityType.Name + "s"),
                    FluentNHibernate.Conventions.Helpers.PrimaryKey.Name.Is(x => "Id"),
                    FluentNHibernate.Conventions.Helpers.ForeignKey.EndsWith("Id")))
            .ExposeConfiguration(cfg =>
            {
                // Segunda-level cache com Redis (configurado externamente)
                cfg.SetProperty(NHibernate.Cfg.Environment.UseSecondLevelCache, "true");
                cfg.SetProperty(NHibernate.Cfg.Environment.UseQueryCache, "true");
                cfg.SetProperty(NHibernate.Cfg.Environment.CacheProvider,
                    "NHibernate.Caches.Redis.RedisCacheProvider, NHibernate.Caches.Redis");

                // Configurações de timezone UTC
                cfg.SetProperty(NHibernate.Cfg.Environment.SqlExceptionConverter,
                    "NHibernate.Exception.MySQLExceptionConverter");

                // Logging de SQL para auditoria (Res. 458/2017)
                cfg.SetProperty(NHibernate.Cfg.Environment.ShowSql, "true");
                cfg.SetProperty(NHibernate.Cfg.Environment.FormatSql, "true");

                // Configurações de performance
                cfg.SetProperty(NHibernate.Cfg.Environment.DefaultBatchFetchSize, "16");
                cfg.SetProperty(NHibernate.Cfg.Environment.BatchSize, "30");

                // Schema export em desenvolvimento
                if (isDevelopment)
                {
                    var schemaExport = new SchemaExport(cfg);
                    schemaExport.Create(true, false); // Script + Execute
                }
            })
            .BuildSessionFactory();
    }

    /// <summary>
    /// Fecha a SessionFactory (chamado no shutdown da aplicação)
    /// </summary>
    public static void CloseSessionFactory()
    {
        lock (_lock)
        {
            if (_sessionFactory != null)
            {
                _sessionFactory.Close();
                _sessionFactory.Dispose();
                _sessionFactory = null;
            }
        }
    }

    /// <summary>
    /// Obtém uma nova sessão
    /// </summary>
    public static ISession OpenSession()
    {
        return SessionFactory.OpenSession();
    }

    /// <summary>
    /// Executa uma operação com transação automática
    /// </summary>
    public static async Task<T> ExecuteInTransactionAsync<T>(Func<ISession, Task<T>> operation)
    {
        using var session = OpenSession();
        using var transaction = session.BeginTransaction();

        try
        {
            var result = await operation(session);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Executa uma operação com transação automática (sem retorno)
    /// </summary>
    public static async Task ExecuteInTransactionAsync(Func<ISession, Task> operation)
    {
        using var session = OpenSession();
        using var transaction = session.BeginTransaction();

        try
        {
            await operation(session);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}