import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject, finalize, takeUntil } from 'rxjs';

import { Dashboard as DadosDashboard } from '../../nucleo/modelos/dashboard';
import { DashboardService } from '../../nucleo/servicos/dashboard.service';
import { SeloOrigem } from '../../nucleo/componentes/selo-origem/selo-origem';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink, MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, SeloOrigem
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {
  private readonly dashboardService = inject(DashboardService);
  private readonly destruido$ = new Subject<void>();

  readonly dados = signal<DadosDashboard | null>(null);
  readonly carregando = signal(false);

  ngOnInit(): void {
    this.carregar();
  }

  ngOnDestroy(): void {
    this.destruido$.next();
    this.destruido$.complete();
  }

  carregar(): void {
    this.carregando.set(true);

    this.dashboardService.obter()
      .pipe(finalize(() => this.carregando.set(false)), takeUntil(this.destruido$))
      .subscribe(dados => this.dados.set(dados));
  }
}
