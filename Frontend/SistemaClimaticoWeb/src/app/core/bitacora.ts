import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BitacoraDto, UsuarioFiltroDto } from './models';

export interface FiltrosBitacora {
  idUsuario?: number | null;
  accion?: string | null;
  desde?: string | null;
  hasta?: string | null;
  limite?: number;
}

// Bitacora de acciones de los usuarios (solo Administrador).
@Injectable({ providedIn: 'root' })
export class BitacoraService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/bitacora`;

  // GET /api/bitacora?idUsuario=&accion=&desde=&hasta=&limite=
  listar(f: FiltrosBitacora = {}) {
    let params = new HttpParams();
    if (f.idUsuario) params = params.set('idUsuario', String(f.idUsuario));
    if (f.accion) params = params.set('accion', f.accion);
    if (f.desde) params = params.set('desde', f.desde);
    if (f.hasta) params = params.set('hasta', f.hasta);
    params = params.set('limite', String(f.limite ?? 100));
    return this.http.get<BitacoraDto[]>(this.baseUrl, { params });
  }

  acciones() {
    return this.http.get<string[]>(`${this.baseUrl}/acciones`);
  }

  usuarios() {
    return this.http.get<UsuarioFiltroDto[]>(`${this.baseUrl}/usuarios`);
  }
}
