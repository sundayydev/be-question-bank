﻿using System.Xml;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;


namespace BEQuestionBank.Core.Services;

public class YeuCauRutTrichService
{
    private readonly IYeuCauRutTrichRepository _repository;
    private readonly IPhanRepository _phanRepository;

    public YeuCauRutTrichService(IYeuCauRutTrichRepository repository , IPhanRepository phanRepository)
    {
        _repository = repository;
        _phanRepository = phanRepository;
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
    public async Task<(bool Success, string Message, Guid MaYeuCau)> AddAsync(CreateYeuCauRutTrichDto dto)
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
                MaTran = JsonConvert.SerializeObject(dto.MaTran)
            };

            await _repository.AddAsync(entity);
            return (true, "Thêm yêu cầu thành công.", entity.MaYeuCau);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi thêm yêu cầu: {ex.Message}", Guid.Empty);
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
    public async Task<MaTranDto> ReadMaTranFromExcelAsync(string filePath, Guid maMonHoc)
    {
        // 1. Lấy map TenPhan -> MaPhan
        var phanList = await _phanRepository.FindAsync(p => p.MaMonHoc == maMonHoc);

        var dict = phanList.ToDictionary(
            (Phan p) => p.TenPhan.Trim().ToLower(),
            (Phan p) => p.MaPhan
        );

        var parts = new List<PartDto>();
        int totalQuestions = 0;

        // 2. Đọc Excel bằng NPOI
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            IWorkbook workbook = new XSSFWorkbook(fs);
            ISheet sheet = workbook.GetSheetAt(0);

            // Bắt đầu từ dòng 2 (row index 1), bỏ header
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var currentRow = sheet.GetRow(row);
                if (currentRow == null) continue;

                var tenPhan = currentRow.GetCell(0)?.ToString()?.Trim().ToLower();
                if (string.IsNullOrEmpty(tenPhan)) break;

                if (!dict.TryGetValue(tenPhan, out var maPhan))
                    throw new Exception($"Không tìm thấy MãPhần cho Tên phần: {tenPhan}");

                var part = new PartDto
                {
                    MaPhan = maPhan.ToString(),
                    NumQuestions = (int)(currentRow.GetCell(1)?.NumericCellValue ?? 0),
                    Clos = new List<CloDto>
                    {
                        new CloDto
                        {
                            Clo = (int)(currentRow.GetCell(2)?.NumericCellValue ?? 0),
                            Num = (int)(currentRow.GetCell(3)?.NumericCellValue ?? 0)
                        }
                    },
                    QuestionTypes = new List<QuestionTypeDto>
                    {
                        new QuestionTypeDto
                        {
                            Loai = currentRow.GetCell(4)?.ToString(),
                            Num = (int)(currentRow.GetCell(5)?.NumericCellValue ?? 0)
                        }
                    }
                };

                parts.Add(part);
                totalQuestions += part.NumQuestions;
            }
        }

        // 3. Build MaTranDto
        return new MaTranDto
        {
            TotalQuestions = totalQuestions,
            CloPerPart = true,
            Parts = parts
        };
    }
    // public async Task<MaTranDto> ReadMaTranFromExcelAndSaveAsync(
    //     string filePath,
    //     Guid maMonHoc,
    //     Guid maNguoiDung)
    // {
    //     // 1. Đọc file Excel thành DTO (dùng lại hàm cũ)
    //     var maTranDto = await ReadMaTranFromExcelAsync(filePath, maMonHoc);
    //
    //     // 2. Tạo yêu cầu rút trích
    //     var yeuCau = new YeuCauRutTrich
    //     {
    //         MaYeuCau = Guid.NewGuid(),
    //         MaMonHoc = maMonHoc,
    //         MaNguoiDung = maNguoiDung,
    //         MaTran = JsonConvert.SerializeObject(maTranDto),
    //         NgayYeuCau = DateTime.UtcNow,
    //         DaXuLy = false
    //     };
    //
    //     await _repository.AddAsync(yeuCau);
    //
    //     // 3. (tùy chọn) Tạo luôn DeThi trống
    //     var deThi = new DeThi
    //     {
    //         MaDeThi = Guid.NewGuid(),
    //         MaYeuCau = yeuCau.MaYeuCau,
    //         NgayTao = DateTime.UtcNow,
    //         TrangThai = "Mới tạo"
    //     };
    //
    //     await _deThiRepository.AddAsync(deThi);
    //
    //     // 4. Lưu thay đổi
    //     await _unitOfWork.SaveChangesAsync();
    //
    //     return maTranDto;
    // }

}
