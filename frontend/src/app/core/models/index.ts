// Interfaces para DTOs que comunicam com a API

export interface LoginDto {
    email: string;
    password: string;
    rememberMe: boolean;
    twoFactorCode?: string;
}

export interface LoginResponseDto {
    token: string;
    refreshToken: string;
    user: UserDto;
    expiresAt: string;
    requiresTwoFactor: boolean;
}

export interface UserDto {
    id: string;
    nome: string;
    email: string;
    codigoAnac: string;
    role: UserRole;
    ativo: boolean;
    ultimoLogin?: string;
    mfaEnabled: boolean;
}

export interface RegistroVooDto {
    id?: string;
    numeroSequencial: number;
    aeronaveId: string;
    aeronave?: AeronaveDto;

    // Dados obrigatórios ANAC 457/2017 - Art. 4º
    pilotoComandoId: string;
    copilotoId?: string;
    data: string; // ISO date string
    aeroportoPartida: string;
    aeroportoDestino: string;
    horarioDecolagem: string; // ISO datetime UTC
    horarioPouso: string; // ISO datetime UTC
    tempoVooIFR: number; // minutos
    combustivelQuantidade: number;
    combustivelUnidade: string;
    naturezaVoo: string;
    pessoasBordo: number;
    carga: number;
    ocorrencias?: string;
    discrepanciasTecnicas?: string;
    acoesCorretivas?: string;
    ultimaManutencao?: string;
    proximaManutencao?: string;
    horasCelula: number;
    responsavelAprovacao: string;

    // Dados de controle
    status: RegistroStatus;
    assinaturaPiloto?: AssinaturaDto;
    assinaturaOperador?: AssinaturaDto;
    criadoEm: string;
    atualizadoEm: string;
}

export interface AeronaveDto {
    id: string;
    prefixo: string;
    modelo: string;
    fabricante: string;
    numeroSerie: string;
    categoria: string;
    tipoOperacao: TipoOperacao;
    ativa: boolean;
}

export interface AssinaturaDto {
    id: string;
    usuarioId: string;
    usuario?: UserDto;
    tipoAssinatura: TipoAssinatura;
    hash: string;
    timestamp: string;
    enderecoIP: string;
    userAgent: string;
    observacoes?: string;
}

export interface DashboardDto {
    estatisticas: {
        totalRegistrosUltimos30Dias: number;
        registrosPendentesAssinatura: number;
        horasVoadasMes: number;
        aeronavesMaisUsadas: { aeronave: string; horas: number }[];
    };
    alertas: AlertaDto[];
    registrosRecentes: RegistroVooDto[];
}

export interface AlertaDto {
    id: string;
    tipo: TipoAlerta;
    titulo: string;
    descricao: string;
    prioridade: PrioridadeAlerta;
    registroVooId?: string;
    criadoEm: string;
    lido: boolean;
}

export interface RelatorioAnacDto {
    periodo: {
        inicio: string;
        fim: string;
    };
    aeronaveId?: string;
    registros: RegistroVooDto[];
    totalizadores: {
        totalVoos: number;
        totalHoras: number;
        totalCombustivel: number;
        totalPassageiros: number;
    };
    assinaturaDigital: string;
    geradoEm: string;
    geradoPor: string;
}

// Enums
export enum UserRole {
    Piloto = 'Piloto',
    Operador = 'Operador',
    DiretorOperacoes = 'DiretorOperacoes',
    Fiscalizacao = 'Fiscalizacao'
}

export enum RegistroStatus {
    Rascunho = 'Rascunho',
    PendenteAssinaturaPiloto = 'PendenteAssinaturaPiloto',
    PendenteAssinaturaOperador = 'PendenteAssinaturaOperador',
    Finalizado = 'Finalizado',
    Cancelado = 'Cancelado'
}

export enum TipoOperacao {
    RBAC121 = 'RBAC121',
    RBAC135 = 'RBAC135',
    RBAC91 = 'RBAC91',
    Outros = 'Outros'
}

export enum TipoAssinatura {
    Piloto = 'Piloto',
    Operador = 'Operador'
}

export enum TipoAlerta {
    PrazoAssinatura = 'PrazoAssinatura',
    ManutencaoVencida = 'ManutencaoVencida',
    ErroSincronizacao = 'ErroSincronizacao',
    RegistroIncompleto = 'RegistroIncompleto'
}

export enum PrioridadeAlerta {
    Baixa = 'Baixa',
    Media = 'Media',
    Alta = 'Alta',
    Critica = 'Critica'
}

// Interfaces de resposta da API
export interface ApiResponse<T> {
    success: boolean;
    data?: T;
    message?: string;
    errors?: string[];
    timestamp: string;
}

export interface PaginatedResponse<T> {
    items: T[];
    totalItems: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
    hasNext: boolean;
    hasPrevious: boolean;
}

// Filtros para consultas
export interface RegistroVooFilter {
    aeronaveId?: string;
    pilotoId?: string;
    dataInicio?: string;
    dataFim?: string;
    status?: RegistroStatus;
    page?: number;
    pageSize?: number;
    orderBy?: string;
    orderDirection?: 'ASC' | 'DESC';
}

export interface NotificacaoSignalR {
    tipo: string;
    titulo: string;
    mensagem: string;
    dados?: any;
    timestamp: string;
    userId?: string;
}