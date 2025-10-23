import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

export function authInterceptor(req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
    const authService = inject(AuthService);

    // Adicionar token de autorização se disponível
    const authReq = addTokenHeader(req, authService);

    return next(authReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
                return handle401Error(authReq, next, authService);
            }
            return throwError(() => error);
        })
    );
}

function addTokenHeader(request: HttpRequest<any>, authService: AuthService): HttpRequest<any> {
    const token = authService.getToken();

    if (token && !isAuthUrl(request.url)) {
        return request.clone({
            headers: request.headers.set('Authorization', `Bearer ${token}`)
        });
    }

    return request;
}

function handle401Error(request: HttpRequest<any>, next: HttpHandlerFn, authService: AuthService): Observable<HttpEvent<any>> {
    if (!isRefreshing) {
        isRefreshing = true;
        refreshTokenSubject.next(null);

        return authService.refreshToken().pipe(
            switchMap((tokenData) => {
                isRefreshing = false;
                refreshTokenSubject.next(tokenData.token);

                return next(addTokenHeader(request, authService));
            }),
            catchError((error) => {
                isRefreshing = false;
                // Se falhar o refresh, fazer logout
                authService.logout().subscribe();
                return throwError(() => error);
            })
        );
    } else {
        // Se já está refreshing, aguardar novo token
        return refreshTokenSubject.pipe(
            filter(token => token !== null),
            take(1),
            switchMap(() => next(addTokenHeader(request, authService)))
        );
    }
}

function isAuthUrl(url: string): boolean {
    return url.includes('/auth/login') ||
        url.includes('/auth/refresh') ||
        url.includes('/auth/register');
}