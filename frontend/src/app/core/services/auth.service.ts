import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError, timer } from 'rxjs';
import { catchError, map, retry, switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
    ApiResponse,
    LoginDto,
    LoginResponseDto,
    UserDto,
    UserRole
} from '../models';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly TOKEN_KEY = 'diario_bordo_token';
    private readonly REFRESH_TOKEN_KEY = 'diario_bordo_refresh_token';
    private readonly USER_KEY = 'diario_bordo_user';

    private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
    private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

    public readonly currentUser$ = this.currentUserSubject.asObservable();
    public readonly isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

    constructor(private http: HttpClient) {
        this.initializeAuthState();
        this.startTokenRefreshTimer();
    }

    private initializeAuthState(): void {
        const token = this.getToken();
        const user = this.getStoredUser();

        if (token && user && !this.isTokenExpired(token)) {
            this.currentUserSubject.next(user);
            this.isAuthenticatedSubject.next(true);
        } else {
            this.clearAuth();
        }
    }

    login(credentials: LoginDto): Observable<LoginResponseDto> {
        return this.http.post<ApiResponse<LoginResponseDto>>(
            `${environment.apiUrl}/auth/login`,
            credentials
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    this.setAuthData(response.data);
                    return response.data;
                }
                throw new Error(response.message || 'Erro no login');
            }),
            catchError(error => {
                console.error('Erro no login:', error);
                return throwError(() => error);
            })
        );
    }

    logout(): Observable<void> {
        return this.http.post<ApiResponse<void>>(
            `${environment.apiUrl}/auth/logout`,
            {}
        ).pipe(
            tap(() => this.clearAuth()),
            map(() => void 0),
            catchError(() => {
                // Mesmo se falhar na API, limpar dados locais
                this.clearAuth();
                return throwError(() => new Error('Erro ao fazer logout'));
            })
        );
    }

    refreshToken(): Observable<LoginResponseDto> {
        const refreshToken = this.getRefreshToken();

        if (!refreshToken) {
            this.clearAuth();
            return throwError(() => new Error('Refresh token não encontrado'));
        }

        return this.http.post<ApiResponse<LoginResponseDto>>(
            `${environment.apiUrl}/auth/refresh`,
            { refreshToken }
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    this.setAuthData(response.data);
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao renovar token');
            }),
            catchError(error => {
                console.error('Erro ao renovar token:', error);
                this.clearAuth();
                return throwError(() => error);
            })
        );
    }

    setupTwoFactor(): Observable<{ qrCode: string; setupKey: string }> {
        return this.http.post<ApiResponse<{ qrCode: string; setupKey: string }>>(
            `${environment.apiUrl}/auth/setup-2fa`,
            {}
        ).pipe(
            map(response => {
                if (response.success && response.data) {
                    return response.data;
                }
                throw new Error(response.message || 'Erro ao configurar 2FA');
            })
        );
    }

    verifyTwoFactor(code: string): Observable<void> {
        return this.http.post<ApiResponse<void>>(
            `${environment.apiUrl}/auth/verify-2fa`,
            { code }
        ).pipe(
            map(response => {
                if (response.success) {
                    return void 0;
                }
                throw new Error(response.message || 'Código 2FA inválido');
            })
        );
    }

    changePassword(currentPassword: string, newPassword: string): Observable<void> {
        return this.http.post<ApiResponse<void>>(
            `${environment.apiUrl}/auth/change-password`,
            { currentPassword, newPassword }
        ).pipe(
            map(response => {
                if (response.success) {
                    return void 0;
                }
                throw new Error(response.message || 'Erro ao alterar senha');
            })
        );
    }

    private setAuthData(authData: LoginResponseDto): void {
        localStorage.setItem(this.TOKEN_KEY, authData.token);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, authData.refreshToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(authData.user));

        this.currentUserSubject.next(authData.user);
        this.isAuthenticatedSubject.next(true);
    }

    private clearAuth(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.REFRESH_TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);

        this.currentUserSubject.next(null);
        this.isAuthenticatedSubject.next(false);
    }

    private getStoredUser(): UserDto | null {
        const userJson = localStorage.getItem(this.USER_KEY);
        return userJson ? JSON.parse(userJson) : null;
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    getRefreshToken(): string | null {
        return localStorage.getItem(this.REFRESH_TOKEN_KEY);
    }

    getCurrentUser(): UserDto | null {
        return this.currentUserSubject.value;
    }

    isAuthenticated(): boolean {
        return this.isAuthenticatedSubject.value;
    }

    hasRole(role: UserRole): boolean {
        const user = this.getCurrentUser();
        return user?.role === role;
    }

    hasAnyRole(roles: UserRole[]): boolean {
        const user = this.getCurrentUser();
        return user ? roles.includes(user.role) : false;
    }

    private isTokenExpired(token: string): boolean {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const now = Math.floor(Date.now() / 1000);
            return payload.exp < now;
        } catch {
            return true;
        }
    }

    private startTokenRefreshTimer(): void {
        // Verificar token a cada 5 minutos
        timer(0, 5 * 60 * 1000).pipe(
            switchMap(() => {
                const token = this.getToken();
                if (token && this.isTokenExpired(token)) {
                    return this.refreshToken();
                }
                return [];
            }),
            retry(3)
        ).subscribe({
            error: (error) => {
                console.error('Erro ao renovar token automaticamente:', error);
                this.clearAuth();
            }
        });
    }
}