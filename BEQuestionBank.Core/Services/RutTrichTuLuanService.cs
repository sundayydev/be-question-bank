// File: BEQuestionBank.Core.Services/RutTrichTuLuanService.cs

using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using Newtonsoft.Json;

namespace BEQuestionBank.Core.Services;

public class RutTrichTuLuanService
{
    private readonly ICauHoiRepository _cauHoiRepository;
    private readonly IDeThiRepository _deThiRepository;
    private readonly IYeuCauRutTrichRepository _yeuCauRutTrichRepository;

    public RutTrichTuLuanService(
        ICauHoiRepository cauHoiRepository,
        IDeThiRepository deThiRepository,
        IYeuCauRutTrichRepository yeuCauRutTrichRepository)
    {
        _cauHoiRepository = cauHoiRepository;
        _deThiRepository = deThiRepository;
        _yeuCauRutTrichRepository = yeuCauRutTrichRepository;
    }

    public async Task<(bool Success, string Message, Guid? MaDeThi)> RutTrichTuLuanAsync(Guid maYeuCau, string tenDeThi)
    {
        try
        {
            var yeuCau = await _yeuCauRutTrichRepository.GetByIdAsync(maYeuCau);
            if (yeuCau == null)
                return (false, "Yêu cầu rút trích không tồn tại.", null);

            if (string.IsNullOrWhiteSpace(yeuCau.MaTran))
                return (false, "Ma trận rút trích chưa được cung cấp.", null);

            MaTranTuLuan maTran;
            try
            {
                maTran = JsonConvert.DeserializeObject<MaTranTuLuan>(yeuCau.MaTran)!;
            }
            catch
            {
                return (false, "Lỗi định dạng ma trận tự luận.", null);
            }

            if (maTran.Parts == null || !maTran.Parts.Any())
                return (false, "Ma trận tự luận không hợp lệ hoặc không có phần nào.", null);

            var maDeThi = Guid.NewGuid();
            var (chiTietDeThis, errors) = await RutTrichTheoPartsAsync(maTran, maDeThi);

            // Nếu có lỗi, hiển thị thông báo chi tiết
            if (errors.Any())
            {
                var errorMessage = "Rút trích không thành công hoặc không đủ câu hỏi:\n" + 
                                   string.Join("\n", errors);
                return (false, errorMessage, null);
            }

            // Chỉ cần rút được ít nhất 1 câu TL là ok
            if (!chiTietDeThis.Any())
                return (false, "Không tìm thấy câu hỏi tự luận nào phù hợp để rút trích.", null);

            // Tính tổng số câu hỏi thực tế (bao gồm cả câu con)
            // Lấy thông tin câu hỏi để tính SoCauHoiCon
            var cauHoiIds = chiTietDeThis.Select(ct => ct.MaCauHoi).ToList();
            var cauHois = await _cauHoiRepository.FindAsync(ch => cauHoiIds.Contains(ch.MaCauHoi));
            var totalQuestions = cauHois.Sum(ch => ch.SoCauHoiCon > 1 ? ch.SoCauHoiCon : 1);

            var deThi = new DeThi
            {
                MaDeThi = maDeThi,
                MaMonHoc = yeuCau.MaMonHoc,
                TenDeThi = tenDeThi,
                DaDuyet = false,
                SoCauHoi = totalQuestions, // Tổng số câu thực tế (bao gồm câu con)
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow,
                ChiTietDeThis = chiTietDeThis
            };

            await _deThiRepository.AddAsync(deThi);

            yeuCau.DaXuLy = true;
            yeuCau.NgayXuLy = DateTime.UtcNow;
            await _yeuCauRutTrichRepository.UpdateAsync(yeuCau);

            return (true, $"Rút trích đề tự luận thành công (tổng {totalQuestions} câu, {chiTietDeThis.Count} câu hỏi cha).", maDeThi);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi hệ thống: {ex.Message}", null);
        }
    }

