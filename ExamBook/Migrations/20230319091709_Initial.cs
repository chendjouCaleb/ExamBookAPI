using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamBook.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Space",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Identifier = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedIdentifier = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCertified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Twitter = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Youtube = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Facebook = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instagram = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Website = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoverImageId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Space", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classrooms_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTeacher = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Capacity = table.Column<uint>(type: "int unsigned", nullable: false),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Specialities",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialities_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Coefficient = table.Column<uint>(type: "int unsigned", nullable: false),
                    IsGeneral = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Courses_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Examinations",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsLock = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SpaceId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Examinations_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Examinations_Space_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Space",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedRId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sex = table.Column<string>(type: "varchar(1)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClassroomSpeciality",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    SpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomSpeciality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomSpeciality_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassroomSpeciality_Specialities_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "Specialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseTeachers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    MemberId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    IsPrincipal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseTeachers_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseTeachers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExaminationSpecialities",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExaminationId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExaminationSpecialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExaminationSpecialities_Examinations_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "Examinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExaminationId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    RoomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Duration = table.Column<uint>(type: "int unsigned", nullable: false),
                    Coefficient = table.Column<uint>(type: "int unsigned", nullable: false),
                    Radical = table.Column<uint>(type: "int unsigned", nullable: false),
                    Closed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Specialized = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tests_Examinations_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "Examinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tests_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    NormalizedRId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Sex = table.Column<string>(type: "varchar(1)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudentId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ExaminationId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_Examinations_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "Examinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participants_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseSpecialities",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ClassroomSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSpecialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSpecialities_ClassroomSpeciality_ClassroomSpecialityId",
                        column: x => x.ClassroomSpecialityId,
                        principalTable: "ClassroomSpeciality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSpecialities_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseHours",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    RoomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CourseTeacherId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseHours_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseHours_CourseTeachers_CourseTeacherId",
                        column: x => x.CourseTeacherId,
                        principalTable: "CourseTeachers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseHours_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseHours_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TestGroups",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Capacity = table.Column<uint>(type: "int unsigned", nullable: false),
                    RoomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TestId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestGroups_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestGroups_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TestSpecialities",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExaminationSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TestId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSpecialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestSpecialities_ExaminationSpecialities_ExaminationSpeciali~",
                        column: x => x.ExaminationSpecialityId,
                        principalTable: "ExaminationSpecialities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestSpecialities_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParticipantSpecialities",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParticipantId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ExaminationSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantSpecialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantSpecialities_ExaminationSpecialities_ExaminationS~",
                        column: x => x.ExaminationSpecialityId,
                        principalTable: "ExaminationSpecialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantSpecialities_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseSessions",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClassroomId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CourseHourId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CourseId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    RoomId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CourseTeacherId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ExpectedStartDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpectedEndDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Report = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessions_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSessions_CourseHours_CourseHourId",
                        column: x => x.CourseHourId,
                        principalTable: "CourseHours",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseSessions_CourseTeachers_CourseTeacherId",
                        column: x => x.CourseTeacherId,
                        principalTable: "CourseTeachers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseSessions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Papers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Score = table.Column<float>(type: "float", nullable: true),
                    IndexInGroup = table.Column<uint>(type: "int unsigned", nullable: false),
                    IsPresent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParticipantId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TestId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TestSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TestGroupId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Papers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Papers_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Papers_TestGroups_TestGroupId",
                        column: x => x.TestGroupId,
                        principalTable: "TestGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Papers_TestSpecialities_TestSpecialityId",
                        column: x => x.TestSpecialityId,
                        principalTable: "TestSpecialities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Papers_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaperSpeciality",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PaperId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ParticipantSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TestSpecialityId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedById = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperSpeciality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaperSpeciality_Papers_PaperId",
                        column: x => x.PaperId,
                        principalTable: "Papers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaperSpeciality_ParticipantSpecialities_ParticipantSpecialit~",
                        column: x => x.ParticipantSpecialityId,
                        principalTable: "ParticipantSpecialities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaperSpeciality_TestSpecialities_TestSpecialityId",
                        column: x => x.TestSpecialityId,
                        principalTable: "TestSpecialities",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_SpaceId",
                table: "Classrooms",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomSpeciality_ClassroomId",
                table: "ClassroomSpeciality",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomSpeciality_SpecialityId",
                table: "ClassroomSpeciality",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseHours_ClassroomId",
                table: "CourseHours",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseHours_CourseId",
                table: "CourseHours",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseHours_CourseTeacherId",
                table: "CourseHours",
                column: "CourseTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseHours_RoomId",
                table: "CourseHours",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ClassroomId",
                table: "Courses",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SpaceId",
                table: "Courses",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_ClassroomId",
                table: "CourseSessions",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseHourId",
                table: "CourseSessions",
                column: "CourseHourId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseId",
                table: "CourseSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseTeacherId",
                table: "CourseSessions",
                column: "CourseTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_RoomId",
                table: "CourseSessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSpecialities_ClassroomSpecialityId",
                table: "CourseSpecialities",
                column: "ClassroomSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSpecialities_CourseId",
                table: "CourseSpecialities",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTeachers_CourseId",
                table: "CourseTeachers",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTeachers_MemberId",
                table: "CourseTeachers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Examinations_ClassroomId",
                table: "Examinations",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Examinations_SpaceId",
                table: "Examinations",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationSpecialities_ExaminationId",
                table: "ExaminationSpecialities",
                column: "ExaminationId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_SpaceId",
                table: "Members",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_ParticipantId",
                table: "Papers",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_TestGroupId",
                table: "Papers",
                column: "TestGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_TestId",
                table: "Papers",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_Papers_TestSpecialityId",
                table: "Papers",
                column: "TestSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperSpeciality_PaperId",
                table: "PaperSpeciality",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperSpeciality_ParticipantSpecialityId",
                table: "PaperSpeciality",
                column: "ParticipantSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperSpeciality_TestSpecialityId",
                table: "PaperSpeciality",
                column: "TestSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_ExaminationId",
                table: "Participants",
                column: "ExaminationId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_StudentId",
                table: "Participants",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSpecialities_ExaminationSpecialityId",
                table: "ParticipantSpecialities",
                column: "ExaminationSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSpecialities_ParticipantId",
                table: "ParticipantSpecialities",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_SpaceId",
                table: "Rooms",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_SpaceId",
                table: "Specialities",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassroomId",
                table: "Students",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_TestGroups_RoomId",
                table: "TestGroups",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TestGroups_TestId",
                table: "TestGroups",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_ExaminationId",
                table: "Tests",
                column: "ExaminationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_RoomId",
                table: "Tests",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TestSpecialities_ExaminationSpecialityId",
                table: "TestSpecialities",
                column: "ExaminationSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_TestSpecialities_TestId",
                table: "TestSpecialities",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSessions");

            migrationBuilder.DropTable(
                name: "CourseSpecialities");

            migrationBuilder.DropTable(
                name: "PaperSpeciality");

            migrationBuilder.DropTable(
                name: "CourseHours");

            migrationBuilder.DropTable(
                name: "ClassroomSpeciality");

            migrationBuilder.DropTable(
                name: "Papers");

            migrationBuilder.DropTable(
                name: "ParticipantSpecialities");

            migrationBuilder.DropTable(
                name: "CourseTeachers");

            migrationBuilder.DropTable(
                name: "Specialities");

            migrationBuilder.DropTable(
                name: "TestGroups");

            migrationBuilder.DropTable(
                name: "TestSpecialities");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "ExaminationSpecialities");

            migrationBuilder.DropTable(
                name: "Tests");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Examinations");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Space");
        }
    }
}
