import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AlertasService } from '../../core/alertas';
import { AuthService } from '../../core/auth';
import { AlertaDto, NivelAlerta } from '../../core/models';

@Component({
  selector: 'app-alertas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alertas.html',
  styleUrl: './alertas.css',
})
export class Alertas implements OnInit {
  private srv = inject(AlertasService);
  private auth = inject(AuthService);

  // Marcar alertas como atendidas: solo Administrador u Operador (la API tambien lo exige).
  get puedeAtender(): boolean {
    return this.auth.currentUser()?.rol !== 'Usuario';
  }

  alertas = signal<AlertaDto[]>([]);
  cargando = signal(true);
  filtroNivel = signal<NivelAlerta | 'Todos'>('Todos');
  niveles: (NivelAlerta | 'Todos')[] = ['Todos', 'Verde', 'Amarillo', 'Naranja', 'Rojo'];

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.cargando.set(true);
    const nivel = this.filtroNivel();
    this.srv.listar(nivel === 'Todos' ? undefined : { nivel }).subscribe({
      next: (data) => { this.alertas.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }

  cambiarFiltro(n: NivelAlerta | 'Todos') {
    this.filtroNivel.set(n);
    this.cargar();
  }

  atender(a: AlertaDto) {
    this.srv.atender(a.idAlerta).subscribe({
      next: () => this.alertas.set(this.alertas().map((x) => x.idAlerta === a.idAlerta ? { ...x, atendida: true } : x)),
    });
  }
}
