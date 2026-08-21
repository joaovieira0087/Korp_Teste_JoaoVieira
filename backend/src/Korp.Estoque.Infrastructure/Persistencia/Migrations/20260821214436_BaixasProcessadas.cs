using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Korp.Estoque.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class BaixasProcessadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "baixas_processadas",
                schema: "public",
                columns: table => new
                {
                    nota_fiscal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resposta_json = table.Column<string>(type: "jsonb", nullable: false),
                    processada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_baixas_processadas", x => x.nota_fiscal_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "baixas_processadas",
                schema: "public");
        }
    }
}
