using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypto.Migrations
{
    /// <inheritdoc />
    public partial class AtualizacaoIdEthereum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CoinGeckoId", "Nome" },
                values: new object[] { "ethereum", "Ethereum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CoinGeckoId", "Nome" },
                values: new object[] { "etherium", "Etherium" });
        }
    }
}
