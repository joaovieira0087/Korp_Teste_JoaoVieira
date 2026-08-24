import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA, MatDialogModule, MatDialogRef
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';

import { SeloOrigem } from '../../../nucleo/componentes/selo-origem/selo-origem'; // Ajuste o caminho conforme seu projeto
import { SugestaoDescricao } from '../../../nucleo/modelos/ia';
import { Produto } from '../../../nucleo/modelos/produto';
import { NotificacaoService } from '../../../nucleo/servicos/notificacao.service';
import { ProdutoService } from '../../../nucleo/servicos/produto.service';

@Component({
  selector: 'app-produto-formulario',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    SeloOrigem
  ],
  templateUrl: './produto-formulario.html'
})
export class ProdutoFormulario {
  private readonly fb = inject(FormBuilder);
  private readonly produtoService = inject(ProdutoService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly referencia = inject(MatDialogRef<ProdutoFormulario>);

  readonly produto = inject<Produto | null>(MAT_DIALOG_DATA);
  readonly edicao = this.produto !== null;
  readonly salvando = signal(false);

  readonly sugerindo = signal(false);
  readonly sugestao = signal<SugestaoDescricao | null>(null);

  readonly formulario = this.fb.nonNullable.group({
    codigo: [
      { value: this.produto?.codigo ?? '', disabled: this.edicao },
      [Validators.required, Validators.maxLength(30)]
    ],
    descricao: [
      this.produto?.descricao ?? '',
      [Validators.required, Validators.maxLength(200)]
    ],
    saldo: [
      this.produto?.saldo ?? 0,
      [Validators.required, Validators.min(0)]
    ]
  });

  salvar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.salvando.set(true);
    const valores = this.formulario.getRawValue();

    const operacao = this.edicao
      ? this.produtoService.atualizar(this.produto!.id, {
        descricao: valores.descricao,
        saldo: valores.saldo
      })
      : this.produtoService.criar(valores);

    operacao.subscribe({
      next: () => {
        this.notificacao.sucesso(
          this.edicao ? 'Produto atualizado.' : 'Produto cadastrado.');
        this.referencia.close(true);
      },
      error: () => this.salvando.set(false)
    });
  }

  sugerirDescricao(): void {
    const base = this.formulario.controls.descricao.value?.trim()
      || this.formulario.getRawValue().codigo?.trim();

    if (!base) {
      this.notificacao.erro('Escreva um nome ou código antes de pedir a sugestão.');
      return;
    }

    this.sugerindo.set(true);
    this.sugestao.set(null);

    this.produtoService.sugerirDescricao(base)
      .pipe(finalize(() => this.sugerindo.set(false)))
      .subscribe(resposta => this.sugestao.set(resposta));
  }

  aceitarSugestao(): void {
    const sugerida = this.sugestao();
    if (!sugerida) return;

    this.formulario.controls.descricao.setValue(sugerida.descricao);
    this.formulario.controls.descricao.markAsDirty();
    this.sugestao.set(null);
  }

  descartarSugestao(): void {
    this.sugestao.set(null);
  }

  cancelar(): void {
    this.referencia.close(false);
  }
}
