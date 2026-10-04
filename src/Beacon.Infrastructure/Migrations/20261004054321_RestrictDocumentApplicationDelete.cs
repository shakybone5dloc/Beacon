using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beacon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDocumentApplicationDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_JobApplications_JobApplicationId",
                table: "Documents");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_JobApplications_JobApplicationId",
                table: "Documents",
                column: "JobApplicationId",
                principalTable: "JobApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_JobApplications_JobApplicationId",
                table: "Documents");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_JobApplications_JobApplicationId",
                table: "Documents",
                column: "JobApplicationId",
                principalTable: "JobApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
