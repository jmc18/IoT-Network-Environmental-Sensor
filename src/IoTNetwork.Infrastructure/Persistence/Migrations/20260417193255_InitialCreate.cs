using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTNetwork.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_data_days",
                columns: table => new
                {
                    NodeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DayUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_data_days", x => new { x.NodeId, x.DayUtc });
                });

            migrationBuilder.CreateTable(
                name: "telemetry_readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Humidity = table.Column<double>(type: "double precision", nullable: true),
                    Co2 = table.Column<double>(type: "double precision", nullable: true),
                    NoiseLevel = table.Column<double>(type: "double precision", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_readings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_data_days_node",
                table: "node_data_days",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_node_time",
                table: "telemetry_readings",
                columns: new[] { "NodeId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_data_days");

            migrationBuilder.DropTable(
                name: "telemetry_readings");
        }
    }
}
