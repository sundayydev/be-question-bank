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

    public DeThiService(
        IDeThiRepository deThiRepository,
        ICauHoiRepository cauHoiRepository,
        IYeuCauRutTrichRepository yeuCauRutTrichRepository)
    {
        _deThiRepository = deThiRepository;
        _cauHoiRepository = cauHoiRepository;
        _yeuCauRutTrichRepository = yeuCauRutTrichRepository;
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
    /// <summary>
    /// Kiểm tra xem có đủ câu hỏi để rút trích theo ma trận không.
    /// </summary>
    private async Task<(bool Success, string Message, int AvailableQuestions)>
        CheckAvailableQuestionsPerPartAsync(MaTranDto maTran, Guid maMonHoc)
    {
        int totalAvailable = 0;

        foreach (var part in maTran.Parts)
        {
            int partRequired = part.NumQuestions;
            int partAvailable = 0;

            foreach (var clo in part.Clos)
            {
                foreach (var questionType in part.QuestionTypes)
                {
                    int required = Math.Min(clo.Num, questionType.Num);

                    var available = await _cauHoiRepository.CountAsync(ch =>
                        ch.MaPhan == part.MaPhan &&
                        ch.CLO == (EnumCLO?)clo.Clo &&
                        ch.LoaiCauHoi == questionType.Loai &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false);

                    if (available < required)
                    {
                        return (false,
                            $"Không đủ câu hỏi cho phần {part.MaPhan}, CLO {clo.Clo}, loại {questionType.Loai}. " +
                            $"Yêu cầu: {required}, có: {available}.",
                            totalAvailable);
                    }

                    partAvailable += available;
                }
            }

            if (partAvailable < partRequired)
            {
                return (false,
                    $"Không đủ câu hỏi cho phần {part.MaPhan}. Yêu cầu: {partRequired}, có: {partAvailable}.",
                    totalAvailable);
            }

            totalAvailable += partAvailable;
        }

        return (true, "Đủ câu hỏi theo từng phần.", totalAvailable);
    }
    
    private async Task<(bool Success, string Message, int AvailableQuestions)>
        CheckAvailableQuestionsNoPartAsync(MaTranDto maTran, Guid maMonHoc)
    {
        int totalAvailable = 0;

        foreach (var clo in maTran.Clos)
        {
            foreach (var questionType in maTran.QuestionTypes)
            {
                int required = Math.Min(clo.Num, questionType.Num);

                var available = await _cauHoiRepository.CountAsync(ch =>
                    ch.CLO == (EnumCLO?)clo.Clo &&
                    ch.LoaiCauHoi == questionType.Loai &&
                    ch.Phan.MaMonHoc == maMonHoc &&
                    ch.XoaTam == false);

                if (available < required)
                {
                    return (false,
                        $"Không đủ câu hỏi cho CLO {clo.Clo}, loại {questionType.Loai}. " +
                        $"Yêu cầu: {required}, có: {available}.",
                        totalAvailable);
                }

                totalAvailable += available;
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


    private async Task<List<ChiTietDeThi>> SelectQuestionsPerPartAsync(MaTranDto maTran, Guid maMonHoc, Guid maDeThi)
    {
        var chiTietDeThis = new List<ChiTietDeThi>();
        int thuTu = 1;

        foreach (var part in maTran.Parts)
        {
            int remainingQuestions = part.NumQuestions;
            foreach (var clo in part.Clos)
            {
                foreach (var questionType in part.QuestionTypes)
                {
                    int numQuestions = Math.Min(clo.Num, Math.Min(questionType.Num, remainingQuestions));
                    if (numQuestions <= 0) continue;

                    var questions = await _cauHoiRepository.FindAsync(ch =>
                        ch.MaPhan == part.MaPhan &&
                        ch.CLO == (EnumCLO?)clo.Clo &&
                        ch.LoaiCauHoi == questionType.Loai &&
                        ch.Phan.MaMonHoc == maMonHoc &&
                        ch.XoaTam == false);

                    var selected = questions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();

                    chiTietDeThis.AddRange(selected.Select(q => new ChiTietDeThi
                    {
                        MaDeThi = maDeThi,
                        MaPhan = part.MaPhan,
                        MaCauHoi = q.MaCauHoi,
                        ThuTu = thuTu++
                    }));

                    remainingQuestions -= selected.Count;
                    if (remainingQuestions <= 0) break;
                }
                if (remainingQuestions <= 0) break;
            }
        }

        return chiTietDeThis;
    }
    private async Task<List<ChiTietDeThi>> SelectQuestionsNoPartAsync(MaTranDto maTran, Guid maMonHoc, Guid maDeThi)
    {
        var chiTietDeThis = new List<ChiTietDeThi>();
        int thuTu = 1;

        foreach (var clo in maTran.Clos)
        {
            foreach (var questionType in maTran.QuestionTypes)
            {
                int numQuestions = Math.Min(clo.Num, questionType.Num);
                if (numQuestions <= 0) continue;

                var questions = await _cauHoiRepository.FindAsync(ch =>
                    ch.CLO == (EnumCLO?)clo.Clo &&
                    ch.LoaiCauHoi == questionType.Loai &&
                    ch.Phan.MaMonHoc == maMonHoc &&
                    ch.XoaTam == false);

                var selected = questions.OrderBy(x => Guid.NewGuid()).Take(numQuestions).ToList();

                chiTietDeThis.AddRange(selected.Select(q => new ChiTietDeThi
                {
                    MaDeThi = maDeThi,
                    MaPhan = q.MaPhan,
                    MaCauHoi = q.MaCauHoi,
                    ThuTu = thuTu++
                }));
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
            if (chiTietDeThis.Count != maTran.TotalQuestions)
            {
                return (false, $"Số câu hỏi rút trích ({chiTietDeThis.Count}) không khớp với yêu cầu ({maTran.TotalQuestions}).", null);
            }

            // Tạo đề thi
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