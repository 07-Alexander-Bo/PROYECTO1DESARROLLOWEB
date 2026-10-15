import { Component, inject, signal, OnDestroy, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/auth';
import { NotificacionesService } from '../../core/notificaciones';
import { TiempoRealService } from '../../core/tiempo-real';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell implements OnDestroy {
  auth = inject(AuthService);
  tiempoReal = inject(TiempoRealService);
  notif = inject(NotificacionesService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  now = signal(new Date());
  // El reloj solo corre en el navegador: en el servidor no tiene sentido
  // y dejaria un setInterval sin limpiar en cada render SSR.
  private timer = isPlatformBrowser(this.platformId)
    ? setInterval(() => this.now.set(new Date()), 1000)
    : undefined;

  constructor() {
    // Mientras el usuario esta dentro de la aplicacion: conexion en tiempo real
    // con la API y avisos (visual + sonoro) de alertas nuevas. Ambos servicios
    // ignoran la llamada en el servidor (SSR).
    this.notif.iniciar();
    this.tiempoReal.iniciar();
  }

  salir() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
    this.tiempoReal.detener();
  }
}