    private async Task<(List<ChiTietDeThi> ChiTiet, List<string> Errors)> RutTrichTheoPartsAsync(MaTranTuLuan maTran, Guid maDeThi)
    {
        var chiTiet = new List<ChiTietDeThi>();
        var usedQuestionIds = new HashSet<Guid>(); // Tránh trùng câu
        var errors = new List<string>();

        foreach (var part in maTran.Parts ?? new List<PartTuLuanDto>())
        {
            int thuTuTrongPart = 1;
            var maPhanPart = part.Part;

            foreach (var req in part.Clos ?? new List<CloDto>())
            {
                var clo = (EnumCLO)req.Clo;
                var numQuestionsNeeded = req.Num; // Số lượng câu hỏi cần rút cho CLO này
                var subQuestionCount = req.SubQuestionCount;

                // Lấy tất cả câu lớn (cha) phù hợp với CLO
                var parentQuestionsQuery = await _cauHoiRepository.FindAsync(ch =>
                    ch.MaPhan == maPhanPart &&
                    ch.MaCauHoiCha == null &&
                    !usedQuestionIds.Contains(ch.MaCauHoi) &&
                    ch.LoaiCauHoi == "TL" &&
                    ch.CLO == clo &&
                    ch.XoaTam == false);

                // Nếu có chỉ định SubQuestionCount, chỉ lấy câu có SoCauHoiCon phù hợp
                var parentQuestions = subQuestionCount.HasValue
                    ? parentQuestionsQuery.Where(ch =>
                        (ch.SoCauHoiCon <= 1 && subQuestionCount.Value == 1) ||  // Câu không có con
                        (ch.SoCauHoiCon == subQuestionCount.Value)                // Câu có đúng số con
                    ).ToList()
                    : parentQuestionsQuery.ToList();

                var availableCount = parentQuestions.Count;
                
                if (!parentQuestions.Any())
                {
                    var subQuestInfo = subQuestionCount.HasValue 
                        ? $" (yêu cầu {subQuestionCount.Value} câu con)" 
                        : "";
                    errors.Add($"Phần {maPhanPart} - CLO {req.Clo}{subQuestInfo}: Không có câu hỏi tự luận nào.");
                    continue;
                }

                if (availableCount < numQuestionsNeeded)
                {
                    var subQuestInfo = subQuestionCount.HasValue 
                        ? $" với {subQuestionCount.Value} câu con" 
                        : "";
                    errors.Add($"Phần {maPhanPart} - CLO {req.Clo}: Cần {numQuestionsNeeded} câu{subQuestInfo} nhưng chỉ có {availableCount} câu.");
                }

                // Random và lấy đúng số lượng câu hỏi theo Num (hoặc ít hơn nếu không đủ)
                var selectedParents = parentQuestions
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(numQuestionsNeeded)
                    .ToList();

                foreach (var selectedParent in selectedParents)
                {
                    usedQuestionIds.Add(selectedParent.MaCauHoi);
                    
                    // Tăng số lần dùng
                    selectedParent.SoLanDung += 1;
                    await _cauHoiRepository.UpdateAsync(selectedParent);

                    // Thêm câu lớn
                    chiTiet.Add(new ChiTietDeThi
                    {
                        MaDeThi = maDeThi,
                        MaPhan = selectedParent.MaPhan,
                        MaCauHoi = selectedParent.MaCauHoi,
                        ThuTu = thuTuTrongPart++
                    });

                    // // Lấy tất cả câu con của nó (nếu có)
                    // var childQuestions = await _cauHoiRepository.FindAsync(ch =>
                    //     ch.MaCauHoiCha == selectedParent.MaCauHoi &&
                    //     !usedQuestionIds.Contains(ch.MaCauHoi) &&
                    //     ch.XoaTam == false);
                    //
                    // foreach (var child in childQuestions.OrderBy(c => c.MaSoCauHoi))
                    // {
                    //     chiTiet.Add(new ChiTietDeThi
                    //     {
                    //         MaDeThi = maDeThi,
                    //         MaPhan = selectedParent.MaPhan,
                    //         MaCauHoi = child.MaCauHoi,
                    //         ThuTu = thuTuTrongPart++
                    //     });
                    //     usedQuestionIds.Add(child.MaCauHoi);
                    // }
                }
            }
        }

        return (chiTiet, errors);
    }
}