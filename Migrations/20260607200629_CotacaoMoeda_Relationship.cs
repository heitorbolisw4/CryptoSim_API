using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Crypto.Migrations
{
    /// <inheritdoc />
    public partial class CotacaoMoeda_Relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cotacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MoedaId = table.Column<int>(type: "integer", nullable: false),
                    PrecoBrl = table.Column<decimal>(type: "numeric", nullable: false),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotacoes_Moedas_MoedaId",
                        column: x => x.MoedaId,
                        principalTable: "Moedas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Moedas",
                columns: new[] { "Id", "Ativo", "Nome", "Simbolo" },
                values: new object[,]
                {
                    { 1, true, "Bitcoin", "BTC" },
                    { 2, true, "Etherium", "ETH" },
                    { 3, true, "Monero", "MON" },
                    { 4, true, "Solana", "SOL" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cotacoes_MoedaId",
                table: "Cotacoes",
                column: "MoedaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cotacoes");

            migrationBuilder.DeleteData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
