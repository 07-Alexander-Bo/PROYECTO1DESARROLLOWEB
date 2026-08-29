import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { HistorialEventoDto } from './models';

@Injectable({ providedIn: 'root' })
export class HistorialService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/historial`;

  // GET /api/historial?tipoFenomeno=&desde=&hasta=
  listar(filtros?: { tipoFenomeno?: string; desde?: string; hasta?: string }) {
    let params = new HttpParams();
    if (filtros?.tipoFenomeno) params = params.set('tipoFenomeno', filtros.tipoFenomeno);
    if (filtros?.desde) params = params.set('desde', filtros.desde);
    if (filtros?.hasta) params = params.set('hasta', filtros.hasta);
    return this.http.get<HistorialEventoDto[]>(this.baseUrl, { params });
  }

  // GET /api/historial/{id}
  obtener(id: number) {
    return this.http.get<HistorialEventoDto>(`${this.baseUrl}/${id}`);
  }
}
