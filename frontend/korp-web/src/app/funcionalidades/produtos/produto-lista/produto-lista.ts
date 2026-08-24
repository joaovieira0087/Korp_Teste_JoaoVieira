import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';

import { Produto } from '../../../nucleo/modelos/produto';
import { NotificacaoService } from '../../../nucleo/servicos/notificacao.service';
import { ProdutoService } from '../../../nucleo/servicos/produto.service';
import { ConfirmacaoDialogComponent } from '../../../compartilhado/confirmacao-dialog.component';
import { ProdutoFormulario } from '../produto-formulario/produto-formulario';

@Component({
  selector: 'app-produto-lista',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressBarModule, MatDialogModule
  ],
  templateUrl: './produto-lista.html',
  styleUrl: './produto-lista.scss'
})
export class ProdutoLista implements OnInit, OnDestroy {
  private readonly produtoService = inject(ProdutoService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly dialogo = inject(MatDialog);

  /** Emite quando o componente é destruído, para encerrar as inscrições. */
  private readonly destruido$ = new Subject<void>();

  readonly colunas = ['codigo', 'descricao', 'saldo', 'acoes'];
  readonly produtos = signal<Produto[]>([]);
  readonly carregando = signal(false);

  readonly filtro = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    // Busca reativa: reage ao que o usuário digita, sem botão de pesquisar.
    this.filtro.valueChanges
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        switchMap(termo => {
          this.carregando.set(true);
          return this.produtoService.listar(termo);
        }),
        takeUntil(this.destruido$)
      )
      .subscribe({
        next: produtos => {
          this.produtos.set(produtos);
          this.carregando.set(false);
        },
        error: () => this.carregando.set(false)
      });

    this.carregar();
  }

  ngOnDestroy(): void {
    this.destruido$.next();
    this.destruido$.complete();
  }

  carregar(): void {
    this.carregando.set(true);

    this.produtoService.listar(this.filtro.value)
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: produtos => {
          this.produtos.set(produtos);
          this.carregando.set(false);
        },
        error: () => this.carregando.set(false)
      });
  }

  abrirFormulario(produto?: Produto): void {
    this.dialogo
      .open(ProdutoFormulario, { width: '480px', data: produto ?? null })
      .afterClosed()
      .pipe(takeUntil(this.destruido$))
      .subscribe(salvou => {
        if (salvou) this.carregar();
      });
  }

  excluir(produto: Produto): void {
    this.dialogo
      .open(ConfirmacaoDialogComponent, {
        width: '380px',
        data: { mensagem: `Tem certeza que deseja excluir o produto ${produto.codigo}?` }
      })
      .afterClosed()
      .pipe(takeUntil(this.destruido$))
      .subscribe(confirmado => {
        if (confirmado) {
          this.produtoService.excluir(produto.id)
            .pipe(takeUntil(this.destruido$))
            .subscribe(() => {
              this.notificacao.sucesso(`Produto ${produto.codigo} excluído.`);
              this.carregar();
            });
        }
      });
  }
}
