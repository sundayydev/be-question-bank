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

namespace BEQuestionBank.Core.Services;

public class DeThiService
{
    private readonly IDeThiRepository _deThiRepository;

    public DeThiService(IDeThiRepository deThiRepository)
    {
        _deThiRepository = deThiRepository;
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
        var deThis = await _deThiRepository.GetAllAsync();
        return deThis.Select(MapToBasicDto).ToList();
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

            // ✅ Tạo ID trước
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
                    MaDeThi = maDeThi, // ✅ gán FK đúng
                    MaPhan = ct.MaPhan,
                    MaCauHoi = ct.MaCauHoi,
                    ThuTu = ct.ThuTu
                }).ToList()
            };

            // Save vào DB
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
                CauHoi = ct.CauHoi != null ? new CauHoiDto
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
                } : null
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
            ChiTietDeThis = deThi.ChiTietDeThis?.Select(ct => new ChiTietDeThiWithCauTraLoiDto
            {
                MaDeThi = ct.MaDeThi,
                MaPhan = ct.MaPhan,
                MaCauHoi = ct.MaCauHoi,
                ThuTu = ct.ThuTu,
                CauHoi = ct.CauHoi != null ? new CauHoiWithCauTraLoiDto
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
                    CauHoiCons = ct.CauHoi.CauHoiCons?.Select(chc => 
                        (CauHoiDto)new CauHoiWithCauTraLoiDto
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
                            CauTraLois = chc.CauTraLois?.Select(ctl => new CauTraLoiDto
                            {
                                MaCauTraLoi = ctl.MaCauTraLoi,
                                MaCauHoi = ctl.MaCauHoi,
                                NoiDung = ctl.NoiDung,
                                ThuTu = ctl.ThuTu,
                                HoanVi = ctl.HoanVi,
                                LaDapAn = ctl.LaDapAn
                            }).ToList() ?? new List<CauTraLoiDto>()
                        }).ToList() ?? new List<CauHoiDto>(),

                    CauTraLois = ct.CauHoi.CauTraLois?.Select(ctl => new CauTraLoiDto
                    {
                        MaCauTraLoi = ctl.MaCauTraLoi,
                        MaCauHoi = ctl.MaCauHoi,
                        NoiDung = ctl.NoiDung,
                        ThuTu = ctl.ThuTu,
                        HoanVi = ctl.HoanVi,
                        LaDapAn = ctl.LaDapAn
                    }).ToList() ?? new List<CauTraLoiDto>()
                } : null
            }).ToList() ?? new List<ChiTietDeThiWithCauTraLoiDto>()
        };
    }
}