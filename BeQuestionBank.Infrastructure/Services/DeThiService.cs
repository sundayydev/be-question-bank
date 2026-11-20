using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BEQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using BeQuestionBank.Shared.Enums;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;

namespace BEQuestionBank.Core.Services;

public class DeThiService
{
    private readonly IDeThiRepository _deThiRepository;
    private readonly ICauHoiRepository _cauHoiRepository;
    private readonly IYeuCauRutTrichRepository _yeuCauRutTrichRepository;
    private readonly IPhanRepository _phanRepository;

    public DeThiService(
        IDeThiRepository deThiRepository,
        ICauHoiRepository cauHoiRepository,
        IYeuCauRutTrichRepository yeuCauRutTrichRepository,
        IPhanRepository phanRepository)
    {
        _deThiRepository = deThiRepository;
        _cauHoiRepository = cauHoiRepository;
        _yeuCauRutTrichRepository = yeuCauRutTrichRepository;
        _phanRepository = phanRepository;
    }

    /// <summary>
    /// lây dethi theo id nhưng chỉ có dethi thôi.
    /// </summary>
    public async Task<DeThiDto?> GetBasicByIdAsync(Guid id)
    {
        var deThi = await _deThiRepository.GetByIdAsync(id);
        return deThi != null ? MapToBasicDto(deThi) : null;
    }

    /// <summary>
    ///  lây dethi theo id nhưng có them chi tiet
    /// </summary>
    public async Task<DeThiWithChiTietDto?> GetByIdWithChiTietAsync(Guid id)
    {
        var deThi = await _deThiRepository.GetByIdWithChiTietAsync(id);
        if (deThi == null)
            return null;

        return MapToChiTietDto((DeThi)deThi);
    }

    /// <summary>
    ///  lây dethi theo id nhưng có chi tiêt và câu trả lời
    /// </summary>
    public async Task<DeThiWithChiTietAndCauTraLoiDto?> GetByIdWithChiTietAndCauTraLoiAsync(Guid id)
    {
        var deThi = await _deThiRepository.GetDeThiWithChiTietAndCauTraLoiAsync(id);
        if (deThi == null)
            return null;

        return MapToChiTietAndCauTraLoiDto((DeThi)deThi);
    }

    /// <summary>
    /// lấy tất cả dè thi only
    /// </summary>
    public async Task<IEnumerable<DeThiDto>> GetAllBasicAsync()
    {
        var deThis = await _deThiRepository.GetAllBasicAsync();
        return deThis.Cast<DeThi>().Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// lây tât ca nhung co chi tiet
    /// </summary>
    public async Task<IEnumerable<DeThiWithChiTietDto>> GetAllWithChiTietAsync()
    {
        var deThis = await _deThiRepository.GetAllWithChiTietAsync();
        return deThis.Cast<DeThi>().Select(MapToChiTietDto).ToList();
    }

    /// <summary>
    /// de thi đc kich hoạt
    /// </summary>
    public async Task<IEnumerable<DeThiWithChiTietDto>> GetApprovedDeThisAsync()
    {
        var deThis = await _deThiRepository.GetApprovedDeThisAsync();
        return deThis.Cast<DeThi>().Select(MapToChiTietDto).ToList();
    }

    /// <summary>
    /// lây de thi theo ma mon học
    /// </summary>
    public async Task<IEnumerable<DeThiDto>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        var deThis = await _deThiRepository.GetByMaMonHocAsync(maMonHoc);
        return deThis.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// timd dề thi
    /// </summary>
    public async Task<IEnumerable<DeThiDto>> FindAsync(Expression<Func<DeThi, bool>> predicate)
    {
        var deThis = await _deThiRepository.FindAsync(predicate);
        return deThis.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// Retrieves the first DeThi that matches the predicate or null if none found (basic info only).
    /// </summary>
    public async Task<DeThiDto?> FirstOrDefaultAsync(Expression<Func<DeThi, bool>> predicate)
    {
        var deThi = await _deThiRepository.FirstOrDefaultAsync(predicate);
        return deThi != null ? MapToBasicDto(deThi) : null;
    }

    /// <summary>
    /// thêm mới đề thi có chi tiết dề thi
    /// </summary>
    public async Task<(bool Success, string Message, Guid? MaDeThi)> AddAsync(CreateDeThiDto deThiDto)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(deThiDto.TenDeThi))
                return (false, "Tên đề thi không được để trống.", null);
            if (deThiDto.MaMonHoc == Guid.Empty)
                return (false, "Mã môn học không hợp lệ.", null);
            if (deThiDto.ChiTietDeThis == null || !deThiDto.ChiTietDeThis.Any())
                return (false, "Đề thi phải có ít nhất một câu hỏi.", null);
            if (deThiDto.ChiTietDeThis.Any(ct => ct.MaCauHoi == Guid.Empty || ct.MaPhan == Guid.Empty))
                return (false, "Mã câu hỏi hoặc mã phần trong chi tiết đề thi không hợp lệ.", null);

            var maDeThi = Guid.NewGuid();

            // Map DTO → Entity
            var entity = new DeThi
            {
                MaDeThi = maDeThi,
                MaMonHoc = deThiDto.MaMonHoc,
                TenDeThi = deThiDto.TenDeThi,
                DaDuyet = deThiDto.DaDuyet ?? false,
                SoCauHoi = deThiDto.SoCauHoi ?? deThiDto.ChiTietDeThis.Count,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow,
                ChiTietDeThis = deThiDto.ChiTietDeThis.Select(ct => new ChiTietDeThi
                {
                    MaDeThi = maDeThi,
                    MaPhan = ct.MaPhan,
                    MaCauHoi = ct.MaCauHoi,
                    ThuTu = ct.ThuTu
                }).ToList()
            };

            await _deThiRepository.AddAsync(entity);

            return (true, "Thêm đề thi thành công.", entity.MaDeThi);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi thêm đề thi: {ex.Message}", null);
        }
    }

    /// <summary>
    /// update
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(Guid maDeThi, UpdateDeThiDto deThiDto)
    {
        try
        {
            // Validate input
            if (maDeThi == Guid.Empty)
                return (false, "Mã đề thi không hợp lệ.");
            if (string.IsNullOrWhiteSpace(deThiDto.TenDeThi))
                return (false, "Tên đề thi không được để trống.");
            if (deThiDto.MaMonHoc == Guid.Empty)
                return (false, "Mã môn học không hợp lệ.");

            var existingDeThi = await _deThiRepository.GetByIdAsync(maDeThi);
            if (existingDeThi == null)
                return (false, "Đề thi không tồn tại.");

            // Update fields
            existingDeThi.TenDeThi = deThiDto.TenDeThi;
            existingDeThi.MaMonHoc = deThiDto.MaMonHoc;
            existingDeThi.DaDuyet = deThiDto.DaDuyet ?? existingDeThi.DaDuyet;
            existingDeThi.SoCauHoi = deThiDto.SoCauHoi ?? existingDeThi.SoCauHoi;
            existingDeThi.NgayCapNhat = DateTime.UtcNow;

            await _deThiRepository.UpdateAsync(existingDeThi);
            return (true, "Cập nhật đề thi thành công.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi cập nhật đề thi: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes 
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
    {
        try
        {
            var deThi = await _deThiRepository.GetByIdAsync(id);
            if (deThi == null)
                return (false, "Đề thi không tồn tại.");

            await _deThiRepository.DeleteAsync(deThi);
            return (true, "Xóa đề thi thành công.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi xóa đề thi: {ex.Message}");
        }
    }

    /// <summary>
    /// map dto only
    /// </summary>
    private DeThiDto MapToBasicDto(DeThi deThi)
    {
        return new DeThiDto
        {
            MaDeThi = deThi.MaDeThi,
            MaMonHoc = deThi.MaMonHoc,
            TenDeThi = deThi.TenDeThi,
            DaDuyet = deThi.DaDuyet,
            SoCauHoi = deThi.SoCauHoi,
            NgayTao = deThi.NgayTao,
            NgayCapNhap = deThi.NgayCapNhat,
            TenMonHoc = deThi.MonHoc?.TenMonHoc,
            TenKhoa = deThi.MonHoc?.Khoa?.TenKhoa
        };
    }
    /// <summary>
    /// Map CauHoi entity → DTO với đáp án và câu hỏi con (có đáp án riêng)
    /// </summary>
    //private CauHoiWithCauTraLoiDto MapCauHoiFull(CauHoi ch)
    //{
    //    var dto = new CauHoiWithCauTraLoiDto
    //    {
    //        MaCauHoi = ch.MaCauHoi,
    //        MaPhan = ch.MaPhan,
    //        MaSoCauHoi = ch.MaSoCauHoi,
    //        NoiDung = ch.NoiDung,
    //        HoanVi = ch.HoanVi,
    //        CapDo = ch.CapDo,
    //        SoCauHoiCon = ch.SoCauHoiCon,
    //        XoaTam = ch.XoaTam,
    //        NgaySua = ch.NgayCapNhat,
    //        CLO = ch.CLO,
    //        LoaiCauHoi = ch.LoaiCauHoi,
    //        MaCauHoiCha = ch.MaCauHoiCha,

    //        // Đáp án của chính câu hỏi này (cha hoặc con)
    //        CauTraLois = ch.CauTraLois?
    //            .OrderBy(tl => tl.ThuTu)
    //            .Select(tl => new CauTraLoiDto
    //            {
    //                MaCauTraLoi = tl.MaCauTraLoi,
    //                MaCauHoi = tl.MaCauHoi,
    //                NoiDung = tl.NoiDung,
    //                ThuTu = tl.ThuTu,
    //                LaDapAn = tl.LaDapAn
    //            }).ToList(),

    //        // Câu hỏi con (nếu có) - và mỗi con cũng có đáp án riêng
    //        CauHoiCons = ch.CauHoiCons?
    //            .OrderBy(con => con.MaCauHoi)
    //            .Select(con => MapCauHoiFull(con))  // Đệ quy để map đầy đủ đáp án của con
    //            .ToList()
    //    };

    //    return dto;
    //}
    private CauHoiDto MapCauHoiFull(CauHoi ch)
    {
        var dto = new CauHoiDto
        {
            MaCauHoi = ch.MaCauHoi,
            MaPhan = ch.MaPhan,
            MaSoCauHoi = ch.MaSoCauHoi,
            NoiDung = ch.NoiDung,
            HoanVi = ch.HoanVi,
            CapDo = ch.CapDo,
            SoCauHoiCon = ch.SoCauHoiCon,
            XoaTam = ch.XoaTam,
            NgaySua = ch.NgayCapNhat,  // hoặc ch.NgaySua nếu entity có field này
            CLO = ch.CLO,
            LoaiCauHoi = ch.LoaiCauHoi,
            MaCauHoiCha = ch.MaCauHoiCha,

            // Đáp án của câu hỏi hiện tại (cha hoặc con)
            CauTraLois = ch.CauTraLois?
                .OrderBy(tl => tl.ThuTu)
                .Select(tl => new CauTraLoiDto
                {
                    MaCauTraLoi = tl.MaCauTraLoi,
                    MaCauHoi = tl.MaCauHoi,
                    NoiDung = tl.NoiDung,
                    ThuTu = tl.ThuTu,
                    LaDapAn = tl.LaDapAn,
                    HoanVi = tl.HoanVi  // nếu entity có
                })
                .ToList(),

            // Map câu hỏi con (chỉ 1 cấp, KHÔNG đệ quy)
            CauHoiCons = ch.CauHoiCons?
                .OrderBy(con => con.MaSoCauHoi)
                .Select(con => new CauHoiDto
                {
                    MaCauHoi = con.MaCauHoi,
                    MaPhan = con.MaPhan,
                    MaSoCauHoi = con.MaSoCauHoi,
                    NoiDung = con.NoiDung,
                    HoanVi = con.HoanVi,
                    CapDo = con.CapDo,
                    SoCauHoiCon = con.SoCauHoiCon,
                    XoaTam = con.XoaTam,
                    NgaySua = con.NgayCapNhat,
                    CLO = con.CLO,
                    LoaiCauHoi = con.LoaiCauHoi,
                    MaCauHoiCha = con.MaCauHoiCha,

                    // Đáp án của câu con
                    CauTraLois = con.CauTraLois?
                        .OrderBy(tl => tl.ThuTu)
                        .Select(tl => new CauTraLoiDto
                        {
                            MaCauTraLoi = tl.MaCauTraLoi,
                            MaCauHoi = tl.MaCauHoi,
                            NoiDung = tl.NoiDung,
                            ThuTu = tl.ThuTu,
                            LaDapAn = tl.LaDapAn,
                            HoanVi = tl.HoanVi
                        })
                        .ToList()

                    // Không map CauHoiCons của con nữa → tránh đệ quy
                    // Nếu sau này cần con của con thì mới thêm
                })
                .ToList()
        };

        return dto;
    }
    /// <summary>
    /// Maps a DeThi ewith chi tiết
    /// </summary>
    private DeThiWithChiTietDto MapToChiTietDto(DeThi deThi)
    {
        return new DeThiWithChiTietDto
        {
            MaDeThi = deThi.MaDeThi,
            MaMonHoc = deThi.MaMonHoc,
            TenDeThi = deThi.TenDeThi,
            DaDuyet = deThi.DaDuyet,
            SoCauHoi = deThi.SoCauHoi,
            NgayTao = deThi.NgayTao,
            NgayCapNhap = deThi.NgayCapNhat,
            TenMonHoc = deThi.MonHoc?.TenMonHoc,
            TenKhoa = deThi.MonHoc?.Khoa?.TenKhoa,
            ChiTietDeThis = deThi.ChiTietDeThis?.Select(ct => new ChiTietDeThiDto
            {
                MaDeThi = ct.MaDeThi,
                MaPhan = ct.MaPhan,
                MaCauHoi = ct.MaCauHoi,
                ThuTu = ct.ThuTu,
                CauHoi = ct.CauHoi != null
                    ? new CauHoiDto
                    {
                        MaCauHoi = ct.CauHoi.MaCauHoi,
                        MaPhan = ct.CauHoi.MaPhan,
                        MaSoCauHoi = ct.CauHoi.MaSoCauHoi,
                        NoiDung = ct.CauHoi.NoiDung,
                        HoanVi = ct.CauHoi.HoanVi,
                        CapDo = ct.CauHoi.CapDo,
                        SoCauHoiCon = ct.CauHoi.SoCauHoiCon,
                        MaCauHoiCha = ct.CauHoi.MaCauHoiCha,
                        SoLanDuocThi = ct.CauHoi.SoLanDuocThi,
                        SoLanDung = ct.CauHoi.SoLanDung,
                        DoPhanCach = ct.CauHoi.DoPhanCach,
                        XoaTam = ct.CauHoi.XoaTam,
                        CLO = ct.CauHoi.CLO,
                        NgaySua = ct.CauHoi.NgayCapNhat,
                        CauHoiCons = ct.CauHoi.CauHoiCons?.Select(chc => new CauHoiDto
                        {
                            MaCauHoi = chc.MaCauHoi,
                            MaPhan = chc.MaPhan,
                            MaSoCauHoi = chc.MaSoCauHoi,
                            NoiDung = chc.NoiDung,
                            HoanVi = chc.HoanVi,
                            CapDo = chc.CapDo,
                            SoCauHoiCon = chc.SoCauHoiCon,
                            MaCauHoiCha = chc.MaCauHoiCha,
                            SoLanDuocThi = chc.SoLanDuocThi,
                            SoLanDung = chc.SoLanDung,
                            DoPhanCach = chc.DoPhanCach,
                            XoaTam = chc.XoaTam,
                            CLO = chc.CLO,
                            NgaySua = chc.NgayCapNhat,
                            CauHoiCons = new List<CauHoiDto>()
                        }).ToList() ?? new List<CauHoiDto>()
                    }
                    : null
            }).ToList() ?? new List<ChiTietDeThiDto>()
        };
    }

    /// <summary>
    /// Maps a DeThi with chi tiết và câu tra lời
    /// </summary>
    private DeThiWithChiTietAndCauTraLoiDto MapToChiTietAndCauTraLoiDto(DeThi deThi)
    {
        return new DeThiWithChiTietAndCauTraLoiDto
        {
            MaDeThi = deThi.MaDeThi,
            MaMonHoc = deThi.MaMonHoc,
            TenDeThi = deThi.TenDeThi,
            DaDuyet = deThi.DaDuyet,
            SoCauHoi = deThi.SoCauHoi,
            NgayTao = deThi.NgayTao,
            NgayCapNhap = deThi.NgayCapNhat,
            TenMonHoc = deThi.MonHoc?.TenMonHoc,
            TenKhoa = deThi.MonHoc?.Khoa?.TenKhoa,

            ChiTietDeThis = deThi.ChiTietDeThis
                .OrderBy(ct => ct.ThuTu)
                .Select(ct => new ChiTietDeThiWithCauTraLoiDto
                {
                    MaDeThi = ct.MaDeThi,
                    MaPhan = ct.MaPhan,
                    MaCauHoi = ct.MaCauHoi,
                    ThuTu = ct.ThuTu ?? 0,
                    CauHoi = ct.CauHoi != null ? new CauHoiWithCauTraLoiDto
                    {
                        MaCauHoi = ct.CauHoi.MaCauHoi,
                        MaPhan = ct.CauHoi.MaPhan,
                        TenPhan = ct.CauHoi.Phan?.TenPhan,
                        MaSoCauHoi = ct.CauHoi.MaSoCauHoi,
                        NoiDung = ct.CauHoi.NoiDung,
                        HoanVi = ct.CauHoi.HoanVi,
                        CapDo = ct.CauHoi.CapDo,
                        SoCauHoiCon = ct.CauHoi.SoCauHoiCon,
                        DoPhanCach = ct.CauHoi.DoPhanCach,
                        MaCauHoiCha = ct.CauHoi.MaCauHoiCha,
                        XoaTam = ct.CauHoi.XoaTam,
                        SoLanDuocThi = ct.CauHoi.SoLanDuocThi,
                        SoLanDung = ct.CauHoi.SoLanDung,
                        NgayTao = ct.CauHoi.NgayTao,
                        NgaySua = ct.CauHoi.NgayCapNhat,
                        CLO = ct.CauHoi.CLO,
                        LoaiCauHoi = ct.CauHoi.LoaiCauHoi,
                        CauHoiCons = ct.CauHoi.CauHoiCons?.Select(MapChildToDto).ToList() ?? new(),
                        CauTraLois = ct.CauHoi.CauTraLois?.Select(ctl => new CauTraLoiDto
                        {
                            // map các field của CauTraLoi
                            MaCauTraLoi = ctl.MaCauTraLoi,
                            NoiDung = ctl.NoiDung,
                            LaDapAn = ctl.LaDapAn,
                            // các field khác...
                        }).ToList() ?? new()
                    } : null!
                }).ToList()
        };
    }
    private CauHoiDto MapChildToDto(CauHoi child)
    {
        if (child == null)
            return null!;

        return new CauHoiDto
        {
            MaCauHoi = child.MaCauHoi,
            MaPhan = child.MaPhan,
            TenPhan = child.Phan?.TenPhan,
            MaSoCauHoi = child.MaSoCauHoi,
            NoiDung = child.NoiDung,
            HoanVi = child.HoanVi,
            CapDo = child.CapDo,
            SoCauHoiCon = child.SoCauHoiCon,
            DoPhanCach = child.DoPhanCach,
            MaCauHoiCha = child.MaCauHoiCha,
            XoaTam = child.XoaTam ?? false,
            SoLanDuocThi = child.SoLanDuocThi,
            SoLanDung = child.SoLanDung,
            NgayTao = child.NgayTao,
            NgaySua = child.NgayCapNhat,
            CLO = child.CLO,
            LoaiCauHoi = child.LoaiCauHoi ?? string.Empty,

            // Câu trả lời của câu hỏi con này
            CauTraLois = child.CauTraLois
                .Select(ctl => new CauTraLoiDto
                {
                    MaCauTraLoi = ctl.MaCauTraLoi,
                    MaCauHoi = ctl.MaCauHoi,
                    NoiDung = ctl.NoiDung,
                    LaDapAn = ctl.LaDapAn,
                    ThuTu = ctl.ThuTu,
                    // Thêm các field khác của CauTraLoiDto nếu có
                    // Ví dụ: GhepNoiId = ctl.GhepNoiId, ...
                })
                .OrderBy(ctl => ctl.ThuTu) // sắp xếp câu trả lời theo thứ tự nếu cần
                .ToList() ?? new List<CauTraLoiDto>()
        };
    }

    /// <summary>
    /// Kiểm tra xem có đủ câu hỏi để rút trích theo ma trận không.
    /// </summary>
    private async Task<(bool Success, string Message, int AvailableQuestions)>
        CheckAvailableQuestionsPerPartAsync(MaTranDto maTran, Guid maMonHoc)
    {
        int totalAvailable = 0;

        foreach (var part in maTran.Parts ?? new List<PartDto>())
        {
            int partRequired = part.NumQuestions;
            int partAvailable = 0;

            var dbPart = await _phanRepository.FirstOrDefaultAsync(x => x.MaPhan == part.MaPhan);
            string partName = dbPart?.TenPhan ?? part.MaPhan.ToString();

            // Sử dụng MatrixCells nếu có
            if (part.MatrixCells != null && part.MatrixCells.Any())
            {
                foreach (var cell in part.MatrixCells)
                {
                    EnumCLO cloEnum = (EnumCLO)cell.Clo;
                    int required = cell.Num;
                    int available = 0;

                    // TN, MN, DT, GN: đếm câu hỏi cha
                    if (cell.Loai == "TN" || cell.Loai == "MN" ||
                        cell.Loai == "DT" || cell.Loai == "GN")
                    {
                        available = await _cauHoiRepository.CountAsync(ch =>
                            ch.MaPhan == part.MaPhan &&
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            ch.LoaiCauHoi == cell.Loai
                        );

                        if (available < required)
                        {
                            return (false,
                                $"Không đủ câu hỏi cho phần {partName}, CLO {cell.Clo}, loại {cell.Loai}. " +
                                $"Yêu cầu: {required} câu hỏi, có: {available} câu hỏi.",
                                totalAvailable);
                        }
                    }
                    // TL, NH: đếm tổng số câu hỏi con
                    else if (cell.Loai == "TL" || cell.Loai == "NH")
                    {
                        var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                            ch.MaPhan == part.MaPhan &&
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            ch.LoaiCauHoi == cell.Loai
                        );

                        foreach (var parent in parentQuestions)
                        {
                            var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );
                            available += childrenCount;
                        }

                        int requiredChildren = required * (cell.SubQuestionCount ?? 1);

                        if (available < requiredChildren)
                        {
                            return (false,
                                $"Không đủ câu hỏi cho phần {partName}, CLO {cell.Clo}, loại {cell.Loai}. " +
                                $"Yêu cầu: {requiredChildren} câu hỏi con, có: {available} câu hỏi con.",
                                totalAvailable);
                        }
                    }

                    partAvailable += available;
                }
            }
            else
            {
                // Fallback: logic cũ
                foreach (var clo in part.Clos ?? new List<CloDto>())
                {
                    foreach (var questionType in part.QuestionTypes ?? new List<QuestionTypeDto>())
                    {
                        int required = Math.Min(clo.Num, questionType.Num);
                        EnumCLO cloEnum = (EnumCLO)clo.Clo;
                        int available = 0;

                        // TN, MN, DT, GN: đếm câu hỏi cha
                        if (questionType.Loai == "TN" || questionType.Loai == "MN" ||
                            questionType.Loai == "DT" || questionType.Loai == "GN")
                        {
                            available = await _cauHoiRepository.CountAsync(ch =>
                            ch.MaPhan == part.MaPhan &&
                                ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                                ch.MaCauHoiCha == null &&
                                ch.CLO == cloEnum &&
                            (
                                (questionType.Loai == "TN" && ch.LoaiCauHoi == "TN") ||
                                (questionType.Loai == "DT" && ch.LoaiCauHoi == "DT") ||
                                (questionType.Loai == "GN" && ch.LoaiCauHoi == "GN") ||
                                    (questionType.Loai == "MN" && ch.LoaiCauHoi != null && ch.LoaiCauHoi.StartsWith("MN") && ch.LoaiCauHoi != "MN")
                                )
                            );
                        }
                        // TL, NH: đếm tổng số câu hỏi con trong các câu hỏi cha
                        else if (questionType.Loai == "TL" || questionType.Loai == "NH")
                        {
                            // Lấy tất cả câu hỏi cha có CLO phù hợp
                            var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                                ch.MaPhan == part.MaPhan &&
                                ch.Phan != null &&
                                ch.Phan.MaMonHoc == maMonHoc &&
                                ch.XoaTam == false &&
                                ch.MaCauHoiCha == null &&
                                ch.CLO == cloEnum &&
                                (
                                    (questionType.Loai == "TL" && ch.LoaiCauHoi == "TL") ||
                                    (questionType.Loai == "NH" && ch.LoaiCauHoi == "NH")
                                )
                            );

                            // Đếm tổng số câu hỏi con
                            foreach (var parent in parentQuestions)
                            {
                                var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                                    ch.MaCauHoiCha == parent.MaCauHoi &&
                                    ch.XoaTam == false
                                );
                                available += childrenCount;
                            }
                        }

                        if (available < required)
                        {
                            string countingType = (questionType.Loai == "TL" || questionType.Loai == "NH")
                                ? "câu hỏi con"
                                : "câu hỏi cha";
                            return (false,
                                $"Không đủ câu hỏi cho phần {partName}, CLO {clo.Clo}, loại {questionType.Loai}. " +
                                $"Yêu cầu: {required} {countingType}, có: {available} {countingType}.",
                                totalAvailable);
                        }

                        partAvailable += available;
                    }
                }
            }

            if (partAvailable < partRequired)
            {
                return (false,
                    $"Không đủ câu hỏi cho phần {partName}. Yêu cầu: {partRequired}, có: {partAvailable}.",
                    totalAvailable);
            }

            totalAvailable += partAvailable;
        }

        if (totalAvailable < maTran.TotalQuestions)
        {
            return (false,
                $"Không đủ tổng số câu hỏi cho đề thi. Yêu cầu: {maTran.TotalQuestions}, có: {totalAvailable}",
                totalAvailable);
        }

        return (true, "Đủ câu hỏi theo từng phần.", totalAvailable);
    }

    private async Task<(bool Success, string Message, int AvailableQuestions)>
        CheckAvailableQuestionsNoPartAsync(MaTranDto maTran, Guid maMonHoc)
    {
        int totalAvailable = 0;

        // Sử dụng MatrixCells nếu có (dữ liệu chi tiết từng ô)
        if (maTran.MatrixCells != null && maTran.MatrixCells.Any())
        {
            foreach (var cell in maTran.MatrixCells)
            {
                EnumCLO cloEnum = (EnumCLO)cell.Clo;
                int required = cell.Num;
                int available = 0;

                // TN, MN, DT, GN: đếm câu hỏi cha
                if (cell.Loai == "TN" || cell.Loai == "MN" ||
                    cell.Loai == "DT" || cell.Loai == "GN")
                {
                    available = await _cauHoiRepository.CountAsync(ch =>
                        ch.Phan != null &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false &&
                        ch.MaCauHoiCha == null &&
                        ch.CLO == cloEnum &&
                        ch.LoaiCauHoi == cell.Loai
                    );

                    if (available < required)
                    {
                        return (false,
                            $"Không đủ câu hỏi cho CLO {cell.Clo}, loại {cell.Loai}. " +
                            $"Yêu cầu: {required} câu hỏi, có: {available} câu hỏi.",
                            totalAvailable);
                    }
                }
                // TL, NH: đếm tổng số câu hỏi con trong các câu hỏi cha
                else if (cell.Loai == "TL" || cell.Loai == "NH")
                {
                    var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                        ch.Phan != null &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false &&
                        ch.MaCauHoiCha == null &&
                        ch.CLO == cloEnum &&
                        ch.LoaiCauHoi == cell.Loai
                    );

                    // Đếm tổng số câu hỏi con
                    foreach (var parent in parentQuestions)
                    {
                        var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                            ch.MaCauHoiCha == parent.MaCauHoi &&
                            ch.XoaTam == false
                        );
                        available += childrenCount;
                    }

                    // Tính số câu hỏi con cần thiết
                    int requiredChildren = required * (cell.SubQuestionCount ?? 1);

                    if (available < requiredChildren)
                    {
                        return (false,
                            $"Không đủ câu hỏi cho CLO {cell.Clo}, loại {cell.Loai}. " +
                            $"Yêu cầu: {requiredChildren} câu hỏi con ({required} câu cha × {cell.SubQuestionCount ?? 1} câu con), " +
                            $"có: {available} câu hỏi con.",
                            totalAvailable);
                    }
                }

                totalAvailable += available;
            }
        }
        else
        {
            // Fallback: logic cũ cho tương thích ngược
            foreach (var clo in maTran.Clos ?? new List<CloDto>())
            {
                foreach (var questionType in maTran.QuestionTypes ?? new List<QuestionTypeDto>())
                {
                    int required = Math.Min(clo.Num, questionType.Num);
                    EnumCLO cloEnum = (EnumCLO)clo.Clo;
                    int available = 0;

                    // TN, MN, DT, GN: đếm câu hỏi cha
                    if (questionType.Loai == "TN" || questionType.Loai == "MN" ||
                        questionType.Loai == "DT" || questionType.Loai == "GN")
                    {
                        available = await _cauHoiRepository.CountAsync(ch =>
                            ch.Phan != null &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                        (
                            (questionType.Loai == "TN" && ch.LoaiCauHoi == "TN") ||
                            (questionType.Loai == "DT" && ch.LoaiCauHoi == "DT") ||
                                (questionType.Loai == "GN" && ch.LoaiCauHoi == "GN") ||
                                (questionType.Loai == "MN" && ch.LoaiCauHoi == "MN")
                            )
                        );
                    }
                    // TL, NH: đếm tổng số câu hỏi con trong các câu hỏi cha
                    else if (questionType.Loai == "TL" || questionType.Loai == "NH")
                    {
                        // Lấy tất cả câu hỏi cha có CLO phù hợp
                        var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            (
                                (questionType.Loai == "TL" && ch.LoaiCauHoi == "TL") ||
                                (questionType.Loai == "NH" && ch.LoaiCauHoi == "NH")
                            )
                        );

                        // Đếm tổng số câu hỏi con
                        foreach (var parent in parentQuestions)
                        {
                            var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );
                            available += childrenCount;
                        }
                    }

                    if (available < required)
                    {
                        string countingType = (questionType.Loai == "TL" || questionType.Loai == "NH")
                            ? "câu hỏi con"
                            : "câu hỏi cha";
                        return (false,
                            $"Không đủ câu hỏi cho CLO {clo.Clo}, loại {questionType.Loai}. " +
                            $"Yêu cầu: {required} {countingType}, có: {available} {countingType}.",
                            totalAvailable);
                    }

                    totalAvailable += available;
                }
            }
        }

        if (totalAvailable < maTran.TotalQuestions)
        {
            return (false,
                $"Không đủ câu hỏi tổng thể. Yêu cầu: {maTran.TotalQuestions}, có: {totalAvailable}.",
                totalAvailable);
        }

        return (true, "Đủ câu hỏi không theo phần.", totalAvailable);
    }

    public async Task<(bool Success, string Message, int AvailableQuestions)>
        CheckAvailableQuestionsAsync(MaTranDto maTran, Guid maMonHoc)
    {
        if (maTran.CloPerPart)
        {
            return await CheckAvailableQuestionsPerPartAsync(maTran, maMonHoc);
        }
        else
        {
            return await CheckAvailableQuestionsNoPartAsync(maTran, maMonHoc);
        }
    }


    private async Task<List<ChiTietDeThi>> SelectQuestionsPerPartAsync
        (MaTranDto maTran, Guid maMonHoc, Guid maDeThi)
    {
        var chiTietDeThis = new List<ChiTietDeThi>();
        int thuTu = 1;
        var usedQuestionIds = new HashSet<Guid>(); // Để tránh trùng lặp

        foreach (var part in maTran.Parts)
        {
            int remainingQuestions = part.NumQuestions;
            foreach (var clo in part.Clos)
            {
                foreach (var questionType in part.QuestionTypes)
                {
                    int numQuestions = Math.Min(clo.Num, Math.Min(questionType.Num, remainingQuestions));
                    if (numQuestions <= 0) continue;

                    EnumCLO cloEnum = (EnumCLO)clo.Clo;
                    List<CauHoi> selected = new List<CauHoi>();

                    // TN, MN, DT, GN: chọn câu hỏi cha
                    if (questionType.Loai == "TN" || questionType.Loai == "MN" ||
                        questionType.Loai == "DT" || questionType.Loai == "GN")
                    {
                        var questions = await _cauHoiRepository.FindAsync(ch =>
                            ch.MaPhan == part.MaPhan &&
                                ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                                ch.MaCauHoiCha == null &&
                                ch.CLO == cloEnum &&
                                !usedQuestionIds.Contains(ch.MaCauHoi) &&
                            (
                                (questionType.Loai == "TN" && ch.LoaiCauHoi == "TN") ||
                                (questionType.Loai == "DT" && ch.LoaiCauHoi == "DT") ||
                                (questionType.Loai == "GN" && ch.LoaiCauHoi == "GN") ||
                                    (questionType.Loai == "MN" && ch.LoaiCauHoi != null && ch.LoaiCauHoi.StartsWith("MN") && ch.LoaiCauHoi != "MN")
                                )
                            );

                        selected = questions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();
                    }
                    // TL, NH: chọn câu hỏi cha và đếm theo số câu con
                    else if (questionType.Loai == "TL" || questionType.Loai == "NH")
                    {
                        var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                            ch.MaPhan == part.MaPhan &&
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            !usedQuestionIds.Contains(ch.MaCauHoi) &&
                            (
                                (questionType.Loai == "TL" && ch.LoaiCauHoi == "TL") ||
                                (questionType.Loai == "NH" && ch.LoaiCauHoi == "NH")
                        )
                    );

                        // Chọn các câu hỏi cha sao cho tổng số câu con đạt yêu cầu
                        var shuffledParents = parentQuestions.OrderBy(x => Guid.NewGuid()).ToList();
                        int totalChildren = 0;

                        foreach (var parent in shuffledParents)
                        {
                            var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );

                            if (totalChildren + childrenCount <= numQuestions || selected.Count == 0)
                            {
                                selected.Add(parent);
                                totalChildren += childrenCount;
                                if (totalChildren >= numQuestions) break;
                            }
                        }
                    }

                    // Thêm câu hỏi cha vào danh sách (KHÔNG thêm câu hỏi con)
                    foreach (var parent in selected)
                    {
                        usedQuestionIds.Add(parent.MaCauHoi);

                        // Chỉ thêm câu hỏi cha
                        chiTietDeThis.Add(new ChiTietDeThi
                        {
                            MaDeThi = maDeThi,
                            MaPhan = part.MaPhan,
                            MaCauHoi = parent.MaCauHoi,
                            ThuTu = thuTu++
                        });
                    }

                    // Cập nhật remainingQuestions
                    if (questionType.Loai == "TL" || questionType.Loai == "NH")
                    {
                        // Đếm số câu con đã chọn
                        int childrenCount = 0;
                        foreach (var parent in selected)
                        {
                            childrenCount += await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );
                        }
                        remainingQuestions -= childrenCount;
                    }
                    else
                    {
                        remainingQuestions -= selected.Count;
                    }

                    if (remainingQuestions <= 0) break;
                }

                if (remainingQuestions <= 0) break;
            }
        }

        return chiTietDeThis;
    }

    private async Task<List<ChiTietDeThi>> SelectQuestionsNoPartAsync(
        MaTranDto maTran, Guid maMonHoc, Guid maDeThi)
    {
        var chiTietDeThis = new List<ChiTietDeThi>();
        int thuTu = 1;
        var usedQuestionIds = new HashSet<Guid>(); // Để tránh trùng lặp

        // Sử dụng MatrixCells nếu có (dữ liệu chi tiết từng ô)
        if (maTran.MatrixCells != null && maTran.MatrixCells.Any())
        {
            foreach (var cell in maTran.MatrixCells)
            {
                EnumCLO cloEnum = (EnumCLO)cell.Clo;
                int numQuestions = cell.Num;

                List<CauHoi> selected = new List<CauHoi>();

                // TN, MN, DT, GN: chọn câu hỏi cha
                if (cell.Loai == "TN" || cell.Loai == "MN" ||
                    cell.Loai == "DT" || cell.Loai == "GN")
                {
                    var questions = await _cauHoiRepository.FindAsync(ch =>
                        ch.Phan != null &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false &&
                        ch.MaCauHoiCha == null &&
                        ch.CLO == cloEnum &&
                        !usedQuestionIds.Contains(ch.MaCauHoi) &&
                        ch.LoaiCauHoi == cell.Loai
                    );

                    selected = questions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();
                }
                // TL, NH: chọn câu hỏi cha (số câu cha = numQuestions)
                else if (cell.Loai == "TL" || cell.Loai == "NH")
                {
                    var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                        ch.Phan != null &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false &&
                        ch.MaCauHoiCha == null &&
                        ch.CLO == cloEnum &&
                        !usedQuestionIds.Contains(ch.MaCauHoi) &&
                        ch.LoaiCauHoi == cell.Loai
                    );

                    // Chọn numQuestions câu cha ngẫu nhiên
                    selected = parentQuestions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();
                }

                // Thêm câu hỏi cha vào danh sách
                foreach (var parent in selected)
                {
                    usedQuestionIds.Add(parent.MaCauHoi);
                    chiTietDeThis.Add(new ChiTietDeThi
                    {
                        MaDeThi = maDeThi,
                        MaPhan = parent.MaPhan,
                        MaCauHoi = parent.MaCauHoi,
                        ThuTu = thuTu++
                    });
                }
            }
        }
        else
        {
            // Fallback: logic cũ cho tương thích ngược
            if (maTran.QuestionTypes == null || !maTran.QuestionTypes.Any())
                return chiTietDeThis;

            var quota = maTran.QuestionTypes.ToDictionary(x => x.Loai, x => x.Num);

            foreach (var clo in maTran.Clos ?? new List<CloDto>())
            {
                EnumCLO cloEnum = (EnumCLO)clo.Clo;
                int remainingNeed = clo.Num;

                // Xử lý từng loại câu hỏi trong quota
                foreach (var questionType in maTran.QuestionTypes)
                {
                    if (quota[questionType.Loai] <= 0 || remainingNeed <= 0) continue;

                    int numQuestions = Math.Min(remainingNeed, quota[questionType.Loai]);
                    if (numQuestions <= 0) continue;

                    List<CauHoi> selected = new List<CauHoi>();

                    // TN, MN, DT, GN: chọn câu hỏi cha
                    if (questionType.Loai == "TN" || questionType.Loai == "MN" ||
                        questionType.Loai == "DT" || questionType.Loai == "GN")
                    {
                        var questions = await _cauHoiRepository.FindAsync(ch =>
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            !usedQuestionIds.Contains(ch.MaCauHoi) &&
                            (
                                (questionType.Loai == "TN" && ch.LoaiCauHoi == "TN") ||
                                (questionType.Loai == "DT" && ch.LoaiCauHoi == "DT") ||
                                (questionType.Loai == "GN" && ch.LoaiCauHoi == "GN") ||
                                (questionType.Loai == "MN" && ch.LoaiCauHoi == "MN")
                            )
                        );

                        selected = questions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();
                    }
                    // TL, NH: chọn câu hỏi cha và đếm theo số câu con
                    else if (questionType.Loai == "TL" || questionType.Loai == "NH")
                    {
                        var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                            ch.Phan != null &&
                            ch.Phan.MaMonHoc == maMonHoc &&
                            ch.XoaTam == false &&
                            ch.MaCauHoiCha == null &&
                            ch.CLO == cloEnum &&
                            !usedQuestionIds.Contains(ch.MaCauHoi) &&
                            (
                                (questionType.Loai == "TL" && ch.LoaiCauHoi == "TL") ||
                                (questionType.Loai == "NH" && ch.LoaiCauHoi == "NH")
                            )
                        );

                        // Chọn các câu hỏi cha sao cho tổng số câu con đạt yêu cầu
                        var shuffledParents = parentQuestions.OrderBy(x => Guid.NewGuid()).ToList();
                        int totalChildren = 0;

                        foreach (var parent in shuffledParents)
                        {
                            var childrenCount = await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );

                            if (totalChildren + childrenCount <= numQuestions || selected.Count == 0)
                            {
                                selected.Add(parent);
                                totalChildren += childrenCount;
                                if (totalChildren >= numQuestions) break;
                            }
                        }
                    }

                    // Thêm câu hỏi cha vào danh sách (KHÔNG thêm câu hỏi con)
                    foreach (var parent in selected)
                    {
                        usedQuestionIds.Add(parent.MaCauHoi);

                        // Chỉ thêm câu hỏi cha
                        chiTietDeThis.Add(new ChiTietDeThi
                        {
                            MaDeThi = maDeThi,
                            MaPhan = parent.MaPhan,
                            MaCauHoi = parent.MaCauHoi,
                            ThuTu = thuTu++
                        });
                    }

                    // Cập nhật quota và remainingNeed
                    if (questionType.Loai == "TL" || questionType.Loai == "NH")
                    {
                        // Đếm số câu con đã chọn
                        int childrenCount = 0;
                        foreach (var parent in selected)
                        {
                            childrenCount += await _cauHoiRepository.CountAsync(ch =>
                                ch.MaCauHoiCha == parent.MaCauHoi &&
                                ch.XoaTam == false
                            );
                        }
                        quota[questionType.Loai] -= childrenCount;
                        remainingNeed -= childrenCount;
                    }
                    else
                    {
                        quota[questionType.Loai] -= selected.Count;
                        remainingNeed -= selected.Count;
                    }

                    // Nếu đã đủ số lượng cho CLO này, dừng lại
                    if (remainingNeed <= 0) break;
                }
            }
        }

        return chiTietDeThis;
    }

    private async Task<List<ChiTietDeThi>> SelectRandomQuestionsAsync(MaTranDto maTran, Guid maMonHoc, Guid maDeThi)
    {
        if (maTran.CloPerPart)
        {
            return await SelectQuestionsPerPartAsync(maTran, maMonHoc, maDeThi);
        }
        else
        {
            return await SelectQuestionsNoPartAsync(maTran, maMonHoc, maDeThi);
        }
    }

    /// <summary>
    /// Rút trích đề thi dựa trên yêu cầu rút trích.
    /// </summary>
    public async Task<(bool Success, string Message, Guid? MaDeThi)> RutTrichDeThiAsync(Guid maYeuCau, string tenDeThi)
    {
        try
        {
            // Lấy yêu cầu rút trích
            var yeuCau = await _yeuCauRutTrichRepository.GetByIdAsync(maYeuCau);
            if (yeuCau == null)
            {
                return (false, "Yêu cầu rút trích không tồn tại.", null);
            }

            // Deserialize ma trận từ JSON
            if (string.IsNullOrWhiteSpace(yeuCau.MaTran))
            {
                return (false, "Ma trận không được để trống.", null);
            }

            MaTranDto maTran;
            try
            {
                maTran = JsonConvert.DeserializeObject<MaTranDto>(yeuCau.MaTran);
            }
            catch (JsonException ex)
            {
                return (false, $"Lỗi khi phân tích ma trận: {ex.Message}", null);
            }

            if (maTran == null)
            {
                return (false, "Ma trận không hợp lệ.", null);
            }

            // Kiểm tra đủ câu hỏi
            var checkResult = await CheckAvailableQuestionsAsync(maTran, yeuCau.MaMonHoc);
            if (!checkResult.Success)
            {
                return (false, checkResult.Message, null);
            }

            // Tạo đề thi mới
            var maDeThi = Guid.NewGuid();
            var chiTietDeThis = await SelectRandomQuestionsAsync(maTran, yeuCau.MaMonHoc, maDeThi);

            // Kiểm tra số lượng câu hỏi
            // if (chiTietDeThis.Count != maTran.TotalQuestions)
            // {
            //     return (false,
            //         $"Số câu hỏi rút trích ({chiTietDeThis.Count}) không khớp với yêu cầu ({maTran.TotalQuestions}).",
            //         null);
            // }

            // Tính tổng số câu hỏi thực tế (bao gồm cả câu con)
            // Câu đơn (SoCauHoiCon == 0 hoặc null) = 1 câu
            // Câu nhóm (SoCauHoiCon > 0) = SoCauHoiCon câu
            int soCauHoiThucTe = 0;
            foreach (var ct in chiTietDeThis)
            {
                var cauHoi = await _cauHoiRepository.GetByIdAsync(ct.MaCauHoi);
                if (cauHoi != null)
                {
                    if (cauHoi.SoCauHoiCon > 0)
                    {
                        // Câu hỏi nhóm: đếm số câu con
                        soCauHoiThucTe += cauHoi.SoCauHoiCon;
                    }
                    else
                    {
                        // Câu hỏi đơn: đếm là 1
                        soCauHoiThucTe += 1;
                    }
                }
                else
                {
                    // Nếu không tìm thấy câu hỏi, tính là 1
                    soCauHoiThucTe += 1;
                }
            }

            // Tạo đề thi
            var deThi = new DeThi
            {
                MaDeThi = maDeThi,
                MaMonHoc = yeuCau.MaMonHoc,
                TenDeThi = tenDeThi,
                DaDuyet = false,
                SoCauHoi = soCauHoiThucTe, // Sử dụng số câu hỏi thực tế đã tính
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow,
                ChiTietDeThis = chiTietDeThis
            };

            // Lưu đề thi
            await _deThiRepository.AddAsync(deThi);

            // Cập nhật trạng thái yêu cầu rút trích
            yeuCau.DaXuLy = true;
            yeuCau.NgayXuLy = DateTime.UtcNow;

            await _yeuCauRutTrichRepository.UpdateAsync(yeuCau);

            return (true, "Rút trích đề thi thành công.", maDeThi);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi rút trích đề thi: {ex.Message}", null);
        }
    }
}