using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameUniqueIndexToCodigoDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatorioRenovacoes_Proposta",
                table: "RelatorioRenovacoes");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioRenovacoes_CodigoDocumento",
                table: "RelatorioRenovacoes",
                column: "CodigoDocumento",
                unique: true,
                filter: "\"CodigoDocumento\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatorioRenovacoes_CodigoDocumento",
                table: "RelatorioRenovacoes");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioRenovacoes_Proposta",
                table: "RelatorioRenovacoes",
                column: "Proposta",
                unique: true,
                filter: "\"Proposta\" IS NOT NULL");
        }
    }
}
