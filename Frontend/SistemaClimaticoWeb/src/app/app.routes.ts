import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/auth-guard';
import { Shell } from './features/shell/shell';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login').then((m) => m.Login) },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'sensores', loadComponent: () => import('./features/sensores/sensores').then((m) => m.Sensores) },
      { path: 'alertas', loadComponent: () => import('./features/alertas/alertas').then((m) => m.Alertas) },
      { path: 'historial', loadComponent: () => import('./features/historial/historial').then((m) => m.Historial) },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin').then((m) => m.Admin),
      },
      {
        // Bitacora de acciones: solo Administrador (la API tambien lo exige).
        path: 'bitacora',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/bitacora/bitacora').then((m) => m.Bitacora),
      },
      {
        // Crear usuarios: solo Administrador (la API tambien lo exige).
        path: 'registro',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/registro/registro').then((m) => m.Registro),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
