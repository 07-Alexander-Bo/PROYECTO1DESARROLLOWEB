import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface SimuladorEstadoDto {
  activo: boolean;
  intervaloSegundos: number;
}

// Control del simulador de sensores del backend (solo Administrador puede pausar/reanudar).
// El cambio de estado llega a todos los navegadores por SignalR (SimuladorEstado).
@Injectable({ providedIn: 'root' })
export class SimuladorService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/simulador`;

  estado() {
    return this.http.get<SimuladorEstadoDto>(this.baseUrl);
  }

  pausar() {
    return this.http.post<SimuladorEstadoDto>(`${this.baseUrl}/pausar`, {});
  }

  reanudar() {
    return this.http.post<SimuladorEstadoDto>(`${this.baseUrl}/reanudar`, {});
  }
}
