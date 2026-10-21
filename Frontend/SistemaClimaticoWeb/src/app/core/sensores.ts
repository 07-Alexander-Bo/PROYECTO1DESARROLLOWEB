import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
  SensorDto, CrearSensorDto, ActualizarSensorDto, CrearLecturaDto, LecturaDto, SerieSensorDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class SensoresService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/sensores`;

  // GET /api/sensores?incluirInactivos=
  listar(incluirInactivos = false) {
    const params = new HttpParams().set('incluirInactivos', String(incluirInactivos));
    return this.http.get<SensorDto[]>(this.baseUrl, { params });
  }

  // GET /api/sensores/{id}
  obtener(id: number) {
    return this.http.get<SensorDto>(`${this.baseUrl}/${id}`);
  }

  // POST /api/sensores
  crear(dto: CrearSensorDto) {
    return this.http.post<SensorDto>(this.baseUrl, dto);
  }

  // PUT /api/sensores/{id}
  actualizar(id: number, dto: ActualizarSensorDto) {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  // PATCH /api/sensores/{id}/activar
  activar(id: number) {
    return this.http.patch<{ mensaje: string }>(`${this.baseUrl}/${id}/activar`, {});
  }

  // PATCH /api/sensores/{id}/desactivar
  desactivar(id: number) {
    return this.http.patch<{ mensaje: string }>(`${this.baseUrl}/${id}/desactivar`, {});
  }

  // GET /api/sensores/{id}/lecturas?ultimas=
  lecturas(id: number, ultimas = 20) {
    const params = new HttpParams().set('ultimas', String(ultimas));
    return this.http.get<LecturaDto[]>(`${this.baseUrl}/${id}/lecturas`, { params });
  }

  // POST /api/sensores/{id}/lecturas
  registrarLectura(id: number, dto: CrearLecturaDto) {
    return this.http.post<LecturaDto>(`${this.baseUrl}/${id}/lecturas`, dto);
  }

  // GET /api/lecturas/series?puntos=&idComunidad=  (para los graficos del dashboard)
  series(puntos = 60, idComunidad?: number | null) {
    let params = new HttpParams().set('puntos', String(puntos));
    if (idComunidad) params = params.set('idComunidad', String(idComunidad));
    return this.http.get<SerieSensorDto[]>(`${environment.apiUrl}/lecturas/series`, { params });
  }
}
