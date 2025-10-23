import { Routes } from '@angular/router';
import { AuthGuard, NoAuthGuard, RoleGuard } from './core/guards/auth.guard';
import { UserRole } from './core/models';

export const routes: Routes = [
    // Rota padrão - redirecionar para dashboard se autenticado, senão para login
    {
        path: '',
        redirectTo: '/dashboard',
        pathMatch: 'full'
    },

    // Rotas de autenticação (sem layout)
    {
        path: 'login',
        loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
        canActivate: [NoAuthGuard]
    },

    // Rotas protegidas (com layout)
    {
        path: '',
        loadComponent: () => import('./shared/components/layout.component').then(m => m.LayoutComponent),
        canActivate: [AuthGuard],
        children: [
            // Dashboard
            {
                path: 'dashboard',
                loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
            },

            // Registros de Voo
            {
                path: 'registro-voo',
                children: [
                    {
                        path: '',
                        loadComponent: () => import('./features/registro-voo').then(m => m.RegistroVooListComponent),
                        data: { roles: [UserRole.Piloto, UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: 'novo',
                        loadComponent: () => import('./features/registro-voo/registro-voo-form.component').then(m => m.RegistroVooFormComponent),
                        data: { roles: [UserRole.Piloto, UserRole.Operador] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: ':id',
                        loadComponent: () => import('./features/registro-voo').then(m => m.RegistroVooDetailComponent),
                        data: { roles: [UserRole.Piloto, UserRole.Operador, UserRole.DiretorOperacoes, UserRole.Fiscalizacao] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: ':id/edit',
                        loadComponent: () => import('./features/registro-voo/registro-voo-form.component').then(m => m.RegistroVooFormComponent),
                        data: { roles: [UserRole.Piloto, UserRole.Operador] },
                        canActivate: [RoleGuard]
                    }
                ]
            },

            // Assinaturas Pendentes
            {
                path: 'assinaturas',
                loadComponent: () => import('./features/assinaturas').then(m => m.AssinaturasComponent),
                data: { roles: [UserRole.Piloto, UserRole.Operador] },
                canActivate: [RoleGuard]
            },

            // Aeronaves
            {
                path: 'aeronaves',
                children: [
                    {
                        path: '',
                        loadComponent: () => import('./features/aeronaves').then(m => m.AeronavesListComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: 'nova',
                        loadComponent: () => import('./features/aeronaves').then(m => m.AeronaveFormComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: ':id',
                        loadComponent: () => import('./features/aeronaves').then(m => m.AeronaveDetailComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    }
                ]
            },

            // Tripulação
            {
                path: 'tripulacao',
                children: [
                    {
                        path: '',
                        loadComponent: () => import('./features/tripulacao').then(m => m.TripulacaoListComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: 'novo',
                        loadComponent: () => import('./features/tripulacao').then(m => m.TripulanteFormComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: ':id',
                        loadComponent: () => import('./features/tripulacao').then(m => m.TripulanteDetailComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes] },
                        canActivate: [RoleGuard]
                    }
                ]
            },

            // Relatórios ANAC
            {
                path: 'relatorios',
                children: [
                    {
                        path: '',
                        loadComponent: () => import('./features/relatorios').then(m => m.RelatoriosComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes, UserRole.Fiscalizacao] },
                        canActivate: [RoleGuard]
                    },
                    {
                        path: 'anac',
                        loadComponent: () => import('./features/relatorios').then(m => m.RelatorioAnacComponent),
                        data: { roles: [UserRole.Operador, UserRole.DiretorOperacoes, UserRole.Fiscalizacao] },
                        canActivate: [RoleGuard]
                    }
                ]
            },

            // Auditoria
            {
                path: 'auditoria',
                loadComponent: () => import('./features/auditoria').then(m => m.AuditoriaComponent),
                data: { roles: [UserRole.DiretorOperacoes, UserRole.Fiscalizacao] },
                canActivate: [RoleGuard]
            },

            // Perfil do usuário
            {
                path: 'perfil',
                loadComponent: () => import('./features/perfil').then(m => m.PerfilComponent)
            },

            // Configurações
            {
                path: 'configuracoes',
                loadComponent: () => import('./features/configuracoes').then(m => m.ConfiguracoesComponent)
            },

            // Ajuda
            {
                path: 'ajuda',
                loadComponent: () => import('./features/ajuda').then(m => m.AjudaComponent)
            }
        ]
    },

    // Página de acesso negado
    {
        path: 'access-denied',
        loadComponent: () => import('./shared/components').then(m => m.AccessDeniedComponent)
    },

    // Página não encontrada
    {
        path: '**',
        loadComponent: () => import('./shared/components').then(m => m.NotFoundComponent)
    }
];
