using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEQuestionBank.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Khoa",
                columns: table => new
                {
                    MaKhoa = table.Column<Guid>(type: "uuid", nullable: false),
                    TenKhoa = table.Column<string>(type: "text", nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: true),
                    XoaTamKhoa = table.Column<bool>(type: "boolean", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoa", x => x.MaKhoa);
                });

            migrationBuilder.CreateTable(
                name: "MonHoc",
                columns: table => new
                {
                    MaMonHoc = table.Column<Guid>(type: "uuid", nullable: false),
                    TenMonHoc = table.Column<string>(type: "text", nullable: false),
                    MaSoMonHoc = table.Column<string>(type: "text", nullable: false),
                    SoTinChi = table.Column<int>(type: "integer", nullable: true),
                    XoaTamMonHoc = table.Column<bool>(type: "boolean", nullable: true),
                    MaKhoa = table.Column<Guid>(type: "uuid", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHoc", x => x.MaMonHoc);
                    table.ForeignKey(
                        name: "FK_MonHoc_Khoa_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "Khoa",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    MaNguoiDung = table.Column<Guid>(type: "uuid", nullable: false),
                    TenDangNhap = table.Column<string>(type: "text", nullable: false),
                    MatKhau = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    VaiTro = table.Column<int>(type: "integer", nullable: false),
                    BiKhoa = table.Column<bool>(type: "boolean", nullable: false),
                    MaKhoa = table.Column<Guid>(type: "uuid", nullable: true),
                    NgayDangNhapCuoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.MaNguoiDung);
                    table.ForeignKey(
                        name: "FK_NguoiDung_Khoa_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "Khoa",
                        principalColumn: "MaKhoa");
                });

            migrationBuilder.CreateTable(
                name: "DeThis",
                columns: table => new
                {
                    MaDeThi = table.Column<Guid>(type: "uuid", nullable: false),
                    MaMonHoc = table.Column<Guid>(type: "uuid", nullable: false),
                    TenDeThi = table.Column<string>(type: "text", nullable: false),
                    DaDuyet = table.Column<bool>(type: "boolean", nullable: false),
                    SoCauHoi = table.Column<int>(type: "integer", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeThis", x => x.MaDeThi);
                    table.ForeignKey(
                        name: "FK_DeThis_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phan",
                columns: table => new
                {
                    MaPhan = table.Column<Guid>(type: "uuid", nullable: false),
                    MaMonHoc = table.Column<Guid>(type: "uuid", nullable: false),
                    TenPhan = table.Column<string>(type: "text", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    SoLuongCauHoi = table.Column<int>(type: "integer", nullable: false),
                    MaPhanCha = table.Column<Guid>(type: "uuid", nullable: true),
                    MaSoPhan = table.Column<int>(type: "integer", nullable: true),
                    XoaTamPhan = table.Column<bool>(type: "boolean", nullable: true),
                    LaCauHoiNhom = table.Column<bool>(type: "boolean", nullable: false),
                    MonHocMaMonHoc = table.Column<Guid>(type: "uuid", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phan", x => x.MaPhan);
                    table.ForeignKey(
                        name: "FK_Phan_MonHoc_MonHocMaMonHoc",
                        column: x => x.MonHocMaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc");
                    table.ForeignKey(
                        name: "FK_Phan_Phan_MaPhanCha",
                        column: x => x.MaPhanCha,
                        principalTable: "Phan",
                        principalColumn: "MaPhan");
                });

            migrationBuilder.CreateTable(
                name: "YeuCauRutTrich",
                columns: table => new
                {
                    MaYeuCau = table.Column<Guid>(type: "uuid", nullable: false),
                    MaNguoiDung = table.Column<Guid>(type: "uuid", nullable: false),
                    MaMonHoc = table.Column<Guid>(type: "uuid", nullable: false),
                    NoiDungRutTrich = table.Column<string>(type: "text", nullable: true),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    NgayYeuCau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DaXuLy = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauRutTrich", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauRutTrich_MonHoc_MaMonHoc",
                        column: x => x.MaMonHoc,
                        principalTable: "MonHoc",
                        principalColumn: "MaMonHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauRutTrich_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CauHoi",
                columns: table => new
                {
                    MaCauHoi = table.Column<Guid>(type: "uuid", nullable: false),
                    MaPhan = table.Column<Guid>(type: "uuid", nullable: false),
                    MaSoCauHoi = table.Column<int>(type: "integer", nullable: true),
                    NoiDung = table.Column<string>(type: "text", nullable: true),
                    HoanVi = table.Column<bool>(type: "boolean", nullable: false),
                    CapDo = table.Column<short>(type: "smallint", nullable: false),
                    SoCauHoiCon = table.Column<int>(type: "integer", nullable: false),
                    MaCauHoiCha = table.Column<Guid>(type: "uuid", nullable: true),
                    TrangThai = table.Column<bool>(type: "boolean", nullable: true),
                    SoLanDuocThi = table.Column<int>(type: "integer", nullable: true),
                    SoLanDung = table.Column<int>(type: "integer", nullable: true),
                    DoPhanCachCauHoi = table.Column<float>(type: "real", nullable: true),
                    XoaTam = table.Column<bool>(type: "boolean", nullable: true),
                    CLO = table.Column<int>(type: "integer", nullable: true),
                    NguoiTao = table.Column<Guid>(type: "uuid", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHoi", x => x.MaCauHoi);
                    table.ForeignKey(
                        name: "FK_CauHoi_CauHoi_MaCauHoiCha",
                        column: x => x.MaCauHoiCha,
                        principalTable: "CauHoi",
                        principalColumn: "MaCauHoi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CauHoi_Phan_MaPhan",
                        column: x => x.MaPhan,
                        principalTable: "Phan",
                        principalColumn: "MaPhan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CauTraLoi",
                columns: table => new
                {
                    MaCauTraLoi = table.Column<Guid>(type: "uuid", nullable: false),
                    MaCauHoi = table.Column<Guid>(type: "uuid", nullable: false),
                    NoiDung = table.Column<string>(type: "text", nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    HoanVi = table.Column<bool>(type: "boolean", nullable: true),
                    LaDapAn = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauTraLoi", x => x.MaCauTraLoi);
                    table.ForeignKey(
                        name: "FK_CauTraLoi_CauHoi_MaCauHoi",
                        column: x => x.MaCauHoi,
                        principalTable: "CauHoi",
                        principalColumn: "MaCauHoi",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDeThi",
                columns: table => new
                {
                    MaDeThi = table.Column<Guid>(type: "uuid", nullable: false),
                    MaCauHoi = table.Column<Guid>(type: "uuid", nullable: false),
                    MaPhan = table.Column<Guid>(type: "uuid", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDeThi", x => new { x.MaDeThi, x.MaCauHoi });
                    table.ForeignKey(
                        name: "FK_ChiTietDeThi_CauHoi_MaCauHoi",
                        column: x => x.MaCauHoi,
                        principalTable: "CauHoi",
                        principalColumn: "MaCauHoi",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDeThi_DeThis_MaDeThi",
                        column: x => x.MaDeThi,
                        principalTable: "DeThis",
                        principalColumn: "MaDeThi",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDeThi_Phan_MaPhan",
                        column: x => x.MaPhan,
                        principalTable: "Phan",
                        principalColumn: "MaPhan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    MaFile = table.Column<Guid>(type: "uuid", nullable: false),
                    MaCauHoi = table.Column<Guid>(type: "uuid", nullable: true),
                    TenFile = table.Column<string>(type: "text", nullable: true),
                    LoaiFile = table.Column<int>(type: "integer", nullable: true),
                    MaCauTraLoi = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.MaFile);
                    table.ForeignKey(
                        name: "FK_Files_CauHoi_MaCauHoi",
                        column: x => x.MaCauHoi,
                        principalTable: "CauHoi",
                        principalColumn: "MaCauHoi");
                    table.ForeignKey(
                        name: "FK_Files_CauTraLoi_MaCauTraLoi",
                        column: x => x.MaCauTraLoi,
                        principalTable: "CauTraLoi",
                        principalColumn: "MaCauTraLoi");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CauHoi_MaCauHoiCha",
                table: "CauHoi",
                column: "MaCauHoiCha");

            migrationBuilder.CreateIndex(
                name: "IX_CauHoi_MaPhan",
                table: "CauHoi",
                column: "MaPhan");

            migrationBuilder.CreateIndex(
                name: "IX_CauTraLoi_MaCauHoi",
                table: "CauTraLoi",
                column: "MaCauHoi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDeThi_MaCauHoi",
                table: "ChiTietDeThi",
                column: "MaCauHoi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDeThi_MaPhan",
                table: "ChiTietDeThi",
                column: "MaPhan");

            migrationBuilder.CreateIndex(
                name: "IX_DeThis_MaMonHoc",
                table: "DeThis",
                column: "MaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_Files_MaCauHoi",
                table: "Files",
                column: "MaCauHoi");

            migrationBuilder.CreateIndex(
                name: "IX_Files_MaCauTraLoi",
                table: "Files",
                column: "MaCauTraLoi");

            migrationBuilder.CreateIndex(
                name: "IX_MonHoc_MaKhoa",
                table: "MonHoc",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_MonHoc_MaSoMonHoc",
                table: "MonHoc",
                column: "MaSoMonHoc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaKhoa",
                table: "NguoiDung",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_Phan_MaPhanCha",
                table: "Phan",
                column: "MaPhanCha");

            migrationBuilder.CreateIndex(
                name: "IX_Phan_MonHocMaMonHoc",
                table: "Phan",
                column: "MonHocMaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauRutTrich_MaMonHoc",
                table: "YeuCauRutTrich",
                column: "MaMonHoc");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauRutTrich_MaNguoiDung",
                table: "YeuCauRutTrich",
                column: "MaNguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDeThi");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "YeuCauRutTrich");

            migrationBuilder.DropTable(
                name: "DeThis");

            migrationBuilder.DropTable(
                name: "CauTraLoi");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "CauHoi");

            migrationBuilder.DropTable(
                name: "Phan");

            migrationBuilder.DropTable(
                name: "MonHoc");

            migrationBuilder.DropTable(
                name: "Khoa");
        }
    }
}
