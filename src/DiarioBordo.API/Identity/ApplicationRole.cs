using Microsoft.AspNetCore.Identity;

namespace DiarioBordo.API.Identity;

/// <summary>
/// Papéis do sistema conforme estrutura organizacional da aviação civil
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    /// <summary>
    /// Descrição detalhada do papel
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Nível de acesso (1-10, sendo 10 o mais alto)
    /// </summary>
    public int NivelAcesso { get; set; }

    /// <summary>
    /// Indica se o papel está ativo
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Data de criação do papel
    /// </summary>
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Papéis padrão do sistema
    public static class Roles
    {
        public const string Piloto = "Piloto";
        public const string Operador = "Operador";
        public const string DiretorOperacoes = "DiretorOperacoes";
        public const string Fiscalizacao = "Fiscalizacao";
        public const string Administrador = "Administrador";
    }

    public static class Descriptions
    {
        public const string Piloto = "Piloto em comando - Pode criar e assinar registros de voo como PIC";
        public const string Operador = "Operador de aeronave - Pode gerenciar registros, aeronaves e tripulantes";
        public const string DiretorOperacoes = "Diretor de Operações - Pode aprovar registros e gerenciar operações";
        public const string Fiscalizacao = "Fiscalização ANAC - Pode auditar todos os registros do sistema";
        public const string Administrador = "Administrador do sistema - Acesso total para configuração";
    }

    public static class AccessLevels
    {
        public const int Piloto = 3;
        public const int Operador = 5;
        public const int DiretorOperacoes = 7;
        public const int Fiscalizacao = 9;
        public const int Administrador = 10;
    }
}