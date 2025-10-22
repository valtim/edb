using DiarioBordo.API.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DiarioBordo.API.Identity;

/// <summary>
/// DbContext para ASP.NET Identity com usuários e papéis customizados
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurações customizadas para ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.CPF).IsUnique();
            entity.HasIndex(u => u.CodigoANAC).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.NomeCompleto).HasMaxLength(200);
            entity.Property(u => u.CPF).HasMaxLength(11).IsFixedLength();
            entity.Property(u => u.CodigoANAC).HasMaxLength(6).IsFixedLength();
        });

        // Configurações para ApplicationRole
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Descricao).HasMaxLength(500);
        });

        // Renomear tabelas para português
        builder.Entity<ApplicationUser>().ToTable("Usuarios");
        builder.Entity<ApplicationRole>().ToTable("Papeis");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuariosPapeis");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UsuariosDeclaracoes");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UsuariosLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UsuariosTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("PapeisDeclaracoes");

        // Seed dos papéis padrão
        SeedRoles(builder);
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        var roles = new[]
        {
            new ApplicationRole
            {
                Id = 1,
                Name = ApplicationRole.Roles.Piloto,
                NormalizedName = ApplicationRole.Roles.Piloto.ToUpperInvariant(),
                Descricao = ApplicationRole.Descriptions.Piloto,
                NivelAcesso = ApplicationRole.AccessLevels.Piloto,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new ApplicationRole
            {
                Id = 2,
                Name = ApplicationRole.Roles.Operador,
                NormalizedName = ApplicationRole.Roles.Operador.ToUpperInvariant(),
                Descricao = ApplicationRole.Descriptions.Operador,
                NivelAcesso = ApplicationRole.AccessLevels.Operador,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new ApplicationRole
            {
                Id = 3,
                Name = ApplicationRole.Roles.DiretorOperacoes,
                NormalizedName = ApplicationRole.Roles.DiretorOperacoes.ToUpperInvariant(),
                Descricao = ApplicationRole.Descriptions.DiretorOperacoes,
                NivelAcesso = ApplicationRole.AccessLevels.DiretorOperacoes,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new ApplicationRole
            {
                Id = 4,
                Name = ApplicationRole.Roles.Fiscalizacao,
                NormalizedName = ApplicationRole.Roles.Fiscalizacao.ToUpperInvariant(),
                Descricao = ApplicationRole.Descriptions.Fiscalizacao,
                NivelAcesso = ApplicationRole.AccessLevels.Fiscalizacao,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new ApplicationRole
            {
                Id = 5,
                Name = ApplicationRole.Roles.Administrador,
                NormalizedName = ApplicationRole.Roles.Administrador.ToUpperInvariant(),
                Descricao = ApplicationRole.Descriptions.Administrador,
                NivelAcesso = ApplicationRole.AccessLevels.Administrador,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        };

        builder.Entity<ApplicationRole>().HasData(roles);
    }
}