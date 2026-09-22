using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPrepostoToRelatorioRenovacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Preposto",
                table: "RelatorioRenovacoes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preposto",
                table: "RelatorioRenovacoes");
        }
    }
}
