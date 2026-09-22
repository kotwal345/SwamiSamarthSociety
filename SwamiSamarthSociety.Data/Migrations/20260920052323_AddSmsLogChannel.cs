using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwamiSamarthSociety.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsLogChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "SmsLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "SMS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "SmsLog");
        }
    }
}
