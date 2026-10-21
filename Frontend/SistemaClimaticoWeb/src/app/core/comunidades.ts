import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ComunidadDto, GuardarComunidadDto } from './models';

@Injectable({ providedIn: 'root' })
export class ComunidadesService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/comunidades`;

  // GET /api/comunidades
  listar() {
    return this.http.get<ComunidadDto[]>(this.baseUrl);
  }

  // POST /api/comunidades (Administrador)
  crear(dto: GuardarComunidadDto) {
    return this.http.post<ComunidadDto>(this.baseUrl, dto);
  }

  // PUT /api/comunidades/{id} (Administrador)
  actualizar(id: number, dto: GuardarComunidadDto) {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  // DELETE /api/comunidades/{id} (Administrador; solo si no tiene sensores)
  eliminar(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
