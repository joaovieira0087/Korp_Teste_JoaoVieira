import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'produtos', pathMatch: 'full' },
  {
    path: 'produtos',
    loadComponent: () =>
      import('./funcionalidades/produtos/produto-lista/produto-lista')
        .then(m => m.ProdutoLista),
    title: 'Produtos | Korp'
  },
  {
    path: 'notas',
    loadComponent: () =>
      import('./funcionalidades/notas/nota-lista/nota-lista')
        .then(m => m.NotaLista),
    title: 'Notas fiscais | Korp'
  },
  {
    path: 'notas/:id',
    loadComponent: () =>
      import('./funcionalidades/notas/nota-detalhe/nota-detalhe')
        .then(m => m.NotaDetalhe),
    title: 'Nota fiscal | Korp'
  },
  { path: '**', redirectTo: 'produtos' }
];
