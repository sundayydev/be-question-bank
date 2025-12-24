using System.Xml;
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
    private readonly ICauHoiRepository _cauHoiRepository;

    public YeuCauRutTrichService(
        IYeuCauRutTrichRepository repository, 
        IPhanRepository phanRepository,
        ICauHoiRepository cauHoiRepository)
    {
        _repository = repository;
        _phanRepository = phanRepository;
        _cauHoiRepository = cauHoiRepository;
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
                    MaPhan = maPhan,
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

    /// <summary>
    /// Tạo yêu cầu rút trích TỰ LUẬN từ ma trận JSON (frontend gửi trực tiếp)
    /// </summary>
    public async Task<(bool Success, string Message, Guid MaYeuCau)> CreateTuLuanRequestAsync(
        Guid maNguoiDung,
        Guid maMonHoc,
        MaTranTuLuan maTranTuLuan,
        string? noiDungRutTrich = null,
        string? ghiChu = null)
    {
        try
        {
            if (maTranTuLuan == null)
                return (false, "Ma trận tự luận không được để trống.", Guid.Empty);

            if (maTranTuLuan.Parts == null || !maTranTuLuan.Parts.Any() ||
                !maTranTuLuan.Parts.Any(p => p.Clos?.Any() == true))
                return (false, "Ma trận tự luận phải có ít nhất một phần và một yêu cầu CLO.", Guid.Empty);

            // Tính tổng số câu hỏi dự kiến từ ma trận
            // Nếu có SubQuestionCount: num × subQuestionCount
            // Nếu không có SubQuestionCount: num × 1 (giả sử mỗi câu cha = 1 câu)
            var expectedTotalQuestions = maTranTuLuan.Parts
                .Where(p => p.Clos != null)
                .SelectMany(p => p.Clos!)
                .Sum(c => c.Num * Math.Max(1, c.SubQuestionCount ?? 1));

            if (expectedTotalQuestions <= 0)
                return (false, "Ma trận phải yêu cầu ít nhất 1 câu hỏi.", Guid.Empty);

            // VALIDATION CHO TỰ LUẬN: 
            // totalQuestions phải khớp với tổng dự kiến từ ma trận
            if (maTranTuLuan.TotalQuestions != expectedTotalQuestions)
            {
                return (false, 
                    $"Tổng câu hỏi (totalQuestions = {maTranTuLuan.TotalQuestions}) không khớp với " +
                    $"tổng dự kiến từ ma trận ({expectedTotalQuestions} câu). " +
                    $"Tính toán: Tổng của (num × subQuestionCount) cho tất cả CLO requests.", 
                    Guid.Empty);
            }

            // Kiểm tra xem có đủ câu hỏi trong database không
            var validationResult = await ValidateMaTranTuLuanAsync(maMonHoc, maTranTuLuan);
            if (!validationResult.IsValid)
                return (false, validationResult.ErrorMessage, Guid.Empty);

            var entity = new YeuCauRutTrich
            {
                MaYeuCau = Guid.NewGuid(),
                MaNguoiDung = maNguoiDung,
                MaMonHoc = maMonHoc,
                NoiDungRutTrich = noiDungRutTrich ?? "Yêu cầu rút trích đề tự luận có phần",
                GhiChu = ghiChu,
                NgayYeuCau = DateTime.UtcNow,
                DaXuLy = false,
                MaTran = JsonConvert.SerializeObject(maTranTuLuan)
            };

            await _repository.AddAsync(entity);

            return (true, "Tạo yêu cầu rút trích tự luận thành công.", entity.MaYeuCau);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi tạo yêu cầu tự luận: {ex.Message}", Guid.Empty);
        }
    }

    /// <summary>
    /// Kiểm tra xem có đủ câu hỏi tự luận trong database để rút trích theo ma trận
    /// </summary>
    private async Task<(bool IsValid, string ErrorMessage)> ValidateMaTranTuLuanAsync(
        Guid maMonHoc,
        MaTranTuLuan maTran)
    {
        var errorMessages = new List<string>();

        foreach (var part in maTran.Parts ?? new List<PartTuLuanDto>())
        {
            // Lấy tên phần từ database để hiển thị trong thông báo lỗi
            var phan = await _phanRepository.GetByIdAsync(part.Part);
            var tenPhan = phan?.TenPhan ?? part.Part.ToString();

            foreach (var cloReq in part.Clos ?? new List<CloDto>())
            {
                var clo = (BeQuestionBank.Shared.Enums.EnumCLO)cloReq.Clo;
                var numNeeded = cloReq.Num;
                var subQuestionCount = cloReq.SubQuestionCount;

                // Đếm số câu hỏi tự luận có sẵn cho phần và CLO này
                var availableQuestionsQuery = await _cauHoiRepository.FindAsync(ch =>
                    ch.MaPhan == part.Part &&
                    ch.MaCauHoiCha == null && // Chỉ lấy câu cha
                    ch.LoaiCauHoi == "TL" &&
                    ch.CLO == clo &&
                    ch.XoaTam == false);

                // Nếu có chỉ định SubQuestionCount, chỉ lấy câu có SoCauHoiCon phù hợp
                var questionsList = subQuestionCount.HasValue
                    ? availableQuestionsQuery.Where(ch => 
                        (ch.SoCauHoiCon <= 1 && subQuestionCount.Value == 1) ||  // Câu không có con
                        (ch.SoCauHoiCon == subQuestionCount.Value)                // Câu có đúng số con
                    ).ToList()
                    : availableQuestionsQuery.ToList();

                var availableQuestionCount = questionsList.Count;

                if (availableQuestionCount == 0)
                {
                    var subQuestInfo = subQuestionCount.HasValue 
                        ? $" (yêu cầu {subQuestionCount.Value} câu con)" 
                        : "";
                    errorMessages.Add($"Phần '{tenPhan}' - CLO {cloReq.Clo}{subQuestInfo}: Không có câu hỏi tự luận nào trong database.");
                }
                else if (availableQuestionCount < numNeeded)
                {
                    var subQuestInfo = subQuestionCount.HasValue 
                        ? $" với {subQuestionCount.Value} câu con" 
                        : "";
                    errorMessages.Add($"Phần '{tenPhan}' - CLO {cloReq.Clo}: Cần {numNeeded} câu hỏi{subQuestInfo} nhưng chỉ có {availableQuestionCount} câu trong database.");
                }
            }
        }

        if (errorMessages.Any())
        {
            var fullMessage = "Không thể tạo yêu cầu rút trích do thiếu câu hỏi:\n" + 
                              string.Join("\n", errorMessages);
            return (false, fullMessage);
        }

        return (true, string.Empty);
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
