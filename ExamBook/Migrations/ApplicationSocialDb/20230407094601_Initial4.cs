using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamBook.Migrations.ApplicationSocialDb
{
    /// <inheritdoc />
    public partial class Initial4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublisherId",
                table: "Posts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "Authors",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PublisherId",
                table: "Authors",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuthorSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuthorId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubscriptionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorSubscriptions_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreateAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reactions_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorSubscriptions_AuthorId",
                table: "AuthorSubscriptions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_AuthorId",
                table: "Reactions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId",
                table: "Reactions",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorSubscriptions");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropColumn(
                name: "PublisherId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "PublisherId",
                table: "Authors");
        }
    }
}
