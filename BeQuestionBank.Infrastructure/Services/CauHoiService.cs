using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.CauHoi.TuLuan;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.Extensions.Caching.Memory;

namespace BEQuestionBank.Core.Services
{
    public class CauHoiService
    {
        private readonly ICauHoiRepository _cauHoiRepository;
        private readonly IMemoryCache _cache;

        public CauHoiService(ICauHoiRepository cauHoiRepository, IMemoryCache cache)
        {
            _cauHoiRepository = cauHoiRepository;
            _cache = cache;
        }

        public async Task<CauHoi> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto dto, Guid userId)
        {
            var nextMaSo = dto.MaSoCauHoi != 0
                ? dto.MaSoCauHoi
                : await _cauHoiRepository.GetNextMaSoCauHoiAsync(dto.MaPhan);

            var cauHoi = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                MaSoCauHoi = nextMaSo,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                HoanVi = dto.HoanVi,
                CapDo = dto.CapDo,
                SoCauHoiCon = 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "TN",
                XoaTam = false
            };

            if (dto.CauTraLois != null && dto.CauTraLois.Any())
            {
                int order = 1;
                foreach (var ansDto in dto.CauTraLois)
                {
                    cauHoi.CauTraLois.Add(new CauTraLoi
                    {
                        MaCauTraLoi = Guid.NewGuid(),
                        MaCauHoi = cauHoi.MaCauHoi,
                        NoiDung = $"<span>{ansDto.NoiDung}</span>",
                        LaDapAn = ansDto.LaDapAn,
                        HoanVi = ansDto.HoanVi,
                        ThuTu = order++
                    });
                }
            }

