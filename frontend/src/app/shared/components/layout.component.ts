import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { Observable, filter, map, shareReplay } from 'rxjs';
import { UserDto, UserRole } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';

interface MenuItem {
    title: string;
    icon: string;
    route: string;
    roles?: UserRole[];
    badge?: number;
}

@Component({
    selector: 'app-layout',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        MatToolbarModule,
        MatSidenavModule,
        MatListModule,
        MatIconModule,
        MatButtonModule,
        MatMenuModule,
        MatBadgeModule,
        MatTooltipModule
    ],
    template: `
    <mat-sidenav-container class="sidenav-container">
      <mat-sidenav #drawer
                   class="sidenav"
                   [mode]="(isHandset$ | async) ? 'over' : 'side'"
                   [opened]="(isHandset$ | async) === false">
        
        <!-- Logo e título -->
        <div class="sidenav-header">
          <img src="/icons/icon-72x72.png" alt="Logo" class="app-logo">
          <h3>Diário de Bordo</h3>
          <p>Digital</p>
        </div>

        <!-- Menu de navegação -->
        <mat-nav-list>
          <ng-container *ngFor="let item of menuItems">
            <a mat-list-item 
               [routerLink]="item.route"
               routerLinkActive="active-route"
               *ngIf="hasAccess(item.roles)"
               [matTooltip]="item.title"
               matTooltipPosition="right">
              <mat-icon matListItemIcon [matBadge]="item.badge || 0" 
                        [matBadgeHidden]="!item.badge"
                        matBadgeColor="warn"
                        matBadgeSize="small">
                {{item.icon}}
              </mat-icon>
              <span matListItemTitle>{{item.title}}</span>
            </a>
          </ng-container>

          <mat-divider></mat-divider>

          <!-- Seção de configurações -->
          <h3 matSubheader>Sistema</h3>
          <a mat-list-item routerLink="/configuracoes" routerLinkActive="active-route">
            <mat-icon matListItemIcon>settings</mat-icon>
            <span matListItemTitle>Configurações</span>
          </a>
          <a mat-list-item routerLink="/ajuda" routerLinkActive="active-route">
            <mat-icon matListItemIcon>help</mat-icon>
            <span matListItemTitle>Ajuda</span>
          </a>
        </mat-nav-list>

        <!-- Informações de conformidade -->
        <div class="sidenav-footer">
          <div class="compliance-info">
            <mat-icon>verified</mat-icon>
            <small>ANAC 457/2017<br>ANAC 458/2017</small>
          </div>
        </div>
      </mat-sidenav>

      <mat-sidenav-content>
        <!-- Toolbar principal -->
        <mat-toolbar color="primary" class="main-toolbar">
          <button type="button"
                  mat-icon-button
                  (click)="drawer.toggle()"
                  *ngIf="isHandset$ | async">
            <mat-icon>menu</mat-icon>
          </button>

          <!-- Título da página atual -->
          <span class="page-title">{{getCurrentPageTitle()}}</span>
          
          <span class="spacer"></span>

          <!-- Botões de ação rápida -->
          <button mat-icon-button 
                  [routerLink]="['/registro-voo/novo']"
                  matTooltip="Novo Registro"
                  *ngIf="canCreateRecord()">
            <mat-icon>add</mat-icon>
          </button>

          <button mat-icon-button 
                  matTooltip="Notificações"
                  [matBadge]="notificationCount"
                  [matBadgeHidden]="notificationCount === 0"
                  matBadgeColor="warn">
            <mat-icon>notifications</mat-icon>
          </button>

          <!-- Menu do usuário -->
          <button mat-icon-button [matMenuTriggerFor]="userMenu">
            <mat-icon>account_circle</mat-icon>
          </button>
          
          <mat-menu #userMenu="matMenu">
            <div class="user-info" mat-menu-item disabled>
              <div>
                <strong>{{currentUser?.nome}}</strong>
                <div class="user-details">
                  <small>{{currentUser?.email}}</small><br>
                  <small>{{getRoleDescription(currentUser?.role)}}</small><br>
                  <small>ANAC: {{currentUser?.codigoAnac}}</small>
                </div>
              </div>
            </div>
            
            <mat-divider></mat-divider>
            
            <button mat-menu-item routerLink="/perfil">
              <mat-icon>person</mat-icon>
              <span>Meu Perfil</span>
            </button>
            
            <button mat-menu-item (click)="toggleDarkMode()">
              <mat-icon>{{isDarkMode ? 'light_mode' : 'dark_mode'}}</mat-icon>
              <span>{{isDarkMode ? 'Modo Claro' : 'Modo Escuro'}}</span>
            </button>
            
            <mat-divider></mat-divider>
            
            <button mat-menu-item (click)="logout()">
              <mat-icon>logout</mat-icon>
              <span>Sair</span>
            </button>
          </mat-menu>
        </mat-toolbar>

        <!-- Conteúdo da página -->
        <main class="main-content">
          <router-outlet></router-outlet>
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
    styles: [`
    .sidenav-container {
      height: 100vh;
    }

    .sidenav {
      width: 280px;
      background-color: #fafafa;
    }

    .sidenav-header {
      padding: 24px 16px;
      text-align: center;
      background: linear-gradient(135deg, #1976d2, #42a5f5);
      color: white;
    }

    .app-logo {
      width: 48px;
      height: 48px;
      margin-bottom: 8px;
    }

    .sidenav-header h3 {
      margin: 0;
      font-size: 18px;
      font-weight: 500;
    }

    .sidenav-header p {
      margin: 0;
      font-size: 14px;
      opacity: 0.9;
    }

    .sidenav-footer {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      padding: 16px;
    }

    .compliance-info {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px;
      background-color: #e8f5e8;
      border-radius: 4px;
      color: #2e7d32;
    }

    .compliance-info mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .compliance-info small {
      font-size: 10px;
      line-height: 1.2;
    }

    .main-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
    }

    .page-title {
      font-size: 18px;
      font-weight: 500;
    }

    .spacer {
      flex: 1 1 auto;
    }

    .main-content {
      padding: 24px;
      min-height: calc(100vh - 64px);
      background-color: #f5f5f5;
    }

    .user-info {
      max-width: 250px;
      cursor: default !important;
    }

    .user-details {
      margin-top: 4px;
      opacity: 0.7;
    }

    .active-route {
      background-color: rgba(25, 118, 210, 0.1) !important;
      color: #1976d2 !important;
    }

    .active-route mat-icon {
      color: #1976d2 !important;
    }

    @media (max-width: 768px) {
      .main-content {
        padding: 16px;
      }
      
      .page-title {
        font-size: 16px;
      }
    }
  `]
})
export class LayoutComponent implements OnInit {
    currentUser: UserDto | null = null;
    notificationCount = 0;
    isDarkMode = false;
    isHandset$: Observable<boolean>;

    menuItems: MenuItem[] = [
        {
            title: 'Dashboard',
            icon: 'dashboard',
            route: '/dashboard'
        },
        {
            title: 'Registros de Voo',
            icon: 'flight',
            route: '/registro-voo',
            roles: [UserRole.Piloto, UserRole.Operador, UserRole.DiretorOperacoes]
        },
        {
            title: 'Assinaturas Pendentes',
            icon: 'edit_note',
            route: '/assinaturas',
            roles: [UserRole.Piloto, UserRole.Operador],
            badge: 3
        },
        {
            title: 'Aeronaves',
            icon: 'airplanemode_active',
            route: '/aeronaves',
            roles: [UserRole.Operador, UserRole.DiretorOperacoes]
        },
        {
            title: 'Tripulação',
            icon: 'group',
            route: '/tripulacao',
            roles: [UserRole.Operador, UserRole.DiretorOperacoes]
        },
        {
            title: 'Relatórios ANAC',
            icon: 'assessment',
            route: '/relatorios',
            roles: [UserRole.Operador, UserRole.DiretorOperacoes, UserRole.Fiscalizacao]
        },
        {
            title: 'Auditoria',
            icon: 'security',
            route: '/auditoria',
            roles: [UserRole.DiretorOperacoes, UserRole.Fiscalizacao]
        }
    ];

    private pageTitle = 'Dashboard';

    constructor(
        private breakpointObserver: BreakpointObserver,
        private authService: AuthService,
        private router: Router
    ) {
        this.isHandset$ = this.breakpointObserver.observe(Breakpoints.Handset)
            .pipe(
                map(result => result.matches),
                shareReplay()
            );
    }

    ngOnInit(): void {
        this.authService.currentUser$.subscribe(user => {
            this.currentUser = user;
        });

        // Escutar mudanças de rota para atualizar título
        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.updatePageTitle();
        });

        // Carregar tema salvo
        const savedTheme = localStorage.getItem('darkMode');
        this.isDarkMode = savedTheme === 'true';
        this.applyTheme();
    }

    hasAccess(roles?: UserRole[]): boolean {
        if (!roles || roles.length === 0) return true;
        return this.authService.hasAnyRole(roles);
    }

    canCreateRecord(): boolean {
        return this.authService.hasAnyRole([UserRole.Piloto, UserRole.Operador]);
    }

    getCurrentPageTitle(): string {
        return this.pageTitle;
    }

    private updatePageTitle(): void {
        const url = this.router.url;
        const menuItem = this.menuItems.find(item => url.includes(item.route));
        this.pageTitle = menuItem?.title || 'Sistema';
    }

    getRoleDescription(role?: UserRole): string {
        const descriptions = {
            [UserRole.Piloto]: 'Piloto',
            [UserRole.Operador]: 'Operador',
            [UserRole.DiretorOperacoes]: 'Diretor de Operações',
            [UserRole.Fiscalizacao]: 'Fiscalização ANAC'
        };
        return role ? descriptions[role] : '';
    }

    toggleDarkMode(): void {
        this.isDarkMode = !this.isDarkMode;
        localStorage.setItem('darkMode', this.isDarkMode.toString());
        this.applyTheme();
    }

    private applyTheme(): void {
        const body = document.body;
        if (this.isDarkMode) {
            body.classList.add('dark-theme');
        } else {
            body.classList.remove('dark-theme');
        }
    }

    logout(): void {
        this.authService.logout().subscribe(() => {
            this.router.navigate(['/login']);
        });
    }
}