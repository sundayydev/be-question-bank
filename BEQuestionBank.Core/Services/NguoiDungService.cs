
using BEQuestionBank.Shared.DTOs.user;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Crypto.Generators;

namespace BEQuestionBank.Core.Services
{
    public class NguoiDungService
    {
        private readonly INguoiDungRepository _userRepository;
        private readonly IKhoaRepository _khoaRepository;

        public NguoiDungService(INguoiDungRepository userRepository, IKhoaRepository khoaRepository)
        {
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _userRepository = userRepository;
            _khoaRepository = khoaRepository;
        }

        public async Task<NguoiDung> GetByIdAsync(Guid maNguoiDung)
        {
            return await _userRepository.GetByIdAsync(maNguoiDung);
        }

        public async Task<NguoiDung> CreateAsync(NguoiDung user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "Dữ liệu người dùng không được để trống");
            }

            user.MaNguoiDung = Guid.NewGuid();
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            await _userRepository.AddAsync(user);
            return user;
        }

        public async Task<NguoiDung> UpdateAsync(Guid maNguoiDung, NguoiDung user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "Dữ liệu người dùng không được để trống");
            }
            if (user.MaKhoa.HasValue)
            {
                var khoa = await _khoaRepository.GetByIdAsync(user.MaKhoa.Value);
                if (khoa == null)
                {
                    throw new ArgumentException($"Mã khoa {user.MaKhoa} không tồn tại.");
                }
            }
            var existingUser = await _userRepository.GetByIdAsync(maNguoiDung);
            existingUser.TenDangNhap = user.TenDangNhap;
            if (!string.IsNullOrEmpty(user.MatKhau))
            {
                existingUser.MatKhau = BCrypt.Net.BCrypt.HashPassword(user.MatKhau);
            }
            existingUser.HoTen = user.HoTen;
            existingUser.Email = user.Email;
            existingUser.VaiTro = user.VaiTro;
            existingUser.BiKhoa = user.BiKhoa;
            existingUser.MaKhoa = user.MaKhoa;

            await _userRepository.UpdateAsync(existingUser);
            return existingUser;
        }

        public async Task<bool> DeleteAsync(Guid maNguoiDung)
        {
            var user = await _userRepository.GetByIdAsync(maNguoiDung);
            if (user == null)
            {
                return false;
            }

            await _userRepository.DeleteAsync(user);
            return true;
        }

        public async Task<NguoiDung> GetByUsernameAsync(string tenDangNhap)
        {
            return await _userRepository.GetByUsernameAsync(tenDangNhap);
        }

        public async Task<IEnumerable<NguoiDung>> GetAllAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<IEnumerable<NguoiDung>> FindAsync(Expression<Func<NguoiDung, bool>> predicate)
        {
            return await _userRepository.FindAsync(predicate);
        }

        public async Task<NguoiDung> FirstOrDefaultAsync(Expression<Func<NguoiDung, bool>> predicate)
        {
            return await _userRepository.FirstOrDefaultAsync(predicate);
        }

        public async Task AddAsync(NguoiDung entity)
        {
            await _userRepository.AddAsync(entity);
        }

        public async Task<NguoiDung> UpdateAsync(NguoiDung entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (entity.MaKhoa.HasValue)
            {
                var khoa = await _khoaRepository.GetByIdAsync(entity.MaKhoa.Value);
                if (khoa == null)
                    throw new ArgumentException($"Mã khoa {entity.MaKhoa} không tồn tại.");
            }

            entity.NgayCapNhat = DateTime.UtcNow;
            await _userRepository.UpdateAsync(entity);
            return entity;
        }

        public async Task DeleteAsync(NguoiDung entity)
        {
            await _userRepository.DeleteAsync(entity);
        }

        public async Task<IEnumerable<NguoiDung>> GetUsersActiveAsync()
        {
            return await _userRepository.FindAsync(u => u.BiKhoa == false);
        }

        public async Task<IEnumerable<NguoiDung>> GetUsersLockedAsync()
        {
            return await _userRepository.FindAsync(u => u.BiKhoa == true);
        }

        
        public async Task<bool> SetUserLockStateAsync(Guid maNguoiDung, bool isLocked)
        {
            var user = await _userRepository.GetByIdAsync(maNguoiDung);
            if (user == null)
            {
                return false; 
            }
            
            if (user.BiKhoa == isLocked)
            {
                return true;
            }
            
            user.BiKhoa = isLocked;
            await _userRepository.UpdateAsync(user);

            return true;
        }

       public async Task<(int SuccessCount, List<string> Errors)> ImportUsersFromExcelAsync(IFormFile file)
        {
            var errors = new List<string>();
            int successCount = 0;

            if (file == null || file.Length == 0)
            {
                errors.Add("File không được trống hoặc không hợp lệ.");
                return (successCount, errors);
            }
            
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    IWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sheet = workbook.GetSheetAt(0);

                    if (sheet == null)
                    {
                        errors.Add("File Excel không chứa dữ liệu hợp lệ.");
                        return (successCount, errors);
                    }

                    int rowCount = sheet.LastRowNum; // dòng cuối cùng có dữ liệu (index bắt đầu từ 0)
                    if (rowCount < 1)
                    {
                        errors.Add("File Excel không chứa dữ liệu người dùng.");
                        return (successCount, errors);
                    }

                    for (int row = 1; row <= rowCount; row++) 
                    {
                        try
                        {
                            IRow currentRow = sheet.GetRow(row);
                            if (currentRow == null) continue;

                            var dto = new ImportUserDto
                            {
                                TenDangNhap = currentRow.GetCell(0)?.ToString()?.Trim(),
                                MatKhau = currentRow.GetCell(1)?.ToString()?.Trim(),
                                HoTen = currentRow.GetCell(2)?.ToString()?.Trim(),
                                Email = currentRow.GetCell(3)?.ToString()?.Trim(),
                                VaiTro = int.TryParse(currentRow.GetCell(4)?.ToString(), out var vt) ? vt : 0,
                                BiKhoa = bool.TryParse(currentRow.GetCell(5)?.ToString(), out var bk) && bk,
                                TenKhoa = currentRow.GetCell(6)?.ToString()?.Trim()
                            };

                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(dto.TenDangNhap))
                            {
                                errors.Add($"Dòng {row + 1}: Tên đăng nhập không được để trống.");
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(dto.MatKhau))
                            {
                                errors.Add($"Dòng {row + 1}: Mật khẩu không được để trống.");
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(dto.Email))
                            {
                                errors.Add($"Dòng {row + 1}: Email không được để trống.");
                                continue;
                            }

                            if (!IsValidEmail(dto.Email))
                            {
                                errors.Add($"Dòng {row + 1}: Email không đúng định dạng.");
                                continue;
                            }

                            // Check for duplicates
                            var isDuplicate = await _userRepository.ExistsAsync(u =>
                                u.TenDangNhap == dto.TenDangNhap || u.Email == dto.Email);

                            if (isDuplicate)
                            {
                                errors.Add($"Dòng {row + 1}: Tên đăng nhập hoặc Email đã tồn tại.");
                                continue;
                            }

                            // Validate VaiTro
                            if (!Enum.IsDefined(typeof(EnumRole), dto.VaiTro))
                            {
                                errors.Add($"Dòng {row + 1}: Vai trò không hợp lệ (giá trị: {dto.VaiTro}).");
                                continue;
                            }

                            // Find MaKhoa
                            Guid? maKhoa = null;
                            if (!string.IsNullOrWhiteSpace(dto.TenKhoa))
                            {
                                var khoa = await _khoaRepository.GetByTenKhoaAsync(dto.TenKhoa);
                                if (khoa == null)
                                {
                                    errors.Add($"Dòng {row + 1}: Tên khoa '{dto.TenKhoa}' không tồn tại.");
                                    continue;
                                }
                                maKhoa = khoa.MaKhoa;
                            }

                            // Create entity
                            var entity = new NguoiDung
                            {
                                MaNguoiDung = Guid.NewGuid(),
                                TenDangNhap = dto.TenDangNhap,
                                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau) 
                                          ?? BCrypt.Net.BCrypt.HashPassword("123456"),
                                HoTen = dto.HoTen,
                                Email = dto.Email,
                                VaiTro = (EnumRole)dto.VaiTro,
                                BiKhoa = dto.BiKhoa,
                                MaKhoa = maKhoa,
                                NgayTao = DateTime.UtcNow,
                                NgayCapNhat = DateTime.UtcNow,
                                NgayDangNhapCuoi = null
                            };

                            await _userRepository.AddAsync(entity);
                            successCount++;
                        }
                        catch (Exception exRow)
                        {
                            errors.Add($"Dòng {row + 1}: Lỗi xử lý dữ liệu - {exRow.Message}");
                            Serilog.Log.Error(exRow, "Lỗi xử lý dòng {Row} khi nhập người dùng từ Excel.", row + 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Lỗi khi nhập người dùng từ Excel");
                errors.Add($"Lỗi hệ thống: {ex.Message}");
            }

            return (successCount, errors);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
    }
}