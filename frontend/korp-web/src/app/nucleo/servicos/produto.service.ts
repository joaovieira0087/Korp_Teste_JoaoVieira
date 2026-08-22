import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AtualizarProduto, CriarProduto, Produto } from '../modelos/produto';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiEstoque}/produtos`;

  listar(filtro?: string): Observable<Produto[]> {
    let parametros = new HttpParams();

    if (filtro?.trim()) {
      parametros = parametros.set('filtro', filtro.trim());
    }

    return this.http.get<Produto[]>(this.url, { params: parametros });
  }

  obterPorId(id: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.url}/${id}`);
  }

  criar(produto: CriarProduto): Observable<Produto> {
    return this.http.post<Produto>(this.url, produto);
  }

  atualizar(id: string, produto: AtualizarProduto): Observable<Produto> {
    return this.http.put<Produto>(`${this.url}/${id}`, produto);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
