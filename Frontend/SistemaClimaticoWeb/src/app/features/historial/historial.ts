import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HistorialService } from '../../core/historial';
import { HistorialEventoDto } from '../../core/models';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './historial.html',
  styleUrl: './historial.css',
})
export class Historial implements OnInit {
  private srv = inject(HistorialService);

  eventos = signal<HistorialEventoDto[]>([]);
  cargando = signal(true);

  ngOnInit() {
    this.srv.listar().subscribe({
      next: (data) => { this.eventos.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }
}
