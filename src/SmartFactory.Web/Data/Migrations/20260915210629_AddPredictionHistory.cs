using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFactory.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    AirTemperature_C = table.Column<double>(type: "float", nullable: false),
                    ProcessTemperature_C = table.Column<double>(type: "float", nullable: false),
                    RotationalSpeed_RPM = table.Column<double>(type: "float", nullable: false),
                    Torque_Nm = table.Column<double>(type: "float", nullable: false),
                    ToolWear_Min = table.Column<double>(type: "float", nullable: false),
                    FailureProbability = table.Column<double>(type: "float", nullable: false),
                    FailurePredicted = table.Column<bool>(type: "bit", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PredictedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionHistories_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionHistories_MachineId",
                table: "PredictionHistories",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionHistories_PredictedAt",
                table: "PredictionHistories",
                column: "PredictedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionHistories_RiskLevel",
                table: "PredictionHistories",
                column: "RiskLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionHistories");
        }
    }
}
