using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSky.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAccessibleModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessibleModules",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessibleModules",
                table: "Roles");
        }
    }
}
