import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AlertaDto, EstadoSensorDto } from './models';

@Injectable({ providedIn: 'root' })
export class AlertasService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/alertas`;

  // GET /api/alertas?nivel=&tipoFenomeno=&atendida=
  listar(filtros?: { nivel?: string; tipoFenomeno?: string; atendida?: boolean }) {
    let params = new HttpParams();
    if (filtros?.nivel) params = params.set('nivel', filtros.nivel);
    if (filtros?.tipoFenomeno) params = params.set('tipoFenomeno', filtros.tipoFenomeno);
    if (filtros?.atendida !== undefined) params = params.set('atendida', String(filtros.atendida));
    return this.http.get<AlertaDto[]>(this.baseUrl, { params });
  }

  // GET /api/alertas/{id}
  obtener(id: number) {
    return this.http.get<AlertaDto>(`${this.baseUrl}/${id}`);
  }

  // PATCH /api/alertas/{id}/atender
  atender(id: number) {
    return this.http.patch<{ mensaje: string }>(`${this.baseUrl}/${id}/atender`, {});
  }

  // GET /api/alertas/estado-actual  (semaforo en vivo para el dashboard)
  estadoActual() {
    return this.http.get<EstadoSensorDto[]>(`${this.baseUrl}/estado-actual`);
  }
}
