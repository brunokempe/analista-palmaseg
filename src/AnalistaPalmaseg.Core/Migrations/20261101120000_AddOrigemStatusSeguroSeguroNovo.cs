using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaPalmaseg.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddOrigemStatusSeguroSeguroNovo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "SeguroNovos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusSeguro",
                table: "SeguroNovos",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Débito automático passa a ser "Débito em conta"; financiamento deixa de existir.
            migrationBuilder.Sql("UPDATE \"SeguroNovos\" SET \"FormaPagamento\" = 'Débito em conta' WHERE \"FormaPagamento\" = 'Débito automático';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Origem", table: "SeguroNovos");
            migrationBuilder.DropColumn(name: "StatusSeguro", table: "SeguroNovos");
        }
    }
}
