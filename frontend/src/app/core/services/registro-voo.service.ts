import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
    ApiResponse,
    PaginatedResponse,
    RegistroVooDto,
    RegistroVooFilter
} from '../models';

@Injectable({
    providedIn: 'root'
})
export class RegistroVooService {
    private readonly baseUrl = `${environment.apiUrl}/registro-voo`;

    constructor(private http: HttpClient) { }

    // Consulta dos últimos 30 dias - Requisito ANAC Art. 8º II Resolução 457/2017
    obterUltimos30Dias(aeronaveId?: string): Observable<RegistroVooDto[]> {
        let params = new HttpParams();
        if (aeronaveId) {
            params = params.set('aeronaveId', aeronaveId);
        }

        return this.http.get<ApiResponse<RegistroVooDto[]>>(
            `${this.baseUrl}/ultimos-30-dias`,
            { params }
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao obter registros');
            })
        );
    }

    // Busca paginada com filtros
    buscar(filtros: RegistroVooFilter): Observable<PaginatedResponse<RegistroVooDto>> {
        let params = new HttpParams();

        if (filtros.aeronaveId) params = params.set('aeronaveId', filtros.aeronaveId);
        if (filtros.pilotoId) params = params.set('pilotoId', filtros.pilotoId);
        if (filtros.dataInicio) params = params.set('dataInicio', filtros.dataInicio);
        if (filtros.dataFim) params = params.set('dataFim', filtros.dataFim);
        if (filtros.status) params = params.set('status', filtros.status);
        if (filtros.page) params = params.set('page', filtros.page.toString());
        if (filtros.pageSize) params = params.set('pageSize', filtros.pageSize.toString());
        if (filtros.orderBy) params = params.set('orderBy', filtros.orderBy);
        if (filtros.orderDirection) params = params.set('orderDirection', filtros.orderDirection);

        return this.http.get<ApiResponse<PaginatedResponse<RegistroVooDto>>>(
            this.baseUrl,
            { params }
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao buscar registros');
            })
        );
    }

    // Obter por ID
    obterPorId(id: string): Observable<RegistroVooDto> {
        return this.http.get<ApiResponse<RegistroVooDto>>(
            `${this.baseUrl}/${id}`
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Registro não encontrado');
            })
        );
    }

    // Criar novo registro
    criar(registro: Partial<RegistroVooDto>): Observable<RegistroVooDto> {
        return this.http.post<ApiResponse<RegistroVooDto>>(
            this.baseUrl,
            registro
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao criar registro');
            })
        );
    }

    // Atualizar registro
    atualizar(id: string, registro: Partial<RegistroVooDto>): Observable<RegistroVooDto> {
        return this.http.put<ApiResponse<RegistroVooDto>>(
            `${this.baseUrl}/${id}`,
            registro
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao atualizar registro');
            })
        );
    }

    // Assinar registro - Conforme ANAC 458/2017
    assinar(id: string, observacoes?: string): Observable<RegistroVooDto> {
        return this.http.post<ApiResponse<RegistroVooDto>>(
            `${this.baseUrl}/${id}/assinar`,
            { observacoes }
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao assinar registro');
            })
        );
    }

    // Cancelar registro
    cancelar(id: string, motivo: string): Observable<void> {
        return this.http.post<ApiResponse<void>>(
            `${this.baseUrl}/${id}/cancelar`,
            { motivo }
        ).pipe(
            map(response => {
                if (response.success) {
                    return void 0;
                }
                throw new Error(response.message || 'Erro ao cancelar registro');
            })
        );
    }

    // Excluir registro (apenas se não assinado)
    excluir(id: string): Observable<void> {
        return this.http.delete<ApiResponse<void>>(
            `${this.baseUrl}/${id}`
        ).pipe(
            map(response => {
                if (response.success) {
                    return void 0;
                }
                throw new Error(response.message || 'Erro ao excluir registro');
            })
        );
    }

    // Duplicar registro para facilitar criação
    duplicar(id: string): Observable<RegistroVooDto> {
        return this.http.post<ApiResponse<RegistroVooDto>>(
            `${this.baseUrl}/${id}/duplicar`,
            {}
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao duplicar registro');
            })
        );
    }

    // Validar campos obrigatórios ANAC
    validarCamposAnac(registro: Partial<RegistroVooDto>): Observable<{ isValid: boolean; errors: string[] }> {
        return this.http.post<ApiResponse<{ isValid: boolean; errors: string[] }>>(
            `${this.baseUrl}/validar-anac`,
            registro
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro na validação');
            })
        );
    }

    // Obter próximo número sequencial para aeronave
    obterProximoNumeroSequencial(aeronaveId: string): Observable<number> {
        return this.http.get<ApiResponse<number>>(
            `${this.baseUrl}/proximo-numero-sequencial/${aeronaveId}`
        ).pipe(
            map(response => {
                if (response.success && response.data !== undefined) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao obter número sequencial');
            })
        );
    }

    // Relatório para ANAC
    gerarRelatorioAnac(aeronaveId: string, dataInicio: string, dataFim: string): Observable<Blob> {
        let params = new HttpParams()
            .set('aeronaveId', aeronaveId)
            .set('dataInicio', dataInicio)
            .set('dataFim', dataFim);

        return this.http.get(
            `${this.baseUrl}/relatorio-anac`,
            {
                params,
                responseType: 'blob'
            }
        );
    }

    // Exportar registros em PDF
    exportarPDF(filtros: RegistroVooFilter): Observable<Blob> {
        let params = new HttpParams();

        if (filtros.aeronaveId) params = params.set('aeronaveId', filtros.aeronaveId);
        if (filtros.dataInicio) params = params.set('dataInicio', filtros.dataInicio);
        if (filtros.dataFim) params = params.set('dataFim', filtros.dataFim);

        return this.http.get(
            `${this.baseUrl}/exportar-pdf`,
            {
                params,
                responseType: 'blob'
            }
        );
    }
}