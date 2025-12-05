using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.Enums;
using DocumentFormat.OpenXml.Drawing.Charts;
using MathNet.Numerics.Distributions;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BEQuestionBank.Core.Services
{
    public class CauHoiService
    {
        private readonly ICauHoiRepository _cauHoiRepository;

        // Inject Repository qua Constructor
        public CauHoiService(ICauHoiRepository cauHoiRepository)
        {
            _cauHoiRepository = cauHoiRepository;
        }

        public async Task<CauHoi> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto dto, Guid userId)
        {
            // 1. Map DTO sang Entity CauHoi
            var cauHoi = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                MaSoCauHoi = dto.MaSoCauHoi, // Nếu muốn tự động tăng, cần logic riêng ở Repo
                NoiDung = dto.NoiDung,
                HoanVi = dto.HoanVi,
                CapDo = dto.CapDo,
                SoCauHoiCon = 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "Single", // Đánh dấu loại câu hỏi
                XoaTam = false
            };

            // 2. Map Câu trả lời
            if (dto.CauTraLois != null && dto.CauTraLois.Any())
            {
                foreach (var ansDto in dto.CauTraLois)
                {
                    cauHoi.CauTraLois.Add(new CauTraLoi
                    {
                        MaCauTraLoi = Guid.NewGuid(),
                        MaCauHoi = cauHoi.MaCauHoi,
                        NoiDung = ansDto.NoiDung,
                        LaDapAn = ansDto.LaDapAn,
                        HoanVi = true // Mặc định cho phép hoán vị câu trả lời
                    });
                }
            }

            // 3. Gọi Repository để lưu
            return (CauHoi)await _cauHoiRepository.AddWithAnswersAsync(cauHoi);
        }

        public async Task<CauHoi> CreateGroupQuestionAsync(CreateCauHoiNhomDto dto, Guid userId)
        {
            // 1. Tạo Câu hỏi Cha (Đoạn văn/Ngữ cảnh)
            var parentQuestion = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                NoiDung = dto.NoiDung, // Nội dung đoạn văn
                MaSoCauHoi = dto.MaSoCauHoi,
                HoanVi = false, // Câu hỏi nhóm thường không hoán vị nội dung đoạn văn
                CapDo = dto.CapDo,
                SoCauHoiCon = dto.CauHoiCons?.Count ?? 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "Group",
                XoaTam = false
            };

            // 2. Tạo các Câu hỏi Con
            if (dto.CauHoiCons != null)
            {
                foreach (var childDto in dto.CauHoiCons)
                {
                    var childQuestion = new CauHoi
                    {
                        MaCauHoi = Guid.NewGuid(),
                        MaCauHoiCha = parentQuestion.MaCauHoi, // Link tới cha
                        MaPhan = dto.MaPhan, // Cùng phân loại với cha
                        // MaSoCauHoi = ... (Có thể cần logic sinh mã tự động)
                        NoiDung = childDto.NoiDung,
                        HoanVi = childDto.HoanVi,
                        CapDo = childDto.CapDo,
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        NguoiTao = userId,
                        CLO = childDto.CLO ?? dto.CLO, // Lấy CLO của con hoặc kế thừa cha
                        LoaiCauHoi = "Child"
                    };

                    // 3. Map Câu trả lời cho Câu hỏi Con
                    if (childDto.CauTraLois != null)
                    {
                        foreach (var ansDto in childDto.CauTraLois)
                        {
                            childQuestion.CauTraLois.Add(new CauTraLoi
                            {
                                MaCauTraLoi = Guid.NewGuid(),
                                MaCauHoi = childQuestion.MaCauHoi,
                                NoiDung = ansDto.NoiDung,
                                LaDapAn = ansDto.LaDapAn,
                                HoanVi = true
                            });
                        }
                    }

                    // Thêm con vào danh sách con của cha
                    parentQuestion.CauHoiCons.Add(childQuestion);
                }
            }

            // 4. Lưu toàn bộ Graph (Cha -> Con -> Trả lời) vào DB
            // EF Core đủ thông minh để insert theo thứ tự đúng
            return (CauHoi)await _cauHoiRepository.AddWithAnswersAsync(parentQuestion);
        }

        // ... (Các hàm Create đã có ở trên)

        /// <summary>
        /// Lấy danh sách tất cả câu hỏi với phân trang và lọc nâng cao
        /// </summary>
        public async Task<object> GetAllWithFilterAsync(
            int pageIndex = 1,
            int pageSize = 20,
            string? keyword = null,
            string? loaiCauHoi = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null,
            string? sortBy = "NgayTao",
            string? sortOrder = "desc")
        {
            var entities = await _cauHoiRepository.GetAllWithAnswersAsync();

            // Lọc chỉ lấy câu hỏi gốc (không phải câu con)
            var query = entities
                .Where(e => e.MaCauHoiCha == null && !e.XoaTam.GetValueOrDefault())
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(q => 
                    (q.NoiDung != null && q.NoiDung.ToLower().Contains(keyword)) ||
                    (q.LoaiCauHoi != null && q.LoaiCauHoi.ToLower().Contains(keyword)));
            }

            // Lọc theo loại câu hỏi
            if (!string.IsNullOrWhiteSpace(loaiCauHoi))
            {
                query = query.Where(q => q.LoaiCauHoi == loaiCauHoi);
            }

            // Lọc theo Phần
            if (phanId.HasValue)
            {
                query = query.Where(q => q.MaPhan == phanId.Value);
            }

            // Lọc theo Môn học (cần có navigation property Phan)
            if (monHocId.HasValue)
            {
                query = query.Where(q => q.Phan != null && q.Phan.MaMonHoc == monHocId.Value);
            }

            // Lọc theo Khoa (cần có navigation property Phan -> MonHoc -> Khoa)
            if (khoaId.HasValue)
            {
                query = query.Where(q => 
                    q.Phan != null && 
                    q.Phan.MonHoc != null && 
                    q.Phan.MonHoc.MaKhoa == khoaId.Value);
            }

            // Sắp xếp
            query = sortBy?.ToLower() switch
            {
                "masocauhoi" => sortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(q => q.MaSoCauHoi)
                    : query.OrderByDescending(q => q.MaSoCauHoi),
                "noidung" => sortOrder?.ToLower() == "asc"
                    ? query.OrderBy(q => q.NoiDung)
                    : query.OrderByDescending(q => q.NoiDung),
                "loaicauhoi" => sortOrder?.ToLower() == "asc"
                    ? query.OrderBy(q => q.LoaiCauHoi)
                    : query.OrderByDescending(q => q.LoaiCauHoi),
                _ => sortOrder?.ToLower() == "asc"
                    ? query.OrderBy(q => q.NgayTao)
                    : query.OrderByDescending(q => q.NgayTao)
            };

            // Đếm tổng
            var totalCount = query.Count();

            // Phân trang và map sang DTO
            var pagedData = query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList() // Convert to list first to avoid expression tree issues
                .Select(e => new CauHoiDto
                {
                    MaCauHoi = e.MaCauHoi,
                    MaPhan = e.MaPhan,
                    TenPhan = e.Phan != null ? e.Phan.TenPhan : null,
                    MaSoCauHoi = e.MaSoCauHoi,
                    NoiDung = e.NoiDung,
                    HoanVi = e.HoanVi,
                    CapDo = e.CapDo,
                    SoCauHoiCon = e.LoaiCauHoi == "Group" ? e.CauHoiCons.Count : 0,
                    NgayTao = e.NgayTao,
                    XoaTam = e.XoaTam ?? false,
                    CLO = e.CLO,
                    LoaiCauHoi = e.LoaiCauHoi,
                    
                    // Chỉ include câu trả lời cho câu hỏi đơn
                    CauTraLois = e.LoaiCauHoi != "Group" 
                        ? e.CauTraLois.Select(a => new CauTraLoiDto
                        {
                            MaCauTraLoi = a.MaCauTraLoi,
                            NoiDung = a.NoiDung,
                            LaDapAn = a.LaDapAn,
                            HoanVi = a.HoanVi
                        }).ToList()
                        : new List<CauTraLoiDto>()
                })
                .ToList();

            return new
            {
                Items = pagedData,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Lấy danh sách câu hỏi đơn (Single questions only)
        /// </summary>
        public async Task<object> GetSingleQuestionsAsync(
            int pageIndex = 1,
            int pageSize = 20,
            string? keyword = null,
            Guid? phanId = null)
        {
            var entities = await _cauHoiRepository.GetAllWithAnswersAsync();

            // Lọc chỉ câu hỏi đơn (Single/Multiple Choice)
            var query = entities
                .Where(e => e.MaCauHoiCha == null && 
                           e.LoaiCauHoi == "Single" && 
                           !e.XoaTam.GetValueOrDefault())
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(q => q.NoiDung != null && q.NoiDung.ToLower().Contains(keyword));
            }

            // Lọc theo Phần
            if (phanId.HasValue)
            {
                query = query.Where(q => q.MaPhan == phanId.Value);
            }

            // Đếm tổng
            var totalCount = query.Count();

            // Phân trang
            var pagedData = query
                .OrderByDescending(q => q.NgayTao)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList() // Convert to list first to avoid expression tree issues
                .Select(e => new CauHoiDto
                {
                    MaCauHoi = e.MaCauHoi,
                    MaPhan = e.MaPhan,
                    TenPhan = e.Phan != null ? e.Phan.TenPhan : null,
                    MaSoCauHoi = e.MaSoCauHoi,
                    NoiDung = e.NoiDung,
                    HoanVi = e.HoanVi,
                    CapDo = e.CapDo,
                    NgayTao = e.NgayTao,
                    CLO = e.CLO,
                    LoaiCauHoi = e.LoaiCauHoi,
                    XoaTam = e.XoaTam ?? false,
                    CauTraLois = e.CauTraLois.Select(a => new CauTraLoiDto
                    {
                        MaCauTraLoi = a.MaCauTraLoi,
                        NoiDung = a.NoiDung,
                        LaDapAn = a.LaDapAn,
                        HoanVi = a.HoanVi
                    }).ToList()
                })
                .ToList();

            return new
            {
                Items = pagedData,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Lấy danh sách tất cả câu hỏi (Legacy - Không phân trang)
        /// </summary>
        public async Task<IEnumerable<CauHoiDto>> GetAllAsync()
        {
            var entities = await _cauHoiRepository.GetAllWithAnswersAsync();

            // Map Entity sang DTO phẳng để hiển thị trên grid
            return entities.Select(e => new CauHoiDto
            {
                MaCauHoi = e.MaCauHoi,
                MaSoCauHoi = e.MaSoCauHoi,
                NoiDung = e.NoiDung,
                LoaiCauHoi = e.LoaiCauHoi,
                NgayTao = e.NgayTao,
                XoaTam = e.XoaTam ?? false,
                CLO = e.CLO
            }).OrderByDescending(x => x.NgayTao).ToList();
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi theo ID (bao gồm cả câu trả lời)
        /// </summary>
        public async Task<CauHoiWithCauTraLoiDto?> GetByIdAsync(Guid id)
        {
            var entity = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (entity == null) return null;

            // Map Entity sang DTO chi tiết
            var dto = new CauHoiWithCauTraLoiDto
            {
                MaCauHoi = entity.MaCauHoi,
                MaPhan = entity.MaPhan,
                MaSoCauHoi = entity.MaSoCauHoi,
                NoiDung = entity.NoiDung,
                HoanVi = entity.HoanVi,
                CapDo = entity.CapDo,
                CLO = entity.CLO,
                LoaiCauHoi = entity.LoaiCauHoi,
                SoCauHoiCon = entity.SoCauHoiCon,
                CauTraLois = entity.CauTraLois.Select(ans => new BeQuestionBank.Shared.DTOs.CauTraLoi.CauTraLoiDto
                {
                    MaCauTraLoi = ans.MaCauTraLoi,
                    NoiDung = ans.NoiDung,
                    LaDapAn = ans.LaDapAn,
                    HoanVi = ans.HoanVi
                }).ToList()
            };

            // Nếu là câu hỏi nhóm, bạn có thể cần thêm logic map CauHoiCons ở đây 
            // (Tùy thuộc vào DTO của bạn có hỗ trợ danh sách con không)

            return dto;
        }

        /// <summary>
        /// Cập nhật câu hỏi và danh sách câu trả lời
        /// </summary>
        public async Task<bool> UpdateAsync(Guid id, UpdateCauHoiWithCauTraLoiDto dto, Guid userId)
        {
            // 1. Kiểm tra tồn tại
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null) return false;

            // 2. Cập nhật thông tin chính (Entity CauHoi)
            // Tạo một object CauHoi mới chứa thông tin update để truyền xuống Repo
            var updateEntity = new CauHoi
            {
                MaCauHoi = id,
                NoiDung = dto.NoiDung,
                MaPhan = dto.MaPhan,
                HoanVi = dto.HoanVi,
                CapDo = dto.CapDo,
                CLO = dto.CLO,
                NgayCapNhat = DateTime.Now,

                // Giữ nguyên các thông tin hệ thống cũ
                NguoiTao = existing.NguoiTao,
                NgayTao = existing.NgayTao,
                LoaiCauHoi = existing.LoaiCauHoi,
                TrangThai = existing.TrangThai,
                XoaTam = existing.XoaTam,
                MaSoCauHoi = existing.MaSoCauHoi
            };

            // 3. Map danh sách câu trả lời mới
            // Repository sẽ lo việc so sánh ID để biết cái nào là Thêm/Sửa/Xóa
            if (dto.CauTraLois != null)
            {
                updateEntity.CauTraLois = dto.CauTraLois.Select(a => new CauTraLoi
                {
                    MaCauHoi = id,
                    NoiDung = a.NoiDung,
                    LaDapAn = a.LaDapAn,
                    HoanVi = a.HoanVi
                }).ToList();
            }

            // 4. Gọi Repository để thực hiện Update (bao gồm cả transaction xử lý câu trả lời)
            await _cauHoiRepository.UpdateWithAnswersAsync(id, updateEntity);
            return true;
        }

        /// <summary>
        /// Xóa câu hỏi (Xóa mềm - Soft Delete)
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _cauHoiRepository.GetByIdAsync(id);
            if (entity == null) return false;

            // Đánh dấu xóa mềm
            entity.XoaTam = true;
            entity.NgayCapNhat = DateTime.Now;
            entity.TrangThai = false;

            // Sử dụng hàm Update cơ bản của GenericRepository
            await _cauHoiRepository.UpdateAsync(entity);
            return true;
        }

        /// <summary>
        /// Lấy danh sách câu hỏi nhóm với phân trang và lọc
        /// </summary>
        public async Task<object> GetCauHoiNhomAsync(
            int pageIndex = 1,
            int pageSize = 10,
            string? keyword = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null)
        {
            var groups = await _cauHoiRepository.GetAllGroupsAsync();

            // Lọc câu hỏi nhóm (LoaiCauHoi = "Group" và có câu hỏi con)
            var query = groups
                .Where(g => g.MaCauHoiCha == null
                            && g.LoaiCauHoi == "Group"
                            && g.CauHoiCons.Any()
                            && !g.XoaTam.GetValueOrDefault())
                .AsQueryable();

            // Lọc theo từ khóa (tìm trong nội dung đoạn văn)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(q => q.NoiDung != null && q.NoiDung.ToLower().Contains(keyword));
            }

            // Lọc theo Phần
            if (phanId.HasValue)
            {
                query = query.Where(q => q.MaPhan == phanId.Value);
            }
            // Nếu muốn lọc theo MonHoc hoặc Khoa, cần join với Phan -> MonHoc -> Khoa
            // (Giả định đã có navigation property hoặc sử dụng Include)

            // Đếm tổng số
            var totalCount = query.Count();

            // Phân trang
            var pagedData = query
                .OrderByDescending(x => x.NgayTao)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(parent => new CauHoiDto
                {
                    MaCauHoi = parent.MaCauHoi,
                    MaPhan = parent.MaPhan,
                    MaSoCauHoi = parent.MaSoCauHoi,
                    NoiDung = parent.NoiDung,
                    HoanVi = parent.HoanVi,
                    CapDo = parent.CapDo,
                    SoCauHoiCon = parent.CauHoiCons.Count,
                    NgayTao = parent.NgayTao,
                    CLO = parent.CLO,
                    LoaiCauHoi = parent.LoaiCauHoi,
                    XoaTam = parent.XoaTam ?? false,

                    // Không include CauHoiCons ở đây để giảm payload
                    // Client có thể gọi API detail để lấy đầy đủ
                })
                .ToList();

            return new
            {
                Items = pagedData,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi nhóm kèm tất cả câu hỏi con và câu trả lời
        /// </summary>
        public async Task<CauHoiNhomDetailDto?> GetGroupQuestionDetailAsync(Guid parentId)
        {
            var parent = await _cauHoiRepository.GetByIdWithChildrenAsync(parentId);

            if (parent == null || parent.LoaiCauHoi != "Group")
                return null;

            var dto = new CauHoiNhomDetailDto
            {
                MaCauHoi = parent.MaCauHoi,
                MaPhan = parent.MaPhan,
                MaSoCauHoi = parent.MaSoCauHoi,
                NoiDung = parent.NoiDung,
                HoanVi = parent.HoanVi,
                CapDo = parent.CapDo,
                CLO = parent.CLO,
                LoaiCauHoi = parent.LoaiCauHoi,
                SoCauHoiCon = parent.CauHoiCons.Count,
                NgayTao = parent.NgayTao,
                XoaTam = parent.XoaTam ?? false,

                // Map các câu hỏi con
                CauHoiCons = parent.CauHoiCons
                    .OrderBy(c => c.NgayTao)
                    .Select(child => new CauHoiWithCauTraLoiDto
                    {
                        MaCauHoi = child.MaCauHoi,
                        MaCauHoiCha = child.MaCauHoiCha,
                        MaPhan = child.MaPhan,
                        MaSoCauHoi = child.MaSoCauHoi,
                        NoiDung = child.NoiDung,
                        HoanVi = child.HoanVi,
                        CapDo = child.CapDo,
                        CLO = child.CLO,
                        LoaiCauHoi = child.LoaiCauHoi,

                        // Map các câu trả lời của câu hỏi con
                        CauTraLois = child.CauTraLois.Select(ans => new CauTraLoiDto
                        {
                            MaCauTraLoi = ans.MaCauTraLoi,
                            NoiDung = ans.NoiDung,
                            LaDapAn = ans.LaDapAn,
                            HoanVi = ans.HoanVi
                        }).ToList()
                    }).ToList()
            };

            return dto;
        }

        /// <summary>
        /// Lấy danh sách câu hỏi con của một câu hỏi nhóm
        /// </summary>
        public async Task<List<CauHoiWithCauTraLoiDto>> GetChildQuestionsByParentIdAsync(Guid parentId)
        {
            var parent = await _cauHoiRepository.GetByIdWithChildrenAsync(parentId);

            if (parent == null || parent.LoaiCauHoi != "Group")
                return new List<CauHoiWithCauTraLoiDto>();

            var childQuestions = parent.CauHoiCons
                .OrderBy(c => c.NgayTao)
                .Select(child => new CauHoiWithCauTraLoiDto
                {
                    MaCauHoi = child.MaCauHoi,
                    MaCauHoiCha = child.MaCauHoiCha,
                    MaPhan = child.MaPhan,
                    MaSoCauHoi = child.MaSoCauHoi,
                    NoiDung = child.NoiDung,
                    HoanVi = child.HoanVi,
                    CapDo = child.CapDo,
                    CLO = child.CLO,
                    LoaiCauHoi = child.LoaiCauHoi,
                    SoCauHoiCon = 0,

                    CauTraLois = child.CauTraLois.Select(ans => new CauTraLoiDto
                    {
                        MaCauTraLoi = ans.MaCauTraLoi,
                        NoiDung = ans.NoiDung,
                        LaDapAn = ans.LaDapAn,
                        HoanVi = ans.HoanVi
                    }).ToList()
                })
                .ToList();

            return childQuestions;
        }

        /// <summary>
        /// Lấy tất cả câu hỏi ghép nối → loại GN
        /// </summary>
        // public async Task<List<GhepNoiDto>> GetCauHoiGhepNoiAsync()
        // {
        //     var list = await _cauHoiRepository.GetAllAsync();
        //
        //     // lấy GN
        //     var gn = list.Where(x => x.LoaiCauHoi == "GN" && !x.XoaTam.GetValueOrDefault()).ToList();
        //
        //     var result = new List<GhepNoiDto>();
        //     var visited = new HashSet<Guid>();
        //
        //     foreach (var cau in gn)
        //     {
        //         if (visited.Contains(cau.MaCauHoi)) continue;
        //
        //         var doi = gn.FirstOrDefault(x => x.MaCauHoi == cau.MaCauHoiCha);
        //
        //         if (doi != null)
        //         {
        //             visited.Add(cau.MaCauHoi);
        //             visited.Add(doi.MaCauHoi);
        //
        //             result.Add(new GhepNoiDto
        //             {
        //                 Trai = new CauHoiDto
        //                 {
        //                     MaCauHoi = cau.MaCauHoi,
        //                     MaPhan = cau.MaPhan,
        //                     MaSoCauHoi = cau.MaSoCauHoi,
        //                     NoiDung = cau.NoiDung,
        //                     HoanVi = cau.HoanVi,
        //                     CapDo = cau.CapDo,
        //                     SoCauHoiCon = cau.SoCauHoiCon,
        //                     NgayTao = cau.NgayTao,
        //                     CLO = cau.CLO,
        //                     LoaiCauHoi = cau.LoaiCauHoi,
        //                 },
        //                 Phai = new CauHoiDto
        //                 {
        //                     MaCauHoi = doi.MaCauHoi,
        //                     MaPhan = doi.MaPhan,
        //                     MaSoCauHoi = doi.MaSoCauHoi,
        //                     NoiDung = doi.NoiDung,
        //                     HoanVi = doi.HoanVi,
        //                     CapDo = doi.CapDo,
        //                     SoCauHoiCon = doi.SoCauHoiCon,
        //                     NgayTao = doi.NgayTao,
        //                     CLO = doi.CLO,
        //                     LoaiCauHoi = doi.LoaiCauHoi,
        //                 },
        //             });
        //         }
        //     }
        //
        //     return result;
        // }
        public async Task<List<GhepNoiGroupDto>> GetCauHoiGhepNoiAsync()
        {
            var list = await _cauHoiRepository.GetAllAsync();

            // 1. LẤY GN CHA (chỉ câu GHÉP NỐI gốc)
            var roots = list
                .Where(x => x.LoaiCauHoi == "GN"
                            && x.MaCauHoiCha == null
                            && !x.XoaTam.GetValueOrDefault())
                .ToList();

            var result = new List<GhepNoiGroupDto>();

            foreach (var root in roots)
            {
                // 2. TÌM TẤT CẢ GN CON của root
                var children = list
                    .Where(x => x.LoaiCauHoi == "GN" 
                                && !x.XoaTam.GetValueOrDefault()
                                && IsDescendantOf(x, root.MaCauHoi, list))
                    .ToList();

                var visited = new HashSet<Guid>();
                var pairs = new List<GhepNoiDto>();

                // 3. GHÉP CẶP: child → itsChild
                foreach (var cau in children)
                {
                    if (visited.Contains(cau.MaCauHoi))
                        continue;

                    // tìm child của "cau"
                    var doi = children.FirstOrDefault(x => x.MaCauHoiCha == cau.MaCauHoi);

                    if (doi != null)
                    {
                        visited.Add(cau.MaCauHoi);
                        visited.Add(doi.MaCauHoi);

                        pairs.Add(new GhepNoiDto
                        {
                            Trai = new CauHoiDto
                            {
                                MaCauHoi = cau.MaCauHoi,
                                MaPhan = cau.MaPhan,
                                MaSoCauHoi = cau.MaSoCauHoi,
                                NoiDung = cau.NoiDung,
                                HoanVi = cau.HoanVi,
                                CapDo = cau.CapDo,
                                SoCauHoiCon = cau.SoCauHoiCon,
                                NgayTao = cau.NgayTao,
                                CLO = cau.CLO,
                                LoaiCauHoi = cau.LoaiCauHoi,
                            },
                            Phai = new CauHoiDto
                            {
                                MaCauHoi = doi.MaCauHoi,
                                MaPhan = doi.MaPhan,
                                MaSoCauHoi = doi.MaSoCauHoi,
                                NoiDung = doi.NoiDung,
                                HoanVi = doi.HoanVi,
                                CapDo = doi.CapDo,
                                SoCauHoiCon = doi.SoCauHoiCon,
                                NgayTao = doi.NgayTao,
                                CLO = doi.CLO,
                                LoaiCauHoi = doi.LoaiCauHoi,
                            }
                        });
                    }
                }

                // 4. ADD GROUP OUTPUT
                result.Add(new GhepNoiGroupDto
                {
                    NhomCha = new CauHoiDto
                    {
                        MaCauHoi = root.MaCauHoi,
                        NoiDung = root.NoiDung,
                        LoaiCauHoi = root.LoaiCauHoi
                    },
                    Pairs = pairs
                });
            }

            return result;
        }
       

        /// <summary>
        /// Lấy tất cả câu hỏi ĐIỀN TỪ (DT)
        /// → Không có câu con
        /// → Nhiều đáp án, tất cả đều đúng (LaDapAn = true)
        /// → Thứ tự đáp án cực kỳ quan trọng
        /// </summary>
        public async Task<List<CauHoiDto>> GetCauHoiDienTuAsync()
        {
            var allQuestions = await _cauHoiRepository.GetAllWithAnswersAsync();

            var dienTuQuestions = allQuestions
                .Where(q => 
                        q.LoaiCauHoi == "DT" 
                        && q.MaCauHoiCha == null 
                        && !q.XoaTam.GetValueOrDefault()
                        && (!q.CauHoiCons.Any()) 
                        && q.CauTraLois.Any()    
                )
                .Select(q => new CauHoiDto
                {
                    MaCauHoi = q.MaCauHoi,
                    MaPhan = q.MaPhan ,
                    MaSoCauHoi = q.MaSoCauHoi,
                    NoiDung = q.NoiDung ?? "", 
                    HoanVi = false, 
                    CapDo = q.CapDo,
                    SoCauHoiCon = 0,
                    NgayTao = q.NgayTao,
                    CLO = q.CLO,
                    LoaiCauHoi = "DT",

                    // Quan trọng: giữ nguyên thứ tự đáp án (không shuffle)
                    CauTraLois = q.CauTraLois
                        .OrderBy(a => a.ThuTu) 
                        .Select(a => new CauTraLoiDto
                        {
                            MaCauTraLoi = a.MaCauTraLoi,
                            ThuTu =a.ThuTu, 
                            NoiDung = a.NoiDung,     
                            LaDapAn = true,          
                            HoanVi = false
                        })
                        .ToList()
                })
                .OrderByDescending(x => x.MaSoCauHoi)
                .ToList();

            return dienTuQuestions;
        }
        /// <summary>
        /// Thống kê số lượng câu hỏi theo loại
        /// </summary>
        public async Task<object> GetStatisticsAsync(Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
        {
            var entities = await _cauHoiRepository.GetAllWithAnswersAsync();

            // Lọc câu hỏi gốc (không phải câu con)
            var query = entities.Where(e => e.MaCauHoiCha == null && !e.XoaTam.GetValueOrDefault());

            // Áp dụng filter
            if (phanId.HasValue)
                query = query.Where(q => q.MaPhan == phanId.Value);
            else if (monHocId.HasValue)
                query = query.Where(q => q.Phan != null && q.Phan.MaMonHoc == monHocId.Value);
            else if (khoaId.HasValue)
                query = query.Where(q => q.Phan != null && q.Phan.MonHoc != null && q.Phan.MonHoc.MaKhoa == khoaId.Value);

            var questionsList = query.ToList();

            // Thống kê theo loại
            var byType = questionsList
                .GroupBy(q => q.LoaiCauHoi ?? "Unknown")
                .Select(g => new
                {
                    LoaiCauHoi = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            // Thống kê theo CLO
            var byCLO = questionsList
                .Where(q => q.CLO.HasValue)
                .GroupBy(q => q.CLO!.Value)
                .Select(g => new
                {
                    CLO = g.Key.ToString(),
                    SoLuong = g.Count()
                })
                .ToList();

            // Thống kê theo cấp độ
            var byLevel = questionsList
                .GroupBy(q => q.CapDo)
                .Select(g => new
                {
                    CapDo = g.Key,
                    SoLuong = g.Count()
                })
                .OrderBy(x => x.CapDo)
                .ToList();

            return new
            {
                TongSoCauHoi = questionsList.Count,
                SoCauHoiDon = questionsList.Count(q => q.LoaiCauHoi == "Single"),
                SoCauHoiNhom = questionsList.Count(q => q.LoaiCauHoi == "Group"),
                SoCauHoiGhepNoi = questionsList.Count(q => q.LoaiCauHoi == "GN"),
                SoCauHoiDienTu = questionsList.Count(q => q.LoaiCauHoi == "DT"),
                ThongKeTheoLoai = byType,
                ThongKeTheoCLO = byCLO,
                ThongKeTheoCapDo = byLevel
            };
        }

        /// <summary>
        /// Lấy danh sách các loại câu hỏi có trong hệ thống
        /// </summary>
        public async Task<object> GetQuestionTypesAsync()
        {
            var entities = await _cauHoiRepository.GetAllAsync();

            var types = entities
                .Where(e => e.MaCauHoiCha == null && !e.XoaTam.GetValueOrDefault())
                .Select(e => e.LoaiCauHoi)
                .Distinct()
                .Where(type => !string.IsNullOrEmpty(type))
                .Select(type => new
                {
                    Value = type,
                    Label = type switch
                    {
                        "Single" => "Câu hỏi đơn",
                        "Group" => "Câu hỏi nhóm",
                        "GN" => "Câu hỏi ghép nối",
                        "DT" => "Câu hỏi điền từ",
                        _ => type
                    }
                })
                .ToList();

            return types;
        }

        private bool IsDescendantOf(CauHoi entity, Guid rootId, IEnumerable<CauHoi> all)
        {
            while (entity.MaCauHoiCha != null)
            {
                if (entity.MaCauHoiCha == rootId)
                    return true;

                entity = all.FirstOrDefault(x => x.MaCauHoi == entity.MaCauHoiCha);
                if (entity == null)
                    return false;
            }
            return false;
        }
    }
}