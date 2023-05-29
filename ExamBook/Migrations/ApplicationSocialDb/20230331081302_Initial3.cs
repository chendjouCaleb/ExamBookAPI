using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamBook.Migrations.ApplicationSocialDb
{
    /// <inheritdoc />
    public partial class Initial3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    FileId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PosterId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPicture = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsGif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsVideo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAudio = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostFiles_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PostFiles_PostId",
                table: "PostFiles",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostFiles");
        }
    }
}
