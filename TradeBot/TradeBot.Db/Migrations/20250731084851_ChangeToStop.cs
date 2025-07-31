using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeBot.Db.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossOrderId",
                table: "FakePositions");

            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "FakePositions");

            migrationBuilder.DropColumn(
                name: "TakeProfitOrderId",
                table: "FakePositions");

            migrationBuilder.DropColumn(
                name: "TakeProfitPrice",
                table: "FakePositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StopLossOrderId",
                table: "FakePositions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPrice",
                table: "FakePositions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TakeProfitOrderId",
                table: "FakePositions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TakeProfitPrice",
                table: "FakePositions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);
        }
    }
}
