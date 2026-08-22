import {
  AfterViewInit, Component, ElementRef, OnDestroy, OnInit,
  ViewChild, computed, inject, signal
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Subject, finalize, takeUntil } from 'rxjs';

import { NotaFiscal } from '../../../nucleo/modelos/nota-fiscal';
import { Produto } from '../../../nucleo/modelos/produto';
import { NotaFiscalService } from '../../../nucleo/servicos/nota-fiscal.service';
import { ProdutoService } from '../../../nucleo/servicos/produto.service';
import { NotificacaoService } from '../../../nucleo/servicos/notificacao.service';

@Component({
  selector: 'app-nota-detalhe',
  standalone: true,
  imports: [
    DatePipe, ReactiveFormsModule, RouterLink, MatCardModule, MatTableModule,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatProgressSpinnerModule
  ],
  templateUrl: './nota-detalhe.html',
  styleUrl: './nota-detalhe.scss'
})
export class NotaDetalhe implements OnInit, AfterViewInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly rota = inject(ActivatedRoute);
  private readonly notaService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly destruido$ = new Subject<void>();

  @ViewChild('seletorProduto') private seletorProduto?: ElementRef<HTMLElement>;

  readonly colunas = ['codigo', 'descricao', 'quantidade', 'acoes'];
  readonly nota = signal<NotaFiscal | null>(null);
  readonly produtos = signal<Produto[]>([]);
  readonly carregando = signal(false);
  readonly adicionando = signal(false);
  readonly imprimindo = signal(false);
  readonly podeEditar = computed(() => this.nota()?.status === 'Aberta');
  readonly podeImprimir = computed(() => {
    const nota = this.nota();
    return nota !== null && nota.status === 'Aberta' && nota.itens.length > 0;
  });

  readonly formularioItem = this.fb.nonNullable.group({
    produtoId: ['', Validators.required],
    quantidade: [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    const id = this.rota.snapshot.paramMap.get('id')!;

    this.carregando.set(true);

    this.notaService.obterPorId(id)
      .pipe(finalize(() => this.carregando.set(false)), takeUntil(this.destruido$))
      .subscribe(nota => this.nota.set(nota));

    this.produtoService.listar()
      .pipe(takeUntil(this.destruido$))
      .subscribe(produtos => this.produtos.set(produtos));
  }

  ngAfterViewInit(): void {
    this.seletorProduto?.nativeElement.focus();
  }

  ngOnDestroy(): void {
    this.destruido$.next();
    this.destruido$.complete();
  }

  adicionarItem(): void {
    const nota = this.nota();
    if (!nota || this.formularioItem.invalid) {
      this.formularioItem.markAllAsTouched();
      return;
    }

    this.adicionando.set(true);

    this.notaService.adicionarItem(nota.id, this.formularioItem.getRawValue())
      .pipe(finalize(() => this.adicionando.set(false)), takeUntil(this.destruido$))
      .subscribe(atualizada => {
        this.nota.set(atualizada);
        this.formularioItem.reset({ produtoId: '', quantidade: 1 });
      });
  }

  removerItem(produtoId: string): void {
    const nota = this.nota();
    if (!nota) return;

    this.notaService.removerItem(nota.id, produtoId)
      .pipe(takeUntil(this.destruido$))
      .subscribe(atualizada => this.nota.set(atualizada));
  }

  imprimir(): void {
    const nota = this.nota();
    if (!nota || nota.status !== 'Aberta') return;

    this.imprimindo.set(true);

    this.notaService.imprimir(nota.id)
      .pipe(finalize(() => this.imprimindo.set(false)), takeUntil(this.destruido$))
      .subscribe(atualizada => {
        this.nota.set(atualizada);
        this.notificacao.sucesso(
          `Nota ${atualizada.numero} impressa. Saldo dos produtos atualizado.`);
      });
  }

  saldoDe(produtoId: string): number | null {
    return this.produtos().find(p => p.id === produtoId)?.saldo ?? null;
  }
}
