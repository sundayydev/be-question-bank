using System.Xml;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace BEQuestionBank.Core.Services;

public class YeuCauRutTrichService
{
    private readonly IYeuCauRutTrichRepository _repository;

    public YeuCauRutTrichService(IYeuCauRutTrichRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Lấy yêu cầu theo ID (basic info only).
    /// </summary>
    public async Task<YeuCauRutTrichDto?> GetBasicByIdAsync(Guid id)
    {
        var yeuCau = await _repository.GetByIdAsync(id);
        return yeuCau != null ? MapToBasicDto(yeuCau) : null;
    }

    /// <summary>
    /// Lấy tất cả yêu cầu (basic info).
    /// </summary>
    public async Task<IEnumerable<YeuCauRutTrichDto>> GetAllBasicAsync()
    {
        var yeuCaus = await _repository.GetAllAsync();
        return yeuCaus.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// Lấy danh sách yêu cầu theo người dùng.
    /// </summary>
    public async Task<IEnumerable<YeuCauRutTrichDto>> GetByMaNguoiDungAsync(Guid maNguoiDung)
    {
        var yeuCaus = await _repository.GetByMaNguoiDungAsync(maNguoiDung);
        return yeuCaus.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// Lấy danh sách yêu cầu theo môn học.
    /// </summary>
    public async Task<IEnumerable<YeuCauRutTrichDto>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        var yeuCaus = await _repository.GetByMaMonHocAsync(maMonHoc);
        return yeuCaus.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// Lấy danh sách yêu cầu theo trạng thái xử lý.
    /// </summary>
    public async Task<IEnumerable<YeuCauRutTrichDto>> GetByTrangThaiAsync(bool? daXuLy)
    {
        var yeuCaus = await _repository.GetByTrangThaiAsync(daXuLy);
        return yeuCaus.Select(MapToBasicDto).ToList();
    }

    /// <summary>
    /// Thêm mới yêu cầu.
    /// </summary>
    public async Task<(bool Success, string Message, Guid? MaYeuCau)> AddAsync(CreateYeuCauRutTrichDto dto)
    {
        try
        {
            var entity = new YeuCauRutTrich
            {
                MaYeuCau = Guid.NewGuid(),
                MaNguoiDung = dto.MaNguoiDung,
                MaMonHoc = dto.MaMonHoc,
                NoiDungRutTrich = dto.NoiDungRutTrich,
                GhiChu = dto.GhiChu,
                NgayYeuCau = DateTime.UtcNow,
                DaXuLy = dto.DaXuLy,
                MaTran = dto.MaTran
            };

            await _repository.AddAsync(entity);
            return (true, "Thêm yêu cầu thành công.", entity.MaYeuCau);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi thêm yêu cầu: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Cập nhật yêu cầu.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(Guid id, YeuCauRutTrichDto dto)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return (false, "Yêu cầu không tồn tại.");

            entity.NoiDungRutTrich = dto.NoiDungRutTrich ?? entity.NoiDungRutTrich;
            entity.GhiChu = dto.GhiChu ?? entity.GhiChu;
            entity.DaXuLy = dto.DaXuLy ?? entity.DaXuLy;
            entity.NgayXuLy = dto.DaXuLy == true ? DateTime.UtcNow : entity.NgayXuLy;

            await _repository.UpdateAsync(entity);
            return (true, "Cập nhật yêu cầu thành công.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi cập nhật yêu cầu: {ex.Message}");
        }
    }

    /// <summary>
    /// Xóa yêu cầu.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return (false, "Yêu cầu không tồn tại.");

            await _repository.DeleteAsync(entity);
            return (true, "Xóa yêu cầu thành công.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi xóa yêu cầu: {ex.Message}");
        }
    }

    /// <summary>
    /// Map sang DTO (basic).
    /// </summary>
    private YeuCauRutTrichDto MapToBasicDto(YeuCauRutTrich entity)
    {
        return new YeuCauRutTrichDto
        {
            MaYeuCau = entity.MaYeuCau,
            MaNguoiDung = entity.MaNguoiDung,
            MaMonHoc = entity.MaMonHoc,
            NoiDungRutTrich = entity.NoiDungRutTrich,
            GhiChu = entity.GhiChu,
            NgayYeuCau = entity.NgayYeuCau,
            NgayXuLy = entity.NgayXuLy,
            DaXuLy = entity.DaXuLy,
            TenNguoiDung = entity.NguoiDung?.HoTen,
            TenMonHoc = entity.MonHoc?.TenMonHoc,
            TenKhoa = entity.MonHoc?.Khoa?.TenKhoa,
            MaTran = entity.MaTran
        };
    }
}
