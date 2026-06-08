using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypto.Migrations
{
    /// <inheritdoc />
    public partial class IdCoinGeckoForMoedas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoinGeckoId",
                table: "Moedas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CoinGeckoId",
                value: "bitcoin");

            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CoinGeckoId",
                value: "etherium");

            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CoinGeckoId",
                value: "monero");

            migrationBuilder.UpdateData(
                table: "Moedas",
                keyColumn: "Id",
                keyValue: 4,
                column: "CoinGeckoId",
                value: "solana");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoinGeckoId",
                table: "Moedas");
        }
    }
}
