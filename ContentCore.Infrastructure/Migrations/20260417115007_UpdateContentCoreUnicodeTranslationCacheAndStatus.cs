using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContentCoreUnicodeTranslationCacheAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalTextHash",
                schema: "content_core",
                table: "TranslationCaches",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Tags",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "content_core",
                table: "Tags",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "content_core",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Specializations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "content_core",
                table: "Specializations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldUnicode: false,
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NativeName",
                schema: "content_core",
                table: "Languages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Languages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "CategoryTranslations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                schema: "content_core",
                table: "CategoryTranslations",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Categories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "UX_TranslationCache_Hash",
                schema: "content_core",
                table: "TranslationCaches",
                column: "OriginalTextHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TranslationCache_Hash",
                schema: "content_core",
                table: "TranslationCaches");

            migrationBuilder.DropColumn(
                name: "OriginalTextHash",
                schema: "content_core",
                table: "TranslationCaches");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "content_core",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "content_core",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "content_core",
                table: "CategoryTranslations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Tags",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Specializations",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "content_core",
                table: "Specializations",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NativeName",
                schema: "content_core",
                table: "Languages",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Languages",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "CategoryTranslations",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "content_core",
                table: "Categories",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
