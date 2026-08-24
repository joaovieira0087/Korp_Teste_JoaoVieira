import { MetricasFaturamento, TextoGerado } from './ia';

export interface ResumoEstoque {
  totalProdutos: number;
  saldoTotal: number;
  produtosSemEstoque: number;
}

export interface Dashboard {
  faturamento: MetricasFaturamento;
  estoque: ResumoEstoque | null;
  estoqueDisponivel: boolean;
  resumo: TextoGerado;
}
