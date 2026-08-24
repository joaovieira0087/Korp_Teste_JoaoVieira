import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subject, finalize, takeUntil } from 'rxjs';

import { SeloOrigem } from '../../../nucleo/componentes/selo-origem/selo-origem';
import { Analise } from '../../../nucleo/modelos/ia';
import { NotaFiscal, StatusNotaFiscal } from '../../../nucleo/modelos/nota-fiscal';
import { NotaFiscalService } from '../../../nucleo/servicos/nota-fiscal.service';
import { NotificacaoService } from '../../../nucleo/servicos/notificacao.service';

@Component({
  selector: 'app-nota-lista',
  standalone: true,
  imports: [
    DatePipe, MatTableModule, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatButtonToggleModule, MatProgressSpinnerModule,
    MatCardModule, SeloOrigem
  ],
  templateUrl: './nota-lista.html',
  styleUrl: './nota-lista.scss'
})
export class NotaLista implements OnInit, OnDestroy {
  private readonly notaService = inject(NotaFiscalService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly router = inject(Router);
  private readonly destruido$ = new Subject<void>();

  readonly colunas = ['numero', 'status', 'itens', 'quantidade', 'criadaEm', 'acoes'];
  readonly notas = signal<NotaFiscal[]>([]);
  readonly carregando = signal(false);
  readonly criando = signal(false);

  readonly analise = signal<Analise | null>(null);
  readonly analisando = signal(false);

  filtro: StatusNotaFiscal | '' = '';

  ngOnInit(): void {
    this.carregar();
  }

  ngOnDestroy(): void {
    this.destruido$.next();
    this.destruido$.complete();
  }

  aoTrocarFiltro(valor: StatusNotaFiscal | ''): void {
    this.filtro = valor;
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);

    this.notaService.listar(this.filtro || undefined)
      .pipe(finalize(() => this.carregando.set(false)), takeUntil(this.destruido$))
      .subscribe(notas => this.notas.set(notas));
  }

  criarNota(): void {
    this.criando.set(true);

    this.notaService.criar([])
      .pipe(finalize(() => this.criando.set(false)), takeUntil(this.destruido$))
      .subscribe(nota => {
        this.notificacao.sucesso(`Nota ${nota.numero} criada.`);
        this.router.navigate(['/notas', nota.id]);
      });
  }

  gerarAnalise(): void {
    this.analisando.set(true);
    this.notaService.analisar()
      .pipe(finalize(() => this.analisando.set(false)), takeUntil(this.destruido$))
      .subscribe(resposta => this.analise.set(resposta));
  }

  abrir(nota: NotaFiscal): void {
    this.router.navigate(['/notas', nota.id]);
  }
}
