using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypto.Migrations
{
    /// <inheritdoc />
    public partial class AttSaldoCriptoMoeda_Relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Carteiras_CarteiraId",
                table: "Transacoes");

            migrationBuilder.DropIndex(
                name: "IX_SaldoCriptos_MoedaId",
                table: "SaldoCriptos");

            migrationBuilder.AlterColumn<Guid>(
                name: "CarteiraId",
                table: "Transacoes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_SaldoCriptos_MoedaId",
                table: "SaldoCriptos",
                column: "MoedaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Carteiras_CarteiraId",
                table: "Transacoes",
                column: "CarteiraId",
                principalTable: "Carteiras",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Carteiras_CarteiraId",
                table: "Transacoes");

            migrationBuilder.DropIndex(
                name: "IX_SaldoCriptos_MoedaId",
                table: "SaldoCriptos");

            migrationBuilder.AlterColumn<Guid>(
                name: "CarteiraId",
                table: "Transacoes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaldoCriptos_MoedaId",
                table: "SaldoCriptos",
                column: "MoedaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Carteiras_CarteiraId",
                table: "Transacoes",
                column: "CarteiraId",
                principalTable: "Carteiras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
