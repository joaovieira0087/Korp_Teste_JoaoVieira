import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { OrigemTexto } from '../../modelos/ia';

@Component({
  selector: 'app-selo-origem',
  standalone: true,
  imports: [MatIconModule, MatTooltipModule],
  template: `
    <span class="selo" [class.selo-fallback]="origem() === 'Fallback'"
          [matTooltip]="dica()">
      <mat-icon class="icone">
        {{ origem() === 'IA' ? 'auto_awesome' : 'calculate' }}
      </mat-icon>
      {{ origem() === 'IA' ? 'Gerado por IA' : 'Gerado localmente' }}
    </span>
  `,
  styles: [`
    .selo {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.15rem 0.55rem;
      border-radius: 12px;
      font-size: 0.75rem;
      font-weight: 600;
      background: #ede7f6;
      color: #5e35b1;
    }
    .selo-fallback { background: #eceff1; color: #546e7a; }
    .icone { font-size: 1rem; width: 1rem; height: 1rem; }
  `]
})
export class SeloOrigem {
  readonly origem = input.required<OrigemTexto>();

  dica(): string {
    return this.origem() === 'IA'
      ? 'Texto redigido por modelo de linguagem a partir dos dados do sistema.'
      : 'A IA está indisponível. Texto montado localmente a partir dos mesmos dados.';
  }
}
