using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObservacaoBoletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObservacaoBoletos",
                table: "SeguroNovos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacaoBoletos",
                table: "RelatorioRenovacoes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObservacaoBoletos",
                table: "SeguroNovos");

            migrationBuilder.DropColumn(
                name: "ObservacaoBoletos",
                table: "RelatorioRenovacoes");
        }
    }
}