            return (CauHoi)await _cauHoiRepository.AddWithAnswersAsync(cauHoi);
        }

        public async Task<CauHoi> CreateGroupQuestionAsync(CreateCauHoiNhomDto dto, Guid userId)
        {
            var nextMaSo = dto.MaSoCauHoi != 0
                ? dto.MaSoCauHoi
                : await _cauHoiRepository.GetNextMaSoCauHoiAsync(dto.MaPhan);
            // 1. Tạo Câu hỏi Cha (Đoạn văn/Ngữ cảnh)
            var parentQuestion = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                MaSoCauHoi = nextMaSo,
                HoanVi = false,
                CapDo = dto.CapDo,
                SoCauHoiCon = dto.CauHoiCons?.Count ?? 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "NH",
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
                        MaCauHoiCha = parentQuestion.MaCauHoi,
                        MaPhan = dto.MaPhan,
                        MaSoCauHoi = nextMaSo,
                        NoiDung = $"<span>{childDto.NoiDung}</span>",
                        HoanVi = childDto.HoanVi,
                        CapDo = childDto.CapDo,
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        NguoiTao = userId,
                        CLO = childDto.CLO ?? dto.CLO,
                        LoaiCauHoi = null
                    };

                    // 3. Map Câu trả lời cho Câu hỏi Con
                    if (childDto.CauTraLois != null)
                    {
                        int order = 1;
                        foreach (var ansDto in childDto.CauTraLois)
                        {
                            childQuestion.CauTraLois.Add(new CauTraLoi
                            {
                                MaCauTraLoi = Guid.NewGuid(),
                                MaCauHoi = childQuestion.MaCauHoi,
                                NoiDung = $"<span>{ansDto.NoiDung}</span>",
                                LaDapAn = ansDto.LaDapAn,
                                HoanVi = ansDto.HoanVi,
                                ThuTu = order++,
                            });
                        }
                    }

                    parentQuestion.CauHoiCons.Add(childQuestion);
                }
            }

            return (CauHoi)await _cauHoiRepository.AddWithAnswersAsync(parentQuestion);
        }

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
        /// Lấy danh sách tất cả câu hỏi (Dùng cho bảng dữ liệu)
        /// </summary>
        public async Task<IEnumerable<CauHoiDto>> GetAllAsync()
        {
            var entities = await _cauHoiRepository.GetAllWithAnswersAsync();

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
        /// Lấy chi tiết câu hỏi theo ID (bao gồm cả câu trả lời va cau hoi con)
        /// </summary>
        public async Task<CauHoiDto?> GetByIdAsync(Guid id)
        {
            var entity = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (entity == null) return null;

            var dto = new CauHoiDto
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

                CauTraLois = entity.CauTraLois.Select(ans => new CauTraLoiDto
                {
                    MaCauTraLoi = ans.MaCauTraLoi,
                    NoiDung = ans.NoiDung,
                    LaDapAn = ans.LaDapAn,
                    HoanVi = ans.HoanVi
                }).ToList(),

                CauHoiCons = entity.CauHoiCons?.Select(ch => new CauHoiDto
                {
                    MaCauHoi = ch.MaCauHoi,
                    NoiDung = ch.NoiDung ?? "",
                    HoanVi = ch.HoanVi,
                    CapDo = ch.CapDo,
                    CLO = ch.CLO,
                    CauTraLois = ch.CauTraLois?.Select(a => new CauTraLoiDto
                    {
                        MaCauTraLoi = a.MaCauTraLoi,
                        NoiDung = a.NoiDung,
                        LaDapAn = a.LaDapAn,
                        HoanVi = a.HoanVi
                    }).ToList() ?? new List<CauTraLoiDto>()
                }).ToList()
            };

            return dto;
        }


        /// <summary>
        /// Cập nhật câu hỏi và danh sách câu trả lời
        /// </summary>
        public async Task<CauHoiDto> UpdateAsync(Guid id, UpdateCauHoiWithCauTraLoiDto dto, Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null) return null;

            // Update parent
            existing.NoiDung = $"<span>{dto.NoiDung}</span>";
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            if (dto.CauTraLois != null && dto.CauTraLois.Any())
            {
                foreach (var ansDto in dto.CauTraLois)
                {
                    // Update existing answer
                    if (ansDto.MaCauTraLoi != null && ansDto.MaCauTraLoi != Guid.Empty)
                    {
                        var ex = existing.CauTraLois.FirstOrDefault(a => a.MaCauTraLoi == ansDto.MaCauTraLoi);
                        if (ex != null)
                        {
                            ex.NoiDung = ansDto.NoiDung;
                            ex.HoanVi = ansDto.HoanVi;
                            ex.LaDapAn = ansDto.LaDapAn;
                            ex.ThuTu = ansDto.ThuTu;
                        }
                    }
                    // Add new answer
                    else
                    {
                        var newThuTu = existing.CauTraLois.Any()
                            ? existing.CauTraLois.Max(x => x.ThuTu) + 1
                            : 1;

                        existing.CauTraLois.Add(new CauTraLoi
                        {
                            MaCauTraLoi = Guid.NewGuid(),
                            NoiDung = ansDto.NoiDung,
                            LaDapAn = ansDto.LaDapAn,
                            HoanVi = ansDto.HoanVi,
                            ThuTu = newThuTu,
                        });
                    }
                }
            }

            // Không xóa bất kỳ câu trả lời nào nếu FE không gửi
            await _cauHoiRepository.SaveChangesAsync();

            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                NoiDung = existing.NoiDung,
                MaPhan = existing.MaPhan,
                CapDo = existing.CapDo,
                HoanVi = existing.HoanVi,
                CauTraLois = existing.CauTraLois.Select(ct => new CauTraLoiDto
                {
                    MaCauTraLoi = ct.MaCauTraLoi,
                    NoiDung = ct.NoiDung,
                    ThuTu = ct.ThuTu,
                    LaDapAn = ct.LaDapAn,
                    HoanVi = ct.HoanVi
                }).ToList()
            };
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
        /// Lấy chi tiết câu hỏi nhóm kèm tất cả câu hỏi con và câu trả lời
        /// </summary>
        public async Task<CauHoiNhomDetailDto?> GetGroupQuestionDetailAsync(Guid parentId)
        {
            var parent = await _cauHoiRepository.GetByIdWithChildrenAsync(parentId);

            if (parent == null || parent.LoaiCauHoi != "NH")
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

            if (parent == null || parent.LoaiCauHoi != "NH")
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
        /// Lấy tất cả câu hỏi GHÉP NỐI (GN)
        /// </summary>
        public async Task<List<CauHoiDto>> GetCauHoiGhepNoiAsync()
        {
            var cacheKey = "PairingQuestions";

            // Kiểm tra cache
            if (_cache.TryGetValue(cacheKey, out List<CauHoiDto> cachedResult))
            {
                return cachedResult;
            }

            var groups = await _cauHoiRepository.GetAllGhepNoiAsync();

            groups = groups.Where(x => x.LoaiCauHoi == "GN").ToList();

            var result = groups
                .Where(g => g.MaCauHoiCha == null && g.CauHoiCons.Any() && !string.IsNullOrEmpty(g.LoaiCauHoi))
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

                    CauHoiCons = parent.CauHoiCons.Select(child => new CauHoiDto
                    {
                        MaCauHoi = child.MaCauHoi,
                        MaSoCauHoi = child.MaSoCauHoi,
                        MaPhan = child.MaPhan,
                        NoiDung = child.NoiDung,
                        HoanVi = child.HoanVi,
                        CapDo = child.CapDo,
                        CLO = child.CLO,
                        LoaiCauHoi = child.LoaiCauHoi,
                        CauTraLois = child.CauTraLois
                            .Where(a => a.LaDapAn)
                            .Take(1)
                            .Select(a => new CauTraLoiDto
                            {
                                MaCauTraLoi = a.MaCauTraLoi,
                                MaCauHoi = a.MaCauHoi,
                                NoiDung = a.NoiDung,
                                LaDapAn = a.LaDapAn,
                                HoanVi = a.HoanVi
                            })
                            .ToList()
                    }).ToList()
                })
                .OrderByDescending(x => x.MaSoCauHoi)
                .ToList();

            // Set cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        /// <summary>
        /// Lấy tất cả câu hỏi MuitiChoi(MN)
        /// </summary>
        public async Task<List<CauHoiDto>> GetMultipleChoiceQuestionsAsync()
        {
            var questions = await _cauHoiRepository.GetAllMultipleChoiceAsync();
            var result = questions.Select(q => new CauHoiDto
                {
                    MaCauHoi = q.MaCauHoi,
                    MaPhan = q.MaPhan,
                    MaSoCauHoi = q.MaSoCauHoi,
                    NoiDung = q.NoiDung,
                    HoanVi = q.HoanVi,
                    CapDo = q.CapDo,
                    CLO = q.CLO,
                    LoaiCauHoi = q.LoaiCauHoi,
                    CauTraLois = q.CauTraLois.Select(a => new CauTraLoiDto
                    {
                        MaCauTraLoi = a.MaCauTraLoi,
                        MaCauHoi = a.MaCauHoi,
                        NoiDung = a.NoiDung,
                        LaDapAn = a.LaDapAn,
                        HoanVi = a.HoanVi
                    }).ToList()
                }).OrderByDescending(q => q.MaSoCauHoi)
                .ToList();

            return result;
        }

        /// <summary>
        /// Lấy tất cả câu hỏi ĐIỀN TỪ (DT)
        /// </summary>
        public async Task<List<CauHoiDto>> GetCauHoiDienTuAsync()
        {
            var groups = await _cauHoiRepository.GetAllDienTuAsync();

            groups = groups.Where(x => x.LoaiCauHoi == "DT").ToList();

            var result = groups
                .Where(g => g.MaCauHoiCha == null && g.CauHoiCons.Any() && !string.IsNullOrEmpty(g.LoaiCauHoi))
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

                    CauHoiCons = parent.CauHoiCons.Select(child => new CauHoiDto
                    {
                        MaCauHoi = child.MaCauHoi,
                        MaSoCauHoi = child.MaSoCauHoi,
                        MaPhan = child.MaPhan,
                        NoiDung = child.NoiDung,
                        HoanVi = child.HoanVi,
                        CapDo = child.CapDo,
                        CLO = child.CLO,
                        LoaiCauHoi = child.LoaiCauHoi,
                        CauTraLois = child.CauTraLois
                            .Where(a => a.LaDapAn)
                            .Select(a => new CauTraLoiDto
                            {
                                MaCauTraLoi = a.MaCauTraLoi,
                                MaCauHoi = a.MaCauHoi,
                                NoiDung = a.NoiDung,
                                LaDapAn = a.LaDapAn,
                                HoanVi = a.HoanVi
                            })
                            .ToList()
                    }).ToList()
                })
                .OrderByDescending(x => x.MaSoCauHoi)
                .ToList();


            return result;
        }

        /// <summary>
        /// Lấy toàn bộ câu hỏi tự luận (không phân trang)
        /// </summary>
        public async Task<List<CauHoiDto>> GetAllEssayQuestionsAsync()
        {
            var groups = await _cauHoiRepository.GetAllEssayAsync();

            groups = groups.Where(x => x.LoaiCauHoi == "TL").ToList();

            var result = groups
                .Where(g => g.MaCauHoiCha == null && g.CauHoiCons.Any() && !string.IsNullOrEmpty(g.LoaiCauHoi))
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

                    CauHoiCons = parent.CauHoiCons.Select(child => new CauHoiDto
                    {
                        MaCauHoi = child.MaCauHoi,
                        MaSoCauHoi = child.MaSoCauHoi,
                        MaPhan = child.MaPhan,
                        NoiDung = child.NoiDung,
                        HoanVi = child.HoanVi,
                        CapDo = child.CapDo,
                        CLO = child.CLO,
                        LoaiCauHoi = child.LoaiCauHoi,
                    }).ToList()
                })
                .ToList();
            return result;
        }


        /// <summary>
        /// Tạo câu hỏi điền từ (Fill in the blank - DT)
        /// - Tất cả đáp án đều đúng (LaDapAn = true)
        /// - Thứ tự đáp án quan trọng (sử dụng ThuTu để sắp xếp)
        /// - Không có câu hỏi con
        /// </summary>
        public async Task<CauHoi> CreateCauHoiDienTuAsync(CreateCauHoiDienTuDto dto, Guid userId)
        {
            // 1. Tạo Câu hỏi Cha
            var parentQuestion = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                MaSoCauHoi = dto.MaSoCauHoi,
                HoanVi = false,
                CapDo = dto.CapDo,
                SoCauHoiCon = dto.CauHoiCons?.Count ?? 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "DT",
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
                        MaCauHoiCha = parentQuestion.MaCauHoi,
                        MaPhan = parentQuestion.MaPhan,
                        NoiDung = $"<span>{childDto.NoiDung}</span>",
                        HoanVi = false,
                        CapDo = childDto.CapDo,
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        NguoiTao = userId,
                        CLO = null,
                        LoaiCauHoi = null,
                        XoaTam = false
                    };

                    if (childDto.CauTraLois != null)
                    {
                        int order = 1;
                        foreach (var ansDto in childDto.CauTraLois)
                        {
                            childQuestion.CauTraLois.Add(new CauTraLoi
                            {
                                MaCauTraLoi = Guid.NewGuid(),
                                MaCauHoi = childQuestion.MaCauHoi,
                                NoiDung = $"<p>{ansDto.NoiDung}</p>",
                                LaDapAn = true,
                                HoanVi = childDto.HoanVi,
                                ThuTu = order++
                            });
                        }
                    }

                    parentQuestion.CauHoiCons.Add(childQuestion);
                }
            }
            else
            {
                throw new ArgumentException("Câu hỏi điền từ phải có ít nhất một đáp án.");
            }

            await _cauHoiRepository.AddWithAnswersAsync(parentQuestion);
            return parentQuestion;
        }

        /// <summary>
        /// Tạo câu hỏi Ghép nối (Matching) - giống nhóm nhưng mỗi cauhoi con chỉ có 1 đáp án đúng
        /// </summary>
        public async Task<CauHoi> CreateGhepNoiQuestionAsync(CreateCauHoiGhepNoiDto dto, Guid userId)
        {
            if (dto.CauHoiCons == null || !dto.CauHoiCons.Any())
                throw new InvalidOperationException("Câu hỏi GN phải có ít nhất 1 câu hỏi con.");

            var parentQuestion = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                MaSoCauHoi = dto.MaSoCauHoi,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                HoanVi = false,
                CapDo = dto.CapDo,
                SoCauHoiCon = dto.CauHoiCons.Count,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "GN",
                XoaTam = false,
                CauHoiCons = new List<CauHoi>()
            };

            foreach (var childDto in dto.CauHoiCons)
            {
                var childQuestion = new CauHoi
                {
                    MaCauHoi = Guid.NewGuid(),
                    MaCauHoiCha = parentQuestion.MaCauHoi,
                    MaPhan = parentQuestion.MaPhan,
                    NoiDung = $"<span>{childDto.NoiDung}</span>",
                    HoanVi = childDto.HoanVi,
                    CapDo = childDto.CapDo,
                    TrangThai = true,
                    NgayTao = DateTime.Now,
                    NguoiTao = userId,
                    CLO = childDto.CLO ?? dto.CLO,
                    LoaiCauHoi = null,
                    XoaTam = false,
                };

                // 2. Thêm đáp án (chỉ đáp án hoán vị)
                if (childDto.CauTraLois != null)
                {
                    int order = 1;
                    foreach (var a in childDto.CauTraLois)
                    {
                        childQuestion.CauTraLois.Add(new CauTraLoi
                        {
                            MaCauTraLoi = Guid.NewGuid(),
                            MaCauHoi = childQuestion.MaCauHoi,
                            NoiDung = $"<p>{a.NoiDung}</p>",
                            LaDapAn = true,
                            HoanVi = childDto.HoanVi,
                            ThuTu = order++
                        });
                    }
                }

                parentQuestion.CauHoiCons.Add(childQuestion);
            }

            //  Lưu vào DB
            await _cauHoiRepository.AddWithAnswersAsync(parentQuestion);
            _cache.Remove("PairingQuestions");

            return parentQuestion;
        }


        /// <summary>
        /// Tạo câu hỏi Muiti  (MN) _ có ít nhát 3 đáp án , và có 2 đpas án đúng
        /// </summary>
        public async Task<CauHoi> CreateMultipleChoiceQuestionAsync(CreateCauHoiMultipleChoiceDto dto, Guid userId)
        {
            if (dto.CauTraLois == null || dto.CauTraLois.Count < 3)
                throw new InvalidOperationException("Câu hỏi Multiple Choice phải có ít nhất 3 câu trả lời.");

            var dapAnDung = dto.CauTraLois.Count(a => a.LaDapAn);
            if (dapAnDung < 2)
                throw new InvalidOperationException("Câu hỏi Multiple Choice phải có ít nhất 2 đáp án đúng.");

            var cauHoi = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                MaSoCauHoi = dto.MaSoCauHoi,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                HoanVi = false,
                CapDo = dto.CapDo,
                SoCauHoiCon = 0,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "MN",
                XoaTam = false
            };
            int order = 1;
            foreach (var ansDto in dto.CauTraLois)
            {
                cauHoi.CauTraLois.Add(new CauTraLoi
                {
                    MaCauTraLoi = Guid.NewGuid(),
                    MaCauHoi = cauHoi.MaCauHoi,
                    NoiDung = $"<span>{ansDto.NoiDung}</span>",
                    LaDapAn = ansDto.LaDapAn,
                    HoanVi = ansDto.HoanVi,
                    ThuTu = order++
                });
            }

            //  Lưu vào DB qua repository
            var createdQuestion = await _cauHoiRepository.AddWithAnswersAsync(cauHoi);

            return createdQuestion;
        }

        /// <summary>
        /// Tạo câu hỏi tự luận (Essay / Open-ended)
        /// </summary>
        public async Task<CauHoi> CreateEssayQuestionAsync(CreateCauHoiTuLuanDto dto, Guid userId)
        {
            // Validate: Nội dung câu hỏi không được để trống
            if (string.IsNullOrWhiteSpace(dto.NoiDung))
                throw new ArgumentException("Nội dung câu hỏi tự luận không được để trống.");

            //  Tạo Parent
            var parentQuestion = new CauHoi
            {
                MaCauHoi = Guid.NewGuid(),
                MaPhan = dto.MaPhan,
                MaSoCauHoi = dto.MaSoCauHoi,
                NoiDung = $"<span>{dto.NoiDung}</span>",
                HoanVi = false,
                CapDo = dto.CapDo,
                SoCauHoiCon = dto.CauHoiCons.Count,
                MaCauHoiCha = null,
                TrangThai = true,
                NgayTao = DateTime.Now,
                NguoiTao = userId,
                CLO = dto.CLO,
                LoaiCauHoi = "TL",
                XoaTam = false,
                CauHoiCons = new List<CauHoi>()
            };

            //  Tạo Child
            foreach (var childDto in dto.CauHoiCons)
            {
                var childQuestion = new CauHoi
                {
                    MaCauHoi = Guid.NewGuid(),
                    MaCauHoiCha = parentQuestion.MaCauHoi,
                    MaPhan = parentQuestion.MaPhan,
                    NoiDung = $"<span>{childDto.NoiDung}</span>",
                    HoanVi = childDto.HoanVi,
                    CapDo = childDto.CapDo,
                    TrangThai = true,
                    NgayTao = DateTime.Now,
                    NguoiTao = userId,
                    CLO = childDto.CLO ?? dto.CLO,
                    LoaiCauHoi = null,
                    XoaTam = false,
                    CauTraLois = new List<CauTraLoi>()
                };

                parentQuestion.CauHoiCons.Add(childQuestion);
            }


            await _cauHoiRepository.AddWithAnswersAsync(parentQuestion);
            _cache.Remove("EssayQuestions");

            return parentQuestion;
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
                query =
                    query.Where(q => q.Phan != null && q.Phan.MonHoc != null && q.Phan.MonHoc.MaKhoa == khoaId.Value);

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
                    Label = GetLabelFromType(type)
                })
                .ToList();

            return types;
        }

        private string GetLabelFromType(string type)
        {
            if (type == null) return string.Empty;

            // Trắc nghiệm (TN, TN2, TN3,...)
            if (type.StartsWith("TN"))
            {
                if (type == "TN")
                    return "Trắc nghiệm 1 đáp án";
            }

            // Nhóm (NH1, NH2,...)
            if (type.StartsWith("NH"))
            {
                var count = type.Substring(2);
                return $"Câu hỏi nhóm ({count} câu hỏi con)";
            }

            return type switch
            {
                "DT" => "Câu hỏi điền từ",
                "TL" => "Câu hỏi tự luận",
                "GN" => "Câu hỏi ghép nối",
                "MN" => "Câu hỏi trắc nghiệm (nhiều đáp án đúng)",
                _ => type
            };
        }

        public async Task<PagedResult<CauHoiDto>> GetEssayPagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetEssayPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);

        public async Task<PagedResult<CauHoiDto>> GetGroupPagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetGroupPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);

        public async Task<PagedResult<CauHoiDto>> GetSinglePagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetSinglePagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);

        public async Task<PagedResult<CauHoiDto>> GetFillBlankPagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetFillBlankPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);

        public async Task<PagedResult<CauHoiDto>> GetPairingPagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetPairingPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);

        public async Task<PagedResult<CauHoiDto>> GetMultipleChoicePagedAsync(int page, int pageSize, string? sort,
            string? search,
            Guid? khoaId, Guid? monHocId, Guid? phanId)
            => await _cauHoiRepository.GetMultipleChoicePagedAsync(page, pageSize, sort, search, khoaId, monHocId,
                phanId);

        /// <summary>
        /// Cập nhật câu hỏi nhóm (NH) - kèm câu hỏi con và đáp án
        /// </summary>
        public async Task<CauHoiDto?> UpdateGroupQuestionAsync(Guid id, UpdateCauHoiNhomDto dto, Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null || existing.LoaiCauHoi != "NH")
                return null;

            // Update parent
            existing.NoiDung = dto.NoiDung;
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            // Duyệt child DTO gửi từ FE
            foreach (var childDto in dto.CauHoiCons)
            {
                // Bỏ qua nếu không có ID -> không update, không thêm
                if (childDto.MaCauHoi == null || childDto.MaCauHoi == Guid.Empty)
                    continue;

                var childEntity = existing.CauHoiCons
                    .FirstOrDefault(ch => ch.MaCauHoi == childDto.MaCauHoi);

                if (childEntity == null)
                    continue; // không có trong DB thì bỏ qua luôn

                // Update child
                childEntity.NoiDung = childDto.NoiDung;
                childEntity.HoanVi = childDto.HoanVi;
                childEntity.CapDo = childDto.CapDo;
                childEntity.CLO = childDto.CLO ?? existing.CLO;
                childEntity.NgayCapNhat = DateTime.Now;

                // Update answers
                foreach (var ansDto in childDto.CauTraLois)
                {
                    // Chỉ update answer có ID
                    if (ansDto.MaCauTraLoi == null || ansDto.MaCauTraLoi == Guid.Empty)
                        continue;

                    var answerEntity = childEntity.CauTraLois
                        .FirstOrDefault(a => a.MaCauTraLoi == ansDto.MaCauTraLoi);

                    if (answerEntity == null)
                        continue;

                    answerEntity.NoiDung = ansDto.NoiDung;
                    answerEntity.LaDapAn = ansDto.LaDapAn;
                    answerEntity.HoanVi = ansDto.HoanVi;

                    answerEntity.ThuTu = ansDto.ThuTu;
                }
            }

            await _cauHoiRepository.SaveChangesAsync();

            // Return DTO tối giản
            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                MaPhan = existing.MaPhan,
                MaSoCauHoi = existing.MaSoCauHoi,
                NoiDung = existing.NoiDung,
                HoanVi = existing.HoanVi,
                CapDo = existing.CapDo,
                CLO = existing.CLO,
                LoaiCauHoi = "NH",
                SoCauHoiCon = existing.CauHoiCons.Count,

                CauHoiCons = existing.CauHoiCons.Select(ch => new CauHoiDto
                {
                    MaCauHoi = ch.MaCauHoi,
                    MaPhan = ch.MaPhan,
                    NoiDung = ch.NoiDung,
                    HoanVi = ch.HoanVi,
                    CapDo = ch.CapDo,
                    CLO = ch.CLO,
                    LoaiCauHoi = ch.LoaiCauHoi,

                    CauTraLois = ch.CauTraLois.Select(ct => new CauTraLoiDto
                    {
                        MaCauTraLoi = ct.MaCauTraLoi,
                        NoiDung = ct.NoiDung,
                        ThuTu = ct.ThuTu,
                        LaDapAn = ct.LaDapAn,
                        HoanVi = ct.HoanVi
                    }).ToList()
                }).ToList(),

                // Group question không có đáp án
                CauTraLois = new List<CauTraLoiDto>()
            };

        }

        /// <summary>
        /// Cập nhật câu hỏi Multiple Choice (MN) 
        /// </summary>

        public async Task<CauHoiDto?> UpdateMultipleChoiceQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto dto,
            Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null) return null;

            // Update parent
            existing.NoiDung = $"<span>{dto.NoiDung}</span>";
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            if (dto.CauTraLois != null && dto.CauTraLois.Any())
            {
                foreach (var ansDto in dto.CauTraLois)
                {
                    // Update existing answer
                    if (ansDto.MaCauTraLoi != null && ansDto.MaCauTraLoi != Guid.Empty)
                    {
                        var ex = existing.CauTraLois.FirstOrDefault(a => a.MaCauTraLoi == ansDto.MaCauTraLoi);
                        if (ex != null)
                        {
                            ex.NoiDung = ansDto.NoiDung;
                            ex.HoanVi = ansDto.HoanVi;
                            ex.LaDapAn = ansDto.LaDapAn;
                            ex.ThuTu = ansDto.ThuTu;
                        }
                    }
                    // Add new answer
                    else
                    {
                        var newThuTu = existing.CauTraLois.Any()
                            ? existing.CauTraLois.Max(x => x.ThuTu) + 1
                            : 1;

                        existing.CauTraLois.Add(new CauTraLoi
                        {
                            MaCauTraLoi = Guid.NewGuid(),
                            NoiDung = ansDto.NoiDung,
                            LaDapAn = ansDto.LaDapAn,
                            HoanVi = ansDto.HoanVi,
                            ThuTu = newThuTu,
                        });
                    }
                }
            }

            // Không xóa bất kỳ câu trả lời nào nếu FE không gửi
            await _cauHoiRepository.SaveChangesAsync();

            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                NoiDung = existing.NoiDung,
                MaPhan = existing.MaPhan,
                CapDo = existing.CapDo,
                HoanVi = existing.HoanVi,
                CauTraLois = existing.CauTraLois.Select(ct => new CauTraLoiDto
                {
                    MaCauTraLoi = ct.MaCauTraLoi,
                    NoiDung = ct.NoiDung,
                    ThuTu = ct.ThuTu,
                    LaDapAn = ct.LaDapAn,
                    HoanVi = ct.HoanVi
                }).ToList()
            };
        }

        /// <summary>
        /// Cập nhật câu hỏi nhóm (TL) - kèm câu hỏi con 
        /// </summary>
        public async Task<CauHoiDto?> UpdateEssayQuestionAsync(Guid id, UpdateCauHoiTuLuanDto dto, Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null || existing.LoaiCauHoi != "TL")
                return null;

            // Update parent
            existing.NoiDung = dto.NoiDung;
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            // Duyệt child DTO gửi từ FE
            foreach (var childDto in dto.CauHoiCons)
            {
                // Bỏ qua nếu không có ID -> không update, không thêm
                if (childDto.MaCauHoi == null || childDto.MaCauHoi == Guid.Empty)
                    continue;

                var childEntity = existing.CauHoiCons
                    .FirstOrDefault(ch => ch.MaCauHoi == childDto.MaCauHoi);

                if (childEntity == null)
                    continue; // không có trong DB thì bỏ qua luôn

                // Update child
                childEntity.NoiDung = childDto.NoiDung;
                childEntity.HoanVi = childDto.HoanVi;
                childEntity.CapDo = childDto.CapDo;
                childEntity.CLO = childDto.CLO ?? existing.CLO;
                childEntity.NgayCapNhat = DateTime.Now;
            }

            await _cauHoiRepository.SaveChangesAsync();

            // Return DTO tối giản
            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                MaPhan = existing.MaPhan,
                MaSoCauHoi = existing.MaSoCauHoi,
                NoiDung = existing.NoiDung,
                HoanVi = existing.HoanVi,
                CapDo = existing.CapDo,
                CLO = existing.CLO,
                LoaiCauHoi = "TL",
                SoCauHoiCon = existing.CauHoiCons.Count,

                CauHoiCons = existing.CauHoiCons.Select(ch => new CauHoiDto
                {
                    MaCauHoi = ch.MaCauHoi,
                    MaPhan = ch.MaPhan,
                    NoiDung = ch.NoiDung,
                    HoanVi = ch.HoanVi,
                    CapDo = ch.CapDo,
                    CLO = ch.CLO,
                    LoaiCauHoi = ch.LoaiCauHoi,
                }).ToList(),


                CauTraLois = new List<CauTraLoiDto>()
            };
        }

        /// <summary>
        /// Cập nhật câu hỏi nhóm DT - DEIENF TỪ 
        /// </summary>
        public async Task<CauHoiDto?> UpdateDienTuQuestionAsync(Guid id, UpdateDienTuQuestionDto dto, Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null || existing.LoaiCauHoi != "DT")
                return null;

            // Update parent
            existing.NoiDung = dto.NoiDung;
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            // Duyệt child DTO gửi từ FE
            foreach (var childDto in dto.CauHoiCons)
            {
                // Bỏ qua nếu không có ID -> không update, không thêm
                if (childDto.MaCauHoi == null || childDto.MaCauHoi == Guid.Empty)
                    continue;

                var childEntity = existing.CauHoiCons
                    .FirstOrDefault(ch => ch.MaCauHoi == childDto.MaCauHoi);

                if (childEntity == null)
                    continue; // không có trong DB thì bỏ qua luôn

                // Update child
                childEntity.NoiDung = childDto.NoiDung;
                childEntity.HoanVi = false;
                childEntity.CapDo = childDto.CapDo;
                childEntity.CLO = childDto.CLO ?? existing.CLO;
                childEntity.NgayCapNhat = DateTime.Now;

                // Update answers
                foreach (var ansDto in childDto.CauTraLois)
                {
                    // Chỉ update answer có ID
                    if (ansDto.MaCauTraLoi == null || ansDto.MaCauTraLoi == Guid.Empty)
                        continue;

                    var answerEntity = childEntity.CauTraLois
                        .FirstOrDefault(a => a.MaCauTraLoi == ansDto.MaCauTraLoi);

                    if (answerEntity == null)
                        continue;

                    answerEntity.NoiDung = ansDto.NoiDung;
                    answerEntity.LaDapAn = ansDto.LaDapAn;
                    answerEntity.HoanVi = ansDto.HoanVi;
                    // Có thể update thứ tự nếu FE gửi
                    answerEntity.ThuTu = ansDto.ThuTu;
                }
            }

            await _cauHoiRepository.SaveChangesAsync();

            // Return DTO tối giản
            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                MaPhan = existing.MaPhan,
                MaSoCauHoi = existing.MaSoCauHoi,
                NoiDung = existing.NoiDung,
                HoanVi = existing.HoanVi,
                CapDo = existing.CapDo,
                CLO = existing.CLO,
                LoaiCauHoi = "DT",
                SoCauHoiCon = existing.CauHoiCons.Count,

                CauHoiCons = existing.CauHoiCons.Select(ch => new CauHoiDto
                {
                    MaCauHoi = ch.MaCauHoi,
                    MaPhan = ch.MaPhan,
                    NoiDung = ch.NoiDung,
                    HoanVi = ch.HoanVi,
                    CapDo = ch.CapDo,
                    CLO = ch.CLO,
                    LoaiCauHoi = ch.LoaiCauHoi,

                    CauTraLois = ch.CauTraLois.Select(ct => new CauTraLoiDto
                    {
                        MaCauTraLoi = ct.MaCauTraLoi,
                        NoiDung = ct.NoiDung,
                        ThuTu = ct.ThuTu,
                        LaDapAn = ct.LaDapAn,
                        HoanVi = ct.HoanVi
                    }).ToList()
                }).ToList(),

                CauTraLois = new List<CauTraLoiDto>()
            };
        }

        public async Task<CauHoiDto?> UpdateGhepNoiQuestionAsync(Guid id, UpdateCauHoiNhomDto dto, Guid userId)
        {
            var existing = await _cauHoiRepository.GetByIdWithAnswersAsync(id);
            if (existing == null || existing.LoaiCauHoi != "GN")
                return null;

            // Tính tổng số cặp hợp lệ từ FE
            var totalPairs = dto.CauHoiCons
                .Where(c => c.MaCauHoi != null && c.MaCauHoi != Guid.Empty)
                .Sum(c => c.CauTraLois?.Count(a => a.MaCauTraLoi != null && a.MaCauTraLoi != Guid.Empty) ?? 0);

            if (totalPairs < 2)
                return null; // hoặc ném exception nếu muốn thông báo lỗi

            // Update parent
            existing.NoiDung = dto.NoiDung;
            existing.MaPhan = dto.MaPhan;
            existing.HoanVi = dto.HoanVi;
            existing.CapDo = dto.CapDo;
            existing.CLO = dto.CLO;
            existing.NgayCapNhat = DateTime.Now;

            // Duyệt child DTO gửi từ FE
            foreach (var childDto in dto.CauHoiCons)
            {
                if (childDto.MaCauHoi == null || childDto.MaCauHoi == Guid.Empty)
                    continue;

                var childEntity = existing.CauHoiCons.FirstOrDefault(ch => ch.MaCauHoi == childDto.MaCauHoi);
                if (childEntity == null)
                    continue;

                childEntity.NoiDung = childDto.NoiDung;
                childEntity.HoanVi = childDto.HoanVi;
                childEntity.CapDo = childDto.CapDo;
                childEntity.CLO = childDto.CLO ?? existing.CLO;
                childEntity.NgayCapNhat = DateTime.Now;

                foreach (var ansDto in childDto.CauTraLois)
                {
                    if (ansDto.MaCauTraLoi == null || ansDto.MaCauTraLoi == Guid.Empty)
                        continue;

                    var answerEntity = childEntity.CauTraLois.FirstOrDefault(a => a.MaCauTraLoi == ansDto.MaCauTraLoi);
                    if (answerEntity == null)
                        continue;

                    answerEntity.NoiDung = ansDto.NoiDung;
                    answerEntity.LaDapAn = ansDto.LaDapAn;
                    answerEntity.HoanVi = ansDto.HoanVi;
                    answerEntity.ThuTu = ansDto.ThuTu;
                }
            }

            await _cauHoiRepository.SaveChangesAsync();

            // Return DTO tối giản (giữ nguyên code cũ)
            return new CauHoiDto
            {
                MaCauHoi = existing.MaCauHoi,
                MaPhan = existing.MaPhan,
                MaSoCauHoi = existing.MaSoCauHoi,
                NoiDung = existing.NoiDung,
                HoanVi = existing.HoanVi,
                CapDo = existing.CapDo,
                CLO = existing.CLO,
                LoaiCauHoi = "GN",
                SoCauHoiCon = existing.CauHoiCons.Count,
                CauHoiCons = existing.CauHoiCons.Select(ch => new CauHoiDto
                {
                    MaCauHoi = ch.MaCauHoi,
                    MaPhan = ch.MaPhan,
                    NoiDung = ch.NoiDung,
                    HoanVi = ch.HoanVi,
                    CapDo = ch.CapDo,
                    CLO = ch.CLO,
                    LoaiCauHoi = ch.LoaiCauHoi,
                    CauTraLois = ch.CauTraLois.Select(ct => new CauTraLoiDto
                    {
                        MaCauTraLoi = ct.MaCauTraLoi,
                        NoiDung = ct.NoiDung,
                        ThuTu = ct.ThuTu,
                        LaDapAn = ct.LaDapAn,
                        HoanVi = ct.HoanVi
                    }).ToList()
                }).ToList(),
                CauTraLois = new List<CauTraLoiDto>()
            };
        }


    }
}