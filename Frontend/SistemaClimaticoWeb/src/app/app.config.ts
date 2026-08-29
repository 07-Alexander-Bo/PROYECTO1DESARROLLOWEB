import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(),
    // withFetch(): usa la Fetch API en vez de XHR, necesario para que
    // HttpClient funcione durante el render en servidor (SSR) con Node.
    // withInterceptors([authInterceptor]): agrega el JWT a cada request.
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};
