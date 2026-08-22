export interface Produto {
  id: string;
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface CriarProduto {
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface AtualizarProduto {
  descricao: string;
  saldo: number;
}
