using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniManageSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentApprovalUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Enrollments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ApprovedById",
                table: "Enrollments",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AspNetUsers_ApprovedById",
                table: "Enrollments",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AspNetUsers_ApprovedById",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ApprovedById",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enrollments");
        }
    }
}
