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
            var chiTietDeThis = await RutTrichTheoPartsAsync(maTran, maDeThi);

            // Chỉ cần rút được ít nhất 1 câu TL là ok
            if (!chiTietDeThis.Any())
                return (false, "Không tìm thấy câu hỏi tự luận nào phù hợp để rút trích.", null);

            var deThi = new DeThi
            {
                MaDeThi = maDeThi,
                MaMonHoc = yeuCau.MaMonHoc,
                TenDeThi = tenDeThi,
                DaDuyet = false,
                SoCauHoi = chiTietDeThis.Count,
                NgayTao = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow,
                ChiTietDeThis = chiTietDeThis
            };

            await _deThiRepository.AddAsync(deThi);

            yeuCau.DaXuLy = true;
            yeuCau.NgayXuLy = DateTime.UtcNow;
            await _yeuCauRutTrichRepository.UpdateAsync(yeuCau);

            return (true, $"Rút trích đề tự luận thành công (tổng {chiTietDeThis.Count} câu).", maDeThi);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi hệ thống: {ex.Message}", null);
        }
    }

    private async Task<List<ChiTietDeThi>> RutTrichTheoPartsAsync(MaTranTuLuan maTran, Guid maDeThi)
    {
        var chiTiet = new List<ChiTietDeThi>();
        var usedQuestionIds = new HashSet<Guid>(); // Tránh trùng câu

        foreach (var part in maTran.Parts ?? new List<PartTuLuanDto>())
        {
            int thuTuTrongPart = 1;
            var maPhanPart = part.Part;

            foreach (var req in part.Clos ?? new List<CloDto>())
            {
                var clo = (EnumCLO)req.Clo;

                // Chỉ lấy câu lớn (cha) trước để làm đầu phần
                var parentQuestions = await _cauHoiRepository.FindAsync(ch =>
                    ch.MaPhan == maPhanPart &&
                    ch.MaCauHoiCha == null &&
                    !usedQuestionIds.Contains(ch.MaCauHoi) &&
                    ch.LoaiCauHoi == "TL" &&
                    ch.CLO == clo &&
                    ch.XoaTam == false);

                if (!parentQuestions.Any())
                    continue;

                // Random chọn 1 câu lớn
                var selectedParent = parentQuestions.OrderBy(_ => Guid.NewGuid()).First();
                usedQuestionIds.Add(selectedParent.MaCauHoi);
//tăng số làn dùng
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

        return chiTiet;
    }
}