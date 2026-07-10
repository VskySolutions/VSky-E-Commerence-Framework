using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSky.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreNotificationEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "Stores");
        }
    }
}
