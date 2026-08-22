export type StatusNotaFiscal = 'Aberta' | 'Fechada';

export interface ItemNotaFiscal {
  produtoId: string;
  codigoProduto: string;
  descricaoProduto: string;
  quantidade: number;
}

export interface NotaFiscal {
  id: string;
  numero: number;
  status: StatusNotaFiscal;
  criadaEm: string;
  fechadaEm: string | null;
  itens: ItemNotaFiscal[];
  quantidadeTotal: number;
}

export interface ItemRequisicao {
  produtoId: string;
  quantidade: number;
}
