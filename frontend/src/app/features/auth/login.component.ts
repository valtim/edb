import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { LoginDto } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatCheckboxModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatSnackBarModule
    ],
    template: `
    <div class="login-container">
      <div class="login-wrapper">
        <mat-card class="login-card">
          <mat-card-header>
            <div class="login-header">
              <img src="/icons/icon-192x192.png" alt="Logo" class="logo">
              <h1>Diário de Bordo Digital</h1>
              <p>Sistema de Aviação Civil - ANAC</p>
            </div>
          </mat-card-header>

          <mat-card-content>
            <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>E-mail</mat-label>
                <input matInput 
                       type="email" 
                       formControlName="email"
                       placeholder="seu@email.com"
                       autocomplete="email">
                <mat-icon matSuffix>email</mat-icon>
                <mat-error *ngIf="loginForm.get('email')?.hasError('required')">
                  E-mail é obrigatório
                </mat-error>
                <mat-error *ngIf="loginForm.get('email')?.hasError('email')">
                  Digite um e-mail válido
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Senha</mat-label>
                <input matInput 
                       [type]="hidePassword ? 'password' : 'text'" 
                       formControlName="password"
                       autocomplete="current-password">
                <button mat-icon-button 
                        matSuffix 
                        type="button"
                        (click)="hidePassword = !hidePassword">
                  <mat-icon>{{hidePassword ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-error *ngIf="loginForm.get('password')?.hasError('required')">
                  Senha é obrigatória
                </mat-error>
                <mat-error *ngIf="loginForm.get('password')?.hasError('minlength')">
                  Senha deve ter pelo menos 6 caracteres
                </mat-error>
              </mat-form-field>

              <!-- Campo 2FA se necessário -->
              <mat-form-field appearance="outline" 
                             class="full-width" 
                             *ngIf="showTwoFactorField">
                <mat-label>Código 2FA</mat-label>
                <input matInput 
                       type="text" 
                       formControlName="twoFactorCode"
                       placeholder="000000"
                       maxlength="6">
                <mat-icon matSuffix>security</mat-icon>
                <mat-error *ngIf="loginForm.get('twoFactorCode')?.hasError('required')">
                  Código 2FA é obrigatório
                </mat-error>
                <mat-error *ngIf="loginForm.get('twoFactorCode')?.hasError('pattern')">
                  Digite apenas números (6 dígitos)
                </mat-error>
              </mat-form-field>

              <div class="form-options">
                <mat-checkbox formControlName="rememberMe">
                  Lembrar-me
                </mat-checkbox>
                
                <button type="button" 
                        mat-button 
                        color="primary" 
                        class="forgot-password"
                        (click)="forgotPassword()">
                  Esqueci minha senha
                </button>
              </div>

              <button mat-raised-button 
                      color="primary" 
                      type="submit"
                      class="login-button full-width"
                      [disabled]="loginForm.invalid || isLoading">
                <mat-spinner diameter="20" *ngIf="isLoading"></mat-spinner>
                <span *ngIf="!isLoading">{{showTwoFactorField ? 'Verificar' : 'Entrar'}}</span>
              </button>
            </form>
          </mat-card-content>

          <mat-card-footer>
            <div class="footer-info">
              <p>
                <mat-icon>shield</mat-icon>
                Conforme Resoluções ANAC 457/2017 e 458/2017
              </p>
              <p class="version">v1.0.0</p>
            </div>
          </mat-card-footer>
        </mat-card>
      </div>
    </div>
  `,
    styles: [`
    .login-container {
      min-height: 100vh;
      background: linear-gradient(135deg, #1976d2 0%, #42a5f5 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px;
    }

    .login-wrapper {
      width: 100%;
      max-width: 400px;
    }

    .login-card {
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
      border-radius: 16px;
      overflow: hidden;
    }

    .login-header {
      text-align: center;
      padding: 20px 0;
    }

    .logo {
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
    }

    .login-header h1 {
      margin: 0 0 8px 0;
      color: #1976d2;
      font-size: 24px;
      font-weight: 500;
    }

    .login-header p {
      margin: 0;
      color: #666;
      font-size: 14px;
    }

    .login-form {
      padding: 20px 0;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .form-options {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin: 16px 0 24px 0;
    }

    .forgot-password {
      min-width: auto;
      padding: 0;
      font-size: 14px;
    }

    .login-button {
      height: 48px;
      font-size: 16px;
      border-radius: 8px;
    }

    .footer-info {
      text-align: center;
      padding: 16px;
      background-color: #f5f5f5;
      color: #666;
      font-size: 12px;
    }

    .footer-info p {
      margin: 4px 0;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }

    .footer-info mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .version {
      font-weight: 500;
    }

    mat-spinner {
      margin-right: 8px;
    }

    @media (max-width: 480px) {
      .login-container {
        padding: 16px;
      }
      
      .login-card {
        margin: 0;
      }
    }
  `]
})
export class LoginComponent implements OnInit {
    loginForm: FormGroup;
    hidePassword = true;
    isLoading = false;
    showTwoFactorField = false;

    constructor(
        private fb: FormBuilder,
        private authService: AuthService,
        private router: Router,
        private snackBar: MatSnackBar
    ) {
        this.loginForm = this.createForm();
    }

    ngOnInit(): void {
        // Limpar dados de autenticação ao carregar a página de login
        this.authService.logout().subscribe();
    }

    private createForm(): FormGroup {
        return this.fb.group({
            email: ['', [Validators.required, Validators.email]],
            password: ['', [Validators.required, Validators.minLength(6)]],
            twoFactorCode: [''],
            rememberMe: [false]
        });
    }

    onSubmit(): void {
        if (this.loginForm.valid) {
            this.isLoading = true;

            const loginData: LoginDto = {
                email: this.loginForm.value.email,
                password: this.loginForm.value.password,
                rememberMe: this.loginForm.value.rememberMe,
                twoFactorCode: this.loginForm.value.twoFactorCode || undefined
            };

            this.authService.login(loginData).subscribe({
                next: (response) => {
                    this.isLoading = false;

                    if (response.requiresTwoFactor && !loginData.twoFactorCode) {
                        this.showTwoFactorField = true;
                        this.loginForm.get('twoFactorCode')?.setValidators([
                            Validators.required,
                            Validators.pattern(/^\d{6}$/)
                        ]);
                        this.loginForm.get('twoFactorCode')?.updateValueAndValidity();

                        this.snackBar.open(
                            'Digite o código do seu aplicativo autenticador',
                            'Fechar',
                            { duration: 5000 }
                        );
                    } else {
                        this.router.navigate(['/dashboard']);
                        this.snackBar.open(
                            `Bem-vindo, ${response.user.nome}!`,
                            'Fechar',
                            { duration: 3000 }
                        );
                    }
                },
                error: (error) => {
                    this.isLoading = false;
                    this.snackBar.open(
                        error.message || 'Erro ao fazer login. Verifique suas credenciais.',
                        'Fechar',
                        { duration: 5000 }
                    );
                }
            });
        }
    }

    forgotPassword(): void {
        this.snackBar.open(
            'Entre em contato com o administrador do sistema',
            'Fechar',
            { duration: 5000 }
        );
    }
}