import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

// En el servidor (SSR) dejamos pasar sin validar: la sesion vive en
// localStorage, que solo existe en el navegador. La proteccion real
// contra llamadas no autorizadas la sigue haciendo tu API con [Authorize].
// El guard aca es proteccion de navegacion/UX en el cliente.
export const authGuard: CanActivateFn = () => {
  const platformId = inject(PLATFORM_ID);
  if (!isPlatformBrowser(platformId)) return true;

  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.estaAutenticado) return true;
  router.navigate(['/login']);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const platformId = inject(PLATFORM_ID);
  if (!isPlatformBrowser(platformId)) return true;

  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.currentUser()?.rol === 'Administrador') return true;
  router.navigate(['/dashboard']);
  return false;
};
