using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Vin = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DealerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle", x => x.Vin);
                });

            migrationBuilder.CreateTable(
                name: "VehicleVariable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleVariable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalCarInfo",
                columns: table => new
                {
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalCarInfo", x => new { x.VariableId, x.VehicleId, x.Value });
                    table.ForeignKey(
                        name: "FK_AdditionalCarInfo_VehicleVariable_VariableId",
                        column: x => x.VariableId,
                        principalTable: "VehicleVariable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdditionalCarInfo_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "Vin",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "VehicleVariable",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 5, "Body Class" },
                    { 15, "Drive Type" },
                    { 24, "Fuel Type - Primary" },
                    { 26, "Make" },
                    { 27, "Manufacturer Name" },
                    { 28, "Model" },
                    { 29, "Model Year" },
                    { 34, "Series" },
                    { 37, "Transmission Style" },
                    { 38, "Trim" },
                    { 39, "Vehicle Type" },
                    { 64, "Engine Configuration" },
                    { 66, "Fuel Type - Secondary" },
                    { 139, "Top Speed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalCarInfo_VehicleId",
                table: "AdditionalCarInfo",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalCarInfo");

            migrationBuilder.DropTable(
                name: "VehicleVariable");

            migrationBuilder.DropTable(
                name: "Vehicle");
        }
    }
}
