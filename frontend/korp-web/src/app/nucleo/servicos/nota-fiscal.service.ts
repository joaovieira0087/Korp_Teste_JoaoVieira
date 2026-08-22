import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ItemRequisicao, NotaFiscal, StatusNotaFiscal } from '../modelos/nota-fiscal';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiFaturamento}/notas-fiscais`;

  listar(status?: StatusNotaFiscal): Observable<NotaFiscal[]> {
    let parametros = new HttpParams();

    if (status) {
      parametros = parametros.set('status', status);
    }

    return this.http.get<NotaFiscal[]>(this.url, { params: parametros });
  }

  obterPorId(id: string): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.url}/${id}`);
  }

  criar(itens: ItemRequisicao[]): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.url, { itens });
  }

  adicionarItem(id: string, item: ItemRequisicao): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.url}/${id}/itens`, item);
  }

  removerItem(id: string, produtoId: string): Observable<NotaFiscal> {
    return this.http.delete<NotaFiscal>(`${this.url}/${id}/itens/${produtoId}`);
  }

  imprimir(id: string): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.url}/${id}/imprimir`, {});
  }
}
