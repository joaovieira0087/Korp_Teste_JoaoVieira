using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Korp.Faturamento.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class NotasFiscaisInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateSequence<int>(
                name: "seq_nota_fiscal_numero",
                schema: "public");

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fechada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens_nota_fiscal",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nota_fiscal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_produto = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descricao_produto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_nota_fiscal", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_nota_fiscal_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalSchema: "public",
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_itens_nota_fiscal_nota_produto",
                schema: "public",
                table: "itens_nota_fiscal",
                columns: new[] { "nota_fiscal_id", "produto_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_numero",
                schema: "public",
                table: "notas_fiscais",
                column: "numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_nota_fiscal",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notas_fiscais",
                schema: "public");

            migrationBuilder.DropSequence(
                name: "seq_nota_fiscal_numero",
                schema: "public");
        }
    }
}
