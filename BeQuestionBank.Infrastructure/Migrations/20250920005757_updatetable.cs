using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEQuestionBank.Core.Migrations
{
    /// <inheritdoc />
    public partial class updatetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDeThi_DeThis_MaDeThi",
                table: "ChiTietDeThi");

            migrationBuilder.DropForeignKey(
                name: "FK_DeThis_MonHoc_MaMonHoc",
                table: "DeThis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeThis",
                table: "DeThis");

            migrationBuilder.RenameTable(
                name: "DeThis",
                newName: "DeThi");

            migrationBuilder.RenameIndex(
                name: "IX_DeThis_MaMonHoc",
                table: "DeThi",
                newName: "IX_DeThi_MaMonHoc");

            migrationBuilder.AddColumn<string>(
                name: "MaTran",
                table: "YeuCauRutTrich",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoiCauHoi",
                table: "CauHoi",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeThi",
                table: "DeThi",
                column: "MaDeThi");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDeThi_DeThi_MaDeThi",
                table: "ChiTietDeThi",
                column: "MaDeThi",
                principalTable: "DeThi",
                principalColumn: "MaDeThi",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeThi_MonHoc_MaMonHoc",
                table: "DeThi",
                column: "MaMonHoc",
                principalTable: "MonHoc",
                principalColumn: "MaMonHoc",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDeThi_DeThi_MaDeThi",
                table: "ChiTietDeThi");

            migrationBuilder.DropForeignKey(
                name: "FK_DeThi_MonHoc_MaMonHoc",
                table: "DeThi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeThi",
                table: "DeThi");

            migrationBuilder.DropColumn(
                name: "MaTran",
                table: "YeuCauRutTrich");

            migrationBuilder.DropColumn(
                name: "LoiCauHoi",
                table: "CauHoi");

            migrationBuilder.RenameTable(
                name: "DeThi",
                newName: "DeThis");

            migrationBuilder.RenameIndex(
                name: "IX_DeThi_MaMonHoc",
                table: "DeThis",
                newName: "IX_DeThis_MaMonHoc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeThis",
                table: "DeThis",
                column: "MaDeThi");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDeThi_DeThis_MaDeThi",
                table: "ChiTietDeThi",
                column: "MaDeThi",
                principalTable: "DeThis",
                principalColumn: "MaDeThi",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeThis_MonHoc_MaMonHoc",
                table: "DeThis",
                column: "MaMonHoc",
                principalTable: "MonHoc",
                principalColumn: "MaMonHoc",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
