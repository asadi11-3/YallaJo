using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogCommentIsContentRedacted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsContentRedacted",
                schema: "content_blogs",
                table: "BlogComments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsContentRedacted",
                schema: "content_blogs",
                table: "BlogComments");
        }
    }
}
