import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificacaoService } from '../servicos/notificacao.service';
import { ProblemDetails } from '../modelos/problema';

/**
 * Captura toda falha HTTP num único ponto, traduz o ProblemDetails do
 * backend em mensagem para o usuário e repassa o erro adiante.
 */
export const erroInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const notificacao = inject(NotificacaoService);

  return proximo(requisicao).pipe(
    catchError((resposta: HttpErrorResponse) => {
      notificacao.erro(traduzir(resposta));
      return throwError(() => resposta);
    })
  );
};

function traduzir(resposta: HttpErrorResponse): string {
  // status 0 = a requisição nem saiu: rede caiu ou o serviço não subiu.
  if (resposta.status === 0) {
    return 'Não foi possível contatar o servidor. Verifique se os serviços estão no ar.';
  }

  const problema = resposta.error as ProblemDetails | null;

  if (problema?.detail) {
    return problema.detail;
  }

  if (resposta.status === 503) {
    return 'Serviço temporariamente indisponível. Tente novamente em instantes.';
  }

  return `Erro inesperado (${resposta.status}). Tente novamente.`;
}
