using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEQuestionBank.Core.Migrations
{
    /// <inheritdoc />
    public partial class updatetablecauhoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LoiCauHoi",
                table: "CauHoi",
                newName: "LoaiCauHoi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LoaiCauHoi",
                table: "CauHoi",
                newName: "LoiCauHoi");
        }
    }
}
