import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { AlertasService } from '../../core/alertas';
import { AuthService } from '../../core/auth';
import { TiempoRealService } from '../../core/tiempo-real';
import { AlertaDto, NivelAlerta, nombreFenomeno } from '../../core/models';

@Component({
  selector: 'app-alertas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alertas.html',
  styleUrl: './alertas.css',
})
export class Alertas implements OnInit, OnDestroy {
  private srv = inject(AlertasService);
  private auth = inject(AuthService);
  private tiempoReal = inject(TiempoRealService);
  private subs = new Subscription();

  // 'IncendioForestal' -> 'Incendio forestal', 'Sequia' -> 'Sequía', etc.
  nombreFenomeno = nombreFenomeno;

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

    // Tiempo real: las alertas nuevas aparecen arriba sin recargar la pagina
    // (si coinciden con el filtro de nivel elegido).
    this.subs.add(
      this.tiempoReal.alertaNueva$.subscribe((a) => {
        const filtro = this.filtroNivel();
        if (filtro !== 'Todos' && filtro !== a.nivel) return;
        this.alertas.update((lista) => [a, ...lista.filter((x) => x.idAlerta !== a.idAlerta)]);
      }),
    );

    // Otro usuario marco una alerta como atendida.
    this.subs.add(
      this.tiempoReal.alertaAtendida$.subscribe((id) =>
        this.alertas.update((lista) => lista.map((x) => (x.idAlerta === id ? { ...x, atendida: true } : x))),
      ),
    );

    // Al reconectar se vuelve a pedir la lista, por si se perdio algo.
    this.subs.add(this.tiempoReal.conectado$.subscribe(() => this.cargar(false)));
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  cargar(mostrarCargando = true) {
    if (mostrarCargando) this.cargando.set(true);
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
