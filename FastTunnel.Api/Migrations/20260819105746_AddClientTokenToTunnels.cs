using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastTunnel.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTokenToTunnels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "WebTunnels");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ForwardTunnels");

            migrationBuilder.AddColumn<string>(
                name: "ClientToken",
                table: "WebTunnels",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientToken",
                table: "ForwardTunnels",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientToken",
                table: "WebTunnels");

            migrationBuilder.DropColumn(
                name: "ClientToken",
                table: "ForwardTunnels");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "WebTunnels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "ForwardTunnels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
