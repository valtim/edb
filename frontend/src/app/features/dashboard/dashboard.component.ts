import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import {
    AlertaDto,
    PrioridadeAlerta,
    RegistroStatus,
    RegistroVooDto,
    UserDto,
    UserRole
} from '../../core/models';
import { AuthService } from '../../core/services/auth.service';
import { RegistroVooService } from '../../core/services/registro-voo.service';

interface DashboardCard {
    title: string;
    value: number | string;
    icon: string;
    color: string;
    subtitle?: string;
    route?: string;
}

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatChipsModule,
        MatTableModule,
        MatProgressSpinnerModule,
        MatBadgeModule,
        MatTooltipModule
    ],
    template: `
    <div class="dashboard-container">
      <!-- Header de boas-vindas -->
      <div class="welcome-section">
        <h1>Bem-vindo, {{currentUser?.nome}}!</h1>
        <p>{{getRoleDescription()}} - Código ANAC: {{currentUser?.codigoAnac}}</p>
      </div>

      <!-- Cards de estatísticas -->
      <div class="stats-grid">
        <mat-card *ngFor="let card of dashboardCards" 
                  class="stat-card"
                  [class]="'card-' + card.color">
          <mat-card-content>
            <div class="stat-content">
              <div class="stat-info">
                <h3>{{card.value}}</h3>
                <p>{{card.title}}</p>
                <small *ngIf="card.subtitle">{{card.subtitle}}</small>
              </div>
              <div class="stat-icon">
                <mat-icon [style.color]="getIconColor(card.color)">{{card.icon}}</mat-icon>
              </div>
            </div>
          </mat-card-content>
          <mat-card-actions *ngIf="card.route">
            <button mat-button 
                    [routerLink]="card.route" 
                    [style.color]="getIconColor(card.color)">
              Ver Detalhes
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <!-- Alertas importantes -->
      <mat-card class="alerts-card" *ngIf="alertas.length > 0">
        <mat-card-header>
          <mat-card-title>
            <mat-icon>warning</mat-icon>
            Alertas Importantes
          </mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <div class="alerts-list">
            <div *ngFor="let alerta of alertas" 
                 class="alert-item"
                 [class]="'alert-' + alerta.prioridade.toLowerCase()">
              <div class="alert-content">
                <div class="alert-info">
                  <h4>{{alerta.titulo}}</h4>
                  <p>{{alerta.descricao}}</p>
                  <small>{{alerta.criadoEm | date:'dd/MM/yyyy HH:mm'}}</small>
                </div>
                <div class="alert-priority">
                  <mat-chip [class]="'chip-' + alerta.prioridade.toLowerCase()">
                    {{alerta.prioridade}}
                  </mat-chip>
                </div>
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <div class="content-grid">
        <!-- Registros recentes -->
        <mat-card class="recent-records-card">
          <mat-card-header>
            <mat-card-title>
              <mat-icon>history</mat-icon>
              Registros Recentes
            </mat-card-title>
            <mat-card-subtitle>Últimos 10 registros de voo</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div *ngIf="isLoadingRecords" class="loading-container">
              <mat-spinner diameter="40"></mat-spinner>
            </div>
            
            <div *ngIf="!isLoadingRecords && registrosRecentes.length === 0" 
                 class="empty-state">
              <mat-icon>flight_off</mat-icon>
              <p>Nenhum registro encontrado</p>
              <button mat-raised-button 
                      color="primary" 
                      [routerLink]="['/registro-voo/novo']"
                      *ngIf="canCreateRecord()">
                Criar Primeiro Registro
              </button>
            </div>

            <div *ngIf="!isLoadingRecords && registrosRecentes.length > 0" 
                 class="records-table">
              <mat-table [dataSource]="registrosRecentes">
                <ng-container matColumnDef="data">
                  <mat-header-cell *matHeaderCellDef>Data</mat-header-cell>
                  <mat-cell *matCellDef="let record">
                    {{record.data | date:'dd/MM/yyyy'}}
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="aeronave">
                  <mat-header-cell *matHeaderCellDef>Aeronave</mat-header-cell>
                  <mat-cell *matCellDef="let record">
                    {{record.aeronave?.prefixo || 'N/A'}}
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="rota">
                  <mat-header-cell *matHeaderCellDef>Rota</mat-header-cell>
                  <mat-cell *matCellDef="let record">
                    {{record.aeroportoPartida}} → {{record.aeroportoDestino}}
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="status">
                  <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
                  <mat-cell *matCellDef="let record">
                    <mat-chip [class]="'status-' + record.status.toLowerCase()">
                      {{getStatusText(record.status)}}
                    </mat-chip>
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="actions">
                  <mat-header-cell *matHeaderCellDef>Ações</mat-header-cell>
                  <mat-cell *matCellDef="let record">
                    <button mat-icon-button 
                            [routerLink]="['/registro-voo', record.id]"
                            matTooltip="Ver detalhes">
                      <mat-icon>visibility</mat-icon>
                    </button>
                    <button mat-icon-button 
                            [routerLink]="['/registro-voo', record.id, 'edit']"
                            matTooltip="Editar"
                            *ngIf="canEditRecord(record)">
                      <mat-icon>edit</mat-icon>
                    </button>
                  </mat-cell>
                </ng-container>

                <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
                <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
              </mat-table>
            </div>
          </mat-card-content>
          <mat-card-actions>
            <button mat-button [routerLink]="['/registro-voo']">
              Ver Todos os Registros
            </button>
          </mat-card-actions>
        </mat-card>

        <!-- Ações rápidas -->
        <mat-card class="quick-actions-card">
          <mat-card-header>
            <mat-card-title>
              <mat-icon>flash_on</mat-icon>
              Ações Rápidas
            </mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="quick-actions">
              <button mat-raised-button 
                      color="primary" 
                      [routerLink]="['/registro-voo/novo']"
                      *ngIf="canCreateRecord()"
                      class="quick-action-btn">
                <mat-icon>add</mat-icon>
                Novo Registro
              </button>

              <button mat-raised-button 
                      [routerLink]="['/assinaturas']"
                      *ngIf="hasRole([userRoles.Piloto, userRoles.Operador])"
                      class="quick-action-btn"
                      [matBadge]="pendingSignatures"
                      [matBadgeHidden]="pendingSignatures === 0"
                      matBadgeColor="warn">
                <mat-icon>edit_note</mat-icon>
                Assinatura Pendente
              </button>

              <button mat-raised-button 
                      [routerLink]="['/relatorios']"
                      *ngIf="hasRole([userRoles.Operador, userRoles.DiretorOperacoes])"
                      class="quick-action-btn">
                <mat-icon>assessment</mat-icon>
                Relatórios ANAC
              </button>

              <button mat-raised-button 
                      [routerLink]="['/aeronaves']"
                      *ngIf="hasRole([userRoles.Operador, userRoles.DiretorOperacoes])"
                      class="quick-action-btn">
                <mat-icon>airplanemode_active</mat-icon>
                Gerenciar Aeronaves
              </button>

              <button mat-raised-button 
                      [routerLink]="['/tripulacao']"
                      *ngIf="hasRole([userRoles.Operador, userRoles.DiretorOperacoes])"
                      class="quick-action-btn">
                <mat-icon>group</mat-icon>
                Gerenciar Tripulação
              </button>

              <button mat-raised-button 
                      [routerLink]="['/configuracoes']"
                      class="quick-action-btn">
                <mat-icon>settings</mat-icon>
                Configurações
              </button>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Informações de conformidade ANAC -->
      <mat-card class="compliance-info-card">
        <mat-card-content>
          <div class="compliance-content">
            <div class="compliance-text">
              <h3>
                <mat-icon>verified</mat-icon>
                Sistema em Conformidade ANAC
              </h3>
              <p>
                Este sistema atende integralmente às <strong>Resoluções ANAC 457/2017</strong> e 
                <strong>458/2017</strong>, garantindo a autenticidade, integridade e 
                disponibilidade dos registros de voo por 30 dias.
              </p>
              <div class="compliance-features">
                <mat-chip-set>
                  <mat-chip>17 Campos Obrigatórios</mat-chip>
                  <mat-chip>Assinaturas Digitais</mat-chip>
                  <mat-chip>Disponibilidade 30 Dias</mat-chip>
                  <mat-chip>Logs de Auditoria</mat-chip>
                  <mat-chip>Backup Automático</mat-chip>
                </mat-chip-set>
              </div>
            </div>
            <div class="compliance-logo">
              <img src="/icons/icon-192x192.png" alt="Sistema Certificado" width="64">
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`
    .dashboard-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    .welcome-section {
      margin-bottom: 32px;
    }

    .welcome-section h1 {
      margin: 0 0 8px 0;
      color: #1976d2;
      font-size: 28px;
      font-weight: 500;
    }

    .welcome-section p {
      margin: 0;
      color: #666;
      font-size: 16px;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 24px;
      margin-bottom: 32px;
    }

    .stat-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 20px rgba(0,0,0,0.1);
    }

    .stat-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .stat-info h3 {
      margin: 0 0 8px 0;
      font-size: 32px;
      font-weight: 600;
    }

    .stat-info p {
      margin: 0 0 4px 0;
      font-size: 16px;
      color: #666;
    }

    .stat-info small {
      color: #999;
      font-size: 12px;
    }

    .stat-icon mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }

    .card-primary .stat-info h3 { color: #1976d2; }
    .card-accent .stat-info h3 { color: #ff5722; }
    .card-warn .stat-info h3 { color: #f57c00; }
    .card-success .stat-info h3 { color: #388e3c; }

    .alerts-card {
      margin-bottom: 32px;
    }

    .alerts-card mat-card-title {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #f57c00;
    }

    .alerts-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .alert-item {
      padding: 16px;
      border-radius: 8px;
      border-left: 4px solid;
    }

    .alert-critica { 
      background-color: #ffebee; 
      border-left-color: #f44336; 
    }

    .alert-alta { 
      background-color: #fff3e0; 
      border-left-color: #ff9800; 
    }

    .alert-media { 
      background-color: #fff8e1; 
      border-left-color: #ffc107; 
    }

    .alert-baixa { 
      background-color: #f3e5f5; 
      border-left-color: #9c27b0; 
    }

    .alert-content {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
    }

    .alert-info h4 {
      margin: 0 0 8px 0;
      font-size: 16px;
      font-weight: 500;
    }

    .alert-info p {
      margin: 0 0 8px 0;
      color: #666;
    }

    .alert-info small {
      color: #999;
      font-size: 12px;
    }

    .chip-critica { background-color: #f44336; color: white; }
    .chip-alta { background-color: #ff9800; color: white; }
    .chip-media { background-color: #ffc107; color: black; }
    .chip-baixa { background-color: #9c27b0; color: white; }

    .content-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
      margin-bottom: 32px;
    }

    .loading-container, .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      color: #666;
    }

    .empty-state mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
      opacity: 0.5;
    }

    .records-table {
      width: 100%;
    }

    .status-rascunho { background-color: #e0e0e0; }
    .status-pendenteassinaturapiloto { background-color: #fff3e0; }
    .status-pendenteassinaturaoperador { background-color: #e3f2fd; }
    .status-finalizado { background-color: #e8f5e8; }
    .status-cancelado { background-color: #ffebee; }

    .quick-actions {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .quick-action-btn {
      justify-content: flex-start;
      padding: 12px 16px;
      text-align: left;
    }

    .quick-action-btn mat-icon {
      margin-right: 12px;
    }

    .compliance-info-card {
      background: linear-gradient(135deg, #e8f5e8, #f1f8e9);
      border: 1px solid #4caf50;
    }

    .compliance-content {
      display: flex;
      align-items: center;
      gap: 24px;
    }

    .compliance-text {
      flex: 1;
    }

    .compliance-text h3 {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 0 0 16px 0;
      color: #2e7d32;
      font-size: 20px;
    }

    .compliance-text p {
      margin: 0 0 16px 0;
      line-height: 1.6;
      color: #1b5e20;
    }

    .compliance-features {
      margin-top: 16px;
    }

    .compliance-features mat-chip {
      background-color: #4caf50;
      color: white;
      margin: 4px;
    }

    @media (max-width: 768px) {
      .dashboard-container {
        padding: 16px;
      }

      .content-grid {
        grid-template-columns: 1fr;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      .compliance-content {
        flex-direction: column;
        text-align: center;
      }
    }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
    currentUser: UserDto | null = null;
    userRoles = UserRole;

    dashboardCards: DashboardCard[] = [];
    alertas: AlertaDto[] = [];
    registrosRecentes: RegistroVooDto[] = [];

    isLoadingRecords = true;
    pendingSignatures = 0;

    displayedColumns: string[] = ['data', 'aeronave', 'rota', 'status', 'actions'];

    private destroy$ = new Subject<void>();

    constructor(
        private authService: AuthService,
        private registroVooService: RegistroVooService
    ) { }

    ngOnInit(): void {
        this.authService.currentUser$
            .pipe(takeUntil(this.destroy$))
            .subscribe(user => {
                this.currentUser = user;
                if (user) {
                    this.loadDashboardData();
                }
            });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private loadDashboardData(): void {
        this.loadStatistics();
        this.loadRecentRecords();
        this.loadAlerts();
    }

    private loadStatistics(): void {
        // Simular dados - implementar serviços reais
        this.dashboardCards = [
            {
                title: 'Registros (30 dias)',
                value: 24,
                icon: 'flight',
                color: 'primary',
                subtitle: 'Últimos 30 dias',
                route: '/registro-voo'
            },
            {
                title: 'Horas Voadas',
                value: '142.5h',
                icon: 'schedule',
                color: 'accent',
                subtitle: 'Este mês'
            },
            {
                title: 'Pendente Assinatura',
                value: 3,
                icon: 'edit_note',
                color: 'warn',
                subtitle: 'Aguardando ação',
                route: '/assinaturas'
            },
            {
                title: 'Conformidade ANAC',
                value: '100%',
                icon: 'verified',
                color: 'success',
                subtitle: 'Últimos 30 dias'
            }
        ];

        this.pendingSignatures = 3;
    }

    private loadRecentRecords(): void {
        this.isLoadingRecords = true;

        // Carregar últimos 30 dias por padrão
        this.registroVooService.obterUltimos30Dias()
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (registros) => {
                    this.registrosRecentes = registros.slice(0, 10); // Últimos 10
                    this.isLoadingRecords = false;
                },
                error: (error) => {
                    console.error('Erro ao carregar registros:', error);
                    this.isLoadingRecords = false;
                }
            });
    }

    private loadAlerts(): void {
        // Simular alertas - implementar serviço real
        this.alertas = [
            {
                id: '1',
                tipo: 'PrazoAssinatura' as any,
                titulo: 'Prazo de Assinatura Vencendo',
                descricao: 'Registro PP-ABC de 15/10/2025 vence em 2 dias (RBAC 121)',
                prioridade: PrioridadeAlerta.Alta,
                criadoEm: new Date().toISOString(),
                lido: false
            },
            {
                id: '2',
                tipo: 'ManutencaoVencida' as any,
                titulo: 'Manutenção Próxima',
                descricao: 'Aeronave PR-DEF próxima da manutenção (50h restantes)',
                prioridade: PrioridadeAlerta.Media,
                criadoEm: new Date().toISOString(),
                lido: false
            }
        ];
    }

    getRoleDescription(): string {
        const descriptions = {
            [UserRole.Piloto]: 'Piloto',
            [UserRole.Operador]: 'Operador',
            [UserRole.DiretorOperacoes]: 'Diretor de Operações',
            [UserRole.Fiscalizacao]: 'Fiscalização ANAC'
        };
        return this.currentUser?.role ? descriptions[this.currentUser.role] : '';
    }

    getIconColor(cardColor: string): string {
        const colors = {
            primary: '#1976d2',
            accent: '#ff5722',
            warn: '#f57c00',
            success: '#388e3c'
        };
        return colors[cardColor as keyof typeof colors] || '#666';
    }

    getStatusText(status: RegistroStatus): string {
        const statusTexts = {
            [RegistroStatus.Rascunho]: 'Rascunho',
            [RegistroStatus.PendenteAssinaturaPiloto]: 'Pend. Piloto',
            [RegistroStatus.PendenteAssinaturaOperador]: 'Pend. Operador',
            [RegistroStatus.Finalizado]: 'Finalizado',
            [RegistroStatus.Cancelado]: 'Cancelado'
        };
        return statusTexts[status] || status;
    }

    canCreateRecord(): boolean {
        return this.authService.hasAnyRole([UserRole.Piloto, UserRole.Operador]);
    }

    canEditRecord(record: RegistroVooDto): boolean {
        return record.status === RegistroStatus.Rascunho ||
            record.status === RegistroStatus.PendenteAssinaturaPiloto;
    }

    hasRole(roles: UserRole[]): boolean {
        return this.authService.hasAnyRole(roles);
    }
}