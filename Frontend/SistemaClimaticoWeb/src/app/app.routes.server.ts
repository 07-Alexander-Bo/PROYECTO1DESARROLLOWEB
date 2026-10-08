import { RenderMode, ServerRoute } from '@angular/ssr';

// La app esta detras de login y todos sus datos vienen de la API con JWT
// (guardado en localStorage, que no existe en el servidor). Por eso las
// pantallas se renderizan en el navegador: en el servidor no hay sesion
// y la API respondería 401.
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Client
  }
];
