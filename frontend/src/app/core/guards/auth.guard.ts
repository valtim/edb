import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { Observable, map, take } from 'rxjs';
import { UserRole } from '../models';
import { AuthService } from '../services/auth.service';

@Injectable({
    providedIn: 'root'
})
export class AuthGuard implements CanActivate {
    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    canActivate(): Observable<boolean> {
        return this.authService.isAuthenticated$.pipe(
            take(1),
            map(isAuthenticated => {
                if (isAuthenticated) {
                    return true;
                } else {
                    this.router.navigate(['/login']);
                    return false;
                }
            })
        );
    }
}

@Injectable({
    providedIn: 'root'
})
export class RoleGuard implements CanActivate {
    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
        const requiredRoles = route.data['roles'] as UserRole[];

        return this.authService.currentUser$.pipe(
            take(1),
            map(user => {
                if (!user) {
                    this.router.navigate(['/login']);
                    return false;
                }

                if (!requiredRoles || requiredRoles.length === 0) {
                    return true;
                }

                const hasRole = requiredRoles.includes(user.role);
                if (!hasRole) {
                    this.router.navigate(['/access-denied']);
                }

                return hasRole;
            })
        );
    }
}

@Injectable({
    providedIn: 'root'
})
export class NoAuthGuard implements CanActivate {
    constructor(
        private authService: AuthService,
        private router: Router
    ) { }

    canActivate(): Observable<boolean> {
        return this.authService.isAuthenticated$.pipe(
            take(1),
            map(isAuthenticated => {
                if (!isAuthenticated) {
                    return true;
                } else {
                    this.router.navigate(['/dashboard']);
                    return false;
                }
            })
        );
    }
}