using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnkiBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningEntryMediaUploadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUploadError",
                schema: "Learning",
                table: "LearningEntry",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioUploadStatus",
                schema: "Learning",
                table: "LearningEntry",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.AddColumn<string>(
                name: "ImageUploadError",
                schema: "Learning",
                table: "LearningEntry",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUploadStatus",
                schema: "Learning",
                table: "LearningEntry",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.Sql(
                """
                UPDATE "Learning"."LearningEntry"
                SET "AudioUploadStatus" = CASE
                    WHEN "AudioPath" IS NULL OR BTRIM("AudioPath") = '' THEN 'NotStarted'
                    ELSE 'Success'
                END,
                "ImageUploadStatus" = CASE
                    WHEN "ImagePath" IS NULL OR BTRIM("ImagePath") = '' THEN 'NotStarted'
                    ELSE 'Success'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUploadError",
                schema: "Learning",
                table: "LearningEntry");

            migrationBuilder.DropColumn(
                name: "AudioUploadStatus",
                schema: "Learning",
                table: "LearningEntry");

            migrationBuilder.DropColumn(
                name: "ImageUploadError",
                schema: "Learning",
                table: "LearningEntry");

            migrationBuilder.DropColumn(
                name: "ImageUploadStatus",
                schema: "Learning",
                table: "LearningEntry");
        }
    }
}
