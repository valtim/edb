using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Application.DTOs;

/// <summary>
/// DTO estendido para RegistroVoo com campos adicionais para UI
/// </summary>
public partial class RegistroVooDto
{
    // Campos calculados para interface
    public string AeronaveMatricula { get; set; } = string.Empty;
    public string PilotoComandoNome { get; set; } = string.Empty;
    public int? DiasRestantesAssinatura { get; set; }
    public bool PrazoAssinaturaVencido => DiasRestantesAssinatura.HasValue && DiasRestantesAssinatura <= 0;

    // Campos de status expandidos
    public string StatusAssinaturaTexto => GetStatusAssinaturaTexto();
    public string StatusWorkflow => GetStatusWorkflow();

    private string GetStatusAssinaturaTexto()
    {
        if (!StatusAssinaturaPiloto && !StatusAssinaturaOperador)
            return "Pendente assinatura do piloto";

        if (StatusAssinaturaPiloto && !StatusAssinaturaOperador)
            return "Pendente assinatura do operador";

        if (StatusAssinaturaPiloto && StatusAssinaturaOperador)
            return "Completamente assinado";

        return "Status inconsistente";
    }

    private string GetStatusWorkflow()
    {
        if (!StatusAssinaturaPiloto)
            return "AGUARDANDO_PILOTO";

        if (StatusAssinaturaPiloto && !StatusAssinaturaOperador)
            return "AGUARDANDO_OPERADOR";

        return "CONCLUIDO";
    }
}