using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace BEQuestionBank.Core.Services
{
    /// <summary>
    /// Service chuyên dụng để export đề thi ra file .ezp (JSON format)
    /// </summary>
    public class DeThiExportService
    {
        private readonly IDeThiRepository _deThiRepository;
        private readonly EzpEncryptionService _encryptionService;
        private readonly EzpSettings _ezpSettings;

        public DeThiExportService(
            IDeThiRepository deThiRepository, 
            EzpEncryptionService encryptionService,
            IOptions<EzpSettings> ezpSettings)
        {
            _deThiRepository = deThiRepository;
            _encryptionService = encryptionService;
            _ezpSettings = ezpSettings.Value;
        }

        /// <summary>
        /// Export đề thi với đầy đủ thông tin câu hỏi và đáp án thành DTO để chuyển thành JSON
        /// </summary>
        public async Task<(bool Success, string Message, DeThiExportDto? Data)> ExportDeThiToJsonAsync(Guid maDeThi)
        {
            try
            {
                // Lấy đề thi với đầy đủ thông tin
                var deThiObj = await _deThiRepository.GetDeThiWithChiTietAndCauTraLoiAsync(maDeThi);
                if (deThiObj == null)
                {
                    return (false, "Không tìm thấy đề thi.", null);
                }

                // Cast sang DeThi
                var deThi = (DeThi)deThiObj;

                // Tạo DTO export
                var exportDto = new DeThiExportDto
                {
                    ExportVersion = "1.0",
                    ExportDate = DateTime.UtcNow,
                    DeThiInfo = new DeThiInfoDto
                    {
                        MaDeThi = deThi.MaDeThi,
                        TenDeThi = deThi.TenDeThi,
                        TenMonHoc = deThi.MonHoc?.TenMonHoc,
                        TenKhoa = deThi.MonHoc?.Khoa?.TenKhoa,
                        SoCauHoi = deThi.SoCauHoi,
                        NgayTao = deThi.NgayTao,
                        DaDuyet = deThi.DaDuyet
                    },
                    CauHois = new List<CauHoiExportDto>()
                };

                // Map câu hỏi từ ChiTietDeThi
                if (deThi.ChiTietDeThis != null && deThi.ChiTietDeThis.Any())
                {
                    foreach (var chiTiet in deThi.ChiTietDeThis.OrderBy(ct => ct.ThuTu))
                    {
                        if (chiTiet.CauHoi != null)
                        {
                            var cauHoiExport = MapToExportDto(chiTiet.CauHoi, chiTiet.ThuTu);
                            exportDto.CauHois.Add(cauHoiExport);
                        }
                    }
                }

                return (true, "Export đề thi thành công.", exportDto);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi export đề thi: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Export đề thi thành chuỗi JSON
        /// </summary>
        public async Task<(bool Success, string Message, string? JsonContent)> ExportDeThiToJsonStringAsync(Guid maDeThi, bool indented = true)
        {
            try
            {
                var (success, message, data) = await ExportDeThiToJsonAsync(maDeThi);
                
                if (!success || data == null)
                {
                    return (false, message, null);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = null // Giữ nguyên tên property
                };

                string jsonContent = JsonSerializer.Serialize(data, options);
                return (true, "Export JSON thành công.", jsonContent);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi export JSON: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Export đề thi thành file .ezp (thực chất là file JSON)
        /// </summary>
        public async Task<(bool Success, string Message, byte[]? FileContent, string FileName)> ExportDeThiToEzpFileAsync(Guid maDeThi)
        {
            try
            {
                var (success, message, jsonContent) = await ExportDeThiToJsonStringAsync(maDeThi, indented: true);
                
                if (!success || string.IsNullOrEmpty(jsonContent))
                {
                    return (false, message, null, string.Empty);
                }

                // Chuyển JSON thành byte array với UTF-8 encoding
                byte[] fileContent = Encoding.UTF8.GetBytes(jsonContent);

                // Tạo tên file
                var deThi = await _deThiRepository.GetByIdAsync(maDeThi);
                string fileName = $"{SanitizeFileName(deThi?.TenDeThi ?? "DeThi")}_{maDeThi.ToString().Substring(0, 8)}.ezp";

                return (true, "Export file .ezp thành công.", fileContent, fileName);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi export file .ezp: {ex.Message}", null, string.Empty);
            }
        }

        /// <summary>
        /// Export đề thi thành file .ezp (tự động mã hóa nếu EnableEncryption = true trong config)
        /// </summary>
        public async Task<(bool Success, string Message, byte[]? FileContent, string FileName)> ExportDeThiToEzpFileWithPasswordAsync(Guid maDeThi)
        {
            try
            {
                var (success, message, jsonContent) = await ExportDeThiToJsonStringAsync(maDeThi, indented: true);
                
                if (!success || string.IsNullOrEmpty(jsonContent))
                {
                    return (false, message, null, string.Empty);
                }

                // Tự động mã hóa nếu được bật trong config
                string contentToSave = jsonContent;
                if (_ezpSettings.EnableEncryption && !string.IsNullOrEmpty(_ezpSettings.EncryptionPassword))
                {
                    try
                    {
                        contentToSave = _encryptionService.Encrypt(jsonContent, _ezpSettings.EncryptionPassword);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Lỗi khi mã hóa file: {ex.Message}", null, string.Empty);
                    }
                }

                // Chuyển nội dung thành byte array với UTF-8 encoding
                byte[] fileContent = Encoding.UTF8.GetBytes(contentToSave);

                // Tạo tên file
                var deThi = await _deThiRepository.GetByIdAsync(maDeThi);
                string fileName = $"{SanitizeFileName(deThi?.TenDeThi ?? "DeThi")}_{maDeThi.ToString().Substring(0, 8)}.ezp";

                return (true, "Export file .ezp thành công.", fileContent, fileName);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi export file .ezp: {ex.Message}", null, string.Empty);
            }
        }

        /// <summary>
        /// Map CauHoi entity sang CauHoiExportDto
        /// </summary>
        private CauHoiExportDto MapToExportDto(CauHoi cauHoi, int? thuTu = null)
        {
            var dto = new CauHoiExportDto
            {
                MaCauHoi = cauHoi.MaCauHoi,
                MaPhan = cauHoi.MaPhan,
                TenPhan = cauHoi.Phan?.TenPhan,
                MaSoCauHoi = cauHoi.MaSoCauHoi,
                NoiDung = cauHoi.NoiDung,
                ThuTu = thuTu,
                HoanVi = cauHoi.HoanVi,
                CapDo = cauHoi.CapDo,
                CLO = cauHoi.CLO?.ToString(),
                LoaiCauHoi = cauHoi.LoaiCauHoi,
                SoCauHoiCon = cauHoi.SoCauHoiCon,
                MaCauHoiCha = cauHoi.MaCauHoiCha,
                CauTraLois = new List<CauTraLoiExportDto>(),
                CauHoiCons = new List<CauHoiExportDto>()
            };

            // Map câu trả lời
            if (cauHoi.CauTraLois != null && cauHoi.CauTraLois.Any())
            {
                dto.CauTraLois = cauHoi.CauTraLois
                    .OrderBy(tl => tl.ThuTu)
                    .Select(tl => new CauTraLoiExportDto
                    {
                        MaCauTraLoi = tl.MaCauTraLoi,
                        MaCauHoi = tl.MaCauHoi,
                        NoiDung = tl.NoiDung,
                        ThuTu = tl.ThuTu,
                        LaDapAn = tl.LaDapAn,
                        HoanVi = tl.HoanVi
                    })
                    .ToList();
            }

            // Map câu hỏi con (recursive)
            if (cauHoi.CauHoiCons != null && cauHoi.CauHoiCons.Any())
            {
                dto.CauHoiCons = cauHoi.CauHoiCons
                    .OrderBy(ch => ch.MaSoCauHoi)
                    .Select(ch => MapToExportDto(ch, null))
                    .ToList();
            }

            return dto;
        }

        /// <summary>
        /// Làm sạch tên file, loại bỏ các ký tự không hợp lệ
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());
            
            // Giới hạn độ dài tên file
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50);
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "DeThi" : sanitized;
        }

        /// <summary>
        /// Giải mã file EZP và trả về nội dung JSON (dùng để test/debug)
        /// </summary>
        /// <param name="encryptedContent">Nội dung file EZP đã mã hóa</param>
        /// <returns>Tuple: (Success, Message, DecryptedJson)</returns>
        public (bool Success, string Message, string? DecryptedJson) DecryptEzpFile(string encryptedContent)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedContent))
                {
                    return (false, "Nội dung file rỗng.", null);
                }

                // Kiểm tra file có được mã hóa không
                if (!_encryptionService.IsEncrypted(encryptedContent))
                {
                    // File không mã hóa, trả về luôn
                    return (true, "File không được mã hóa (plain JSON).", encryptedContent);
                }

                // Kiểm tra password có trong config không
                if (string.IsNullOrEmpty(_ezpSettings.EncryptionPassword))
                {
                    return (false, "Không tìm thấy password trong cấu hình để giải mã.", null);
                }

                // Giải mã
                try
                {
                    string decryptedJson = _encryptionService.Decrypt(encryptedContent, _ezpSettings.EncryptionPassword);
                    return (true, "Giải mã thành công!", decryptedJson);
                }
                catch (UnauthorizedAccessException)
                {
                    return (false, "Mật khẩu không đúng! Kiểm tra lại password trong appsettings.json", null);
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi khi giải mã: {ex.Message}", null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi không mong đợi: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Giải mã file EZP từ byte array (upload từ form)
        /// </summary>
        /// <param name="fileBytes">Byte array của file EZP</param>
        /// <returns>Tuple: (Success, Message, DecryptedJson)</returns>
        public (bool Success, string Message, string? DecryptedJson) DecryptEzpFileFromBytes(byte[] fileBytes)
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    return (false, "File rỗng.", null);
                }

                // Convert byte array sang string
                string encryptedContent = Encoding.UTF8.GetString(fileBytes);
                
                // Gọi method decrypt
                return DecryptEzpFile(encryptedContent);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi đọc file: {ex.Message}", null);
            }
        }
    }
}
