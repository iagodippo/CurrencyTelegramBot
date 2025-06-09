using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurrencyTelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UltimoEnvio",
                table: "User",
                newName: "LastNotify");

            migrationBuilder.RenameColumn(
                name: "IntervaloMinutos",
                table: "User",
                newName: "MinutesInterval");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "MinutesInterval",
                table: "User",
                newName: "IntervaloMinutos");

            migrationBuilder.RenameColumn(
                name: "LastNotify",
                table: "User",
                newName: "UltimoEnvio");
        }
    }
}
