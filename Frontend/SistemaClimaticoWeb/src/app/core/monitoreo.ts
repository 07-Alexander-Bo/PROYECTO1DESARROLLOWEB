import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface ReinicioResultado {
  mensaje: string;
  lecturasEliminadas: number;
  alertasEliminadas: number;
  eventosEliminados: number;
}

// Acciones sobre todo el sistema de monitoreo (solo Administrador).
@Injectable({ providedIn: 'root' })
export class MonitoreoService {
  private http = inject(HttpClient);

  // POST /api/monitoreo/reiniciar: borra lecturas, alertas e historial.
  reiniciar() {
    return this.http.post<ReinicioResultado>(`${environment.apiUrl}/monitoreo/reiniciar`, {});
  }
}
