import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UsuarioDto } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  private baseUrl = `${environment.apiUrl}/auth`;

  // localStorage no existe durante el render en servidor (SSR/hydration),
  // por eso todo acceso queda protegido con this.isBrowser.
  currentUser = signal<UsuarioDto | null>(this.isBrowser ? this.leerUsuarioGuardado() : null);
  token = signal<string | null>(this.isBrowser ? localStorage.getItem('sc_token') : null);

  get estaAutenticado(): boolean {
    return !!this.token();
  }

  login(correo: string, password: string) {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { correo, password })
      .pipe(tap((res) => this.guardarSesion(res)));
  }

  registro(data: { nombreCompleto: string; correo: string; password: string; rol: string }) {
    return this.http.post<UsuarioDto>(`${this.baseUrl}/registro`, data);
  }

  logout() {
    if (this.isBrowser) {
      localStorage.removeItem('sc_token');
      localStorage.removeItem('sc_usuario');
    }
    this.token.set(null);
    this.currentUser.set(null);
  }

  private guardarSesion(res: LoginResponse) {
    if (this.isBrowser) {
      localStorage.setItem('sc_token', res.token);
      localStorage.setItem('sc_usuario', JSON.stringify(res.usuario));
    }
    this.token.set(res.token);
    this.currentUser.set(res.usuario);
  }

  private leerUsuarioGuardado(): UsuarioDto | null {
    const raw = localStorage.getItem('sc_usuario');
    return raw ? JSON.parse(raw) : null;
  }
}
