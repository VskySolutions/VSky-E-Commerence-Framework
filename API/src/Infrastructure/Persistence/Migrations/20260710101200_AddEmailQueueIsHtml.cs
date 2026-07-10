using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VSky.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailQueueIsHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHtml",
                table: "EmailQueue",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHtml",
                table: "EmailQueue");
        }
    }
}
