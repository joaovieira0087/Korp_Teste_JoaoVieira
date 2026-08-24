export type OrigemTexto = 'IA' | 'Fallback';

export interface SugestaoDescricao {
  descricao: string;
  origem: OrigemTexto;
}

export interface TextoGerado {
  texto: string;
  origem: OrigemTexto;
}

export interface MetricasFaturamento {
  totalNotas: number;
  abertas: number;
  fechadas: number;
  unidadesFaturadas: number;
  unidadesPendentes: number;
  produtoMaisMovimentado: string | null;
  quantidadeDoProdutoTop: number;
}

export interface Analise {
  metricas: MetricasFaturamento;
  analise: TextoGerado;
}
