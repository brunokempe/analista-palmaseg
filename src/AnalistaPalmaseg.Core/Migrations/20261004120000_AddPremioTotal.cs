using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPremioTotal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PremioTotal",
                table: "SeguroNovos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FechamentoPremioTotal",
                table: "RelatorioRenovacoes",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremioTotal",
                table: "SeguroNovos");

            migrationBuilder.DropColumn(
                name: "FechamentoPremioTotal",
                table: "RelatorioRenovacoes");
        }
    }
}
