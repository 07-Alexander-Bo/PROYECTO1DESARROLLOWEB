import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth';

// Interceptor funcional (estilo Angular 20+):
//  1) agrega el JWT a cada request.
//  2) si la API responde 401 (token vencido o invalido), cierra la sesion
//     y manda al usuario al login. El 401 del propio login no cuenta: ahi
//     significa "credenciales incorrectas" y el formulario muestra el error.
//
// Protegido para SSR: en el servidor no hay localStorage ni sesion, asi que
// ahi la request sale sin token y sin manejo de 401.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  if (!isPlatformBrowser(platformId)) {
    return next(req);
  }

  const auth = inject(AuthService);
  const router = inject(Router);

  const token = localStorage.getItem('sc_token');
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  const esLogin = req.url.includes('/auth/login');

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !esLogin) {
        auth.logout();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    }),
  );
};
