using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankapp.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sequences",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sequences", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "Sequences",
                columns: new[] { "Key", "Value" },
                values: new object[] { "AccountNumber", 10000000 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sequences");
        }
    }
}
