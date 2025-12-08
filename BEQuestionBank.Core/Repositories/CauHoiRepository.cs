using System.Linq.Expressions;
using BEQuestionBank.Core.Common;
using BeQuestionBank.Core.Configurations;
using BEQuestionBank.Core.Helpers;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories;

public class CauHoiRepository : GenericRepository<CauHoi>, ICauHoiRepository
{
    private readonly AppDbContext _context;

    public CauHoiRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CauHoi>> GetAllWithAnswersAsync()
    {
        return await _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Where(c => c.XoaTam != true)
            .OrderByDescending(c => c.NgayTao) // Sửa CreatedDate -> NgayTao
            .ToListAsync();
    }

    public async Task<CauHoi?> GetByIdWithAnswersAsync(Guid maCauHoi)
    {
        return await _context.CauHois
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Include(c => c.CauHoiCons).ThenInclude(chc => chc.CauTraLois)
            .FirstOrDefaultAsync(c => c.MaCauHoi == maCauHoi && c.XoaTam != true);
    }

    public async Task<CauHoi?> GetByIdWithChildrenAsync(Guid maCauHoi)
    {
        return await _context.CauHois
            .AsNoTracking()
            .Include(c => c.Phan)
            .ThenInclude(p => p.MonHoc)
            .ThenInclude(m => m.Khoa)
            .Include(c => c.CauHoiCons.Where(child => child.XoaTam != true))
            .ThenInclude(child => child.CauTraLois)
            .FirstOrDefaultAsync(c => c.MaCauHoi == maCauHoi && c.XoaTam != true);
    }

    public async Task<IEnumerable<CauHoi>> GetByCLoAsync(EnumCLO maCLo)
    {
        return await _context.CauHois
            .AsNoTracking()
            .Where(c => c.CLO == maCLo && c.XoaTam != true)
            .Include(c => c.CauTraLois)
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetByMaPhanAsync(Guid maPhan)
    {
        return await _context.CauHois
            .AsNoTracking()
            .Where(c => c.MaPhan == maPhan && c.XoaTam != true)
            .Include(c => c.CauTraLois)
            .OrderBy(c => c.MaSoCauHoi)
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        return await _context.CauHois
            .AsNoTracking()
            .Include(c => c.Phan)
            .Include(c => c.CauTraLois)
            .Where(c => c.Phan != null && c.Phan.MaMonHoc == maMonHoc && c.XoaTam != true)
            .OrderByDescending(c => c.NgayTao) // Sửa CreatedDate -> NgayTao
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetByMaDeThiAsync(Guid maDeThi)
    {
        var query = from ctdt in _context.ChiTietDeThis
            join ch in _context.CauHois on ctdt.MaCauHoi equals ch.MaCauHoi
            where ctdt.MaDeThi == maDeThi && ch.XoaTam != true
            select ch;

        return await query
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetByMaCauHoiChasync(Guid maCHCha)
    {
        return await _context.CauHois
            .AsNoTracking()
            .Where(c => c.MaCauHoiCha == maCHCha && c.XoaTam != true)
            .Include(c => c.CauTraLois)
            .OrderBy(c => c.MaSoCauHoi)
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetAllGroupsAsync()
    {
        return await _context.CauHois
            .AsNoTracking()
            .Where(c => c.MaCauHoiCha == null &&
                        (c.LoaiCauHoi == "NH" || c.CauHoiCons.Any()) &&
                        c.XoaTam != true)
            .Include(c => c.Phan)
            .Include(c => c.CauHoiCons.Where(child => child.XoaTam != true))
            .ThenInclude(child => child.CauTraLois)
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetAllGhepNoiAsync()
    {
        return await _context.CauHois
            .Where(c => c.LoaiCauHoi == "GN" && c.MaCauHoiCha == null)
            .Include(c => c.CauHoiCons) // load câu hỏi con
            .ThenInclude(ch => ch.CauTraLois) // load câu trả lời
            .AsSplitQuery() // <--- đây
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetAllMultipleChoiceAsync()
    {
        return await _context.CauHois
            .Where(c => c.LoaiCauHoi == "MN" && c.MaCauHoiCha == null)
            .Include(c => c.CauHoiCons) // load câu hỏi con
            .ThenInclude(ch => ch.CauTraLois) // load câu trả lời
            .AsSplitQuery() // <--- đây
            .ToListAsync();
    }

    public async Task<IEnumerable<CauHoi>> GetAllDienTuAsync()
    {
        return await _context.CauHois
            .Where(c => c.LoaiCauHoi == "DT" && c.MaCauHoiCha == null)
            .Include(c => c.CauHoiCons) // load câu hỏi con
            .ThenInclude(ch => ch.CauTraLois) // load câu trả lời
            .AsSplitQuery() // <--- đây
            .ToListAsync();
    }


    public async Task<CauHoi> AddWithAnswersAsync(CauHoi cauHoi)
    {
        await _context.CauHois.AddAsync(cauHoi);
        await _context.SaveChangesAsync();
        return cauHoi;
    }

    public async Task<CauHoi> UpdateWithAnswersAsync(Guid maCauHoi, CauHoi cauHoi)
    {
        var existing = await _context.CauHois
            .Include(c => c.CauTraLois)
            .FirstOrDefaultAsync(c => c.MaCauHoi == maCauHoi);

        if (existing == null) return null!;

        _context.Entry(existing).CurrentValues.SetValues(cauHoi);

        if (cauHoi.CauTraLois != null)
        {
            foreach (var existingAnswer in existing.CauTraLois.ToList())
            {
                if (!cauHoi.CauTraLois.Any(c => c.MaCauTraLoi == existingAnswer.MaCauTraLoi))
                {
                    _context.CauTraLois.Remove(existingAnswer);
                }
            }

            foreach (var newAnswer in cauHoi.CauTraLois)
            {
                var existingAnswer = existing.CauTraLois
                    .FirstOrDefault(c => c.MaCauTraLoi == newAnswer.MaCauTraLoi && newAnswer.MaCauTraLoi != Guid.Empty);

                if (existingAnswer != null)
                {
                    _context.Entry(existingAnswer).CurrentValues.SetValues(newAnswer);
                }
                else
                {
                    newAnswer.MaCauHoi = maCauHoi;
                    existing.CauTraLois.Add(newAnswer);
                }
            }
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<int> CountAsync(Expression<Func<CauHoi, bool>> predicate)
    {
        return await _context.CauHois.CountAsync(predicate);
    }

    public async Task AddRangeAsync(IEnumerable<CauHoi> cauHois)
    {
        await _context.CauHois.AddRangeAsync(cauHois);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetMaxMaSoCauHoiAsync()
    {
        if (!await _context.CauHois.AnyAsync()) return 0;
        return await _context.CauHois.MaxAsync(c => c.MaSoCauHoi);
    }

    public async Task<PagedResult<CauHoiDto>> GetEssayPagedAsync(
        int page, int pageSize, string? sort, string? search,
        Guid? khoaId, Guid? monHocId, Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Where(c => c.LoaiCauHoi == "TL" && (c.XoaTam == null || c.XoaTam == false));

        // Filter
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (Bổ sung)
        // Logic: Câu hỏi -> Phần -> Môn -> Khoa
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        // Sort
        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            NgayTao = c.NgayTao,
            SoLanDung = c.SoLanDung,
            CLO = c.CLO,
            CauHoiCons = c.CauHoiCons.Select(cc => new CauHoiDto
            {
                MaCauHoi = cc.MaCauHoi,
                MaPhan = cc.MaPhan,
                TenPhan = cc.Phan.TenPhan,
                MaSoCauHoi = cc.MaSoCauHoi,
                NoiDung = cc.NoiDung,
                NgayTao = c.NgayTao,
                CLO = c.CLO,
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public async Task<PagedResult<CauHoiDto>> GetGroupPagedAsync(
        int page, int pageSize, string? sort, string? search,
        Guid? khoaId, Guid? monHocId, Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Include(c => c.CauHoiCons)
            .Where(c => c.LoaiCauHoi == "NH" && (c.XoaTam == null || c.XoaTam == false) && c.MaCauHoiCha == null);

        // Filter
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        // 2. Lọc theo Môn học (BỔ SUNG)
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (BỔ SUNG)
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            HoanVi = c.HoanVi,
            CapDo = c.CapDo,
            SoCauHoiCon = c.SoCauHoiCon,
            SoLanDung = c.SoLanDung,
            DoPhanCach = c.DoPhanCach,
            XoaTam = c.XoaTam,
            NgayTao = c.NgayTao,
            NgaySua = c.NgayCapNhat,
            LoaiCauHoi = c.LoaiCauHoi,
            CauHoiCons = c.CauHoiCons.Select(child => new CauHoiDto
            {
                MaCauHoi = child.MaCauHoi,
                NoiDung = child.NoiDung,
                LoaiCauHoi = child.LoaiCauHoi,
                CauTraLois = child.CauTraLois.Select(ct => new CauTraLoiDto
                {
                    MaCauTraLoi = ct.MaCauTraLoi,
                    NoiDung = ct.NoiDung,
                    LaDapAn = ct.LaDapAn,
                    HoanVi = ct.HoanVi,
                }).ToList()
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public async Task<PagedResult<CauHoiDto>> GetSinglePagedAsync(int page, int pageSize, string? sort, string? search,
        Guid? khoaId, Guid? monHocId,
        Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Where(c => c.LoaiCauHoi == "TN"
                        && (c.XoaTam == null || c.XoaTam == false)
                        && c.MaCauHoiCha == null);

        // Filter + Sort giống trên
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (Bổ sung)
        // Logic: Câu hỏi -> Phần -> Môn -> Khoa
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            HoanVi = c.HoanVi,
            CapDo = c.CapDo,
            LoaiCauHoi = c.LoaiCauHoi,
            SoLanDung = c.SoLanDung,
            CLO = c.CLO,
            NgayTao = c.NgayTao,
            CauTraLois = c.CauTraLois.Select(ct => new CauTraLoiDto
            {
                MaCauTraLoi = ct.MaCauTraLoi,
                NoiDung = ct.NoiDung,
                LaDapAn = ct.LaDapAn,
                HoanVi = ct.HoanVi
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public async Task<PagedResult<CauHoiDto>> GetFillBlankPagedAsync(int page, int pageSize, string? sort,
        string? search, Guid? khoaId, Guid? monHocId,
        Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Include(c => c.CauHoiCons)
            .Where(c => c.LoaiCauHoi == "DT" && (c.XoaTam == null || c.XoaTam == false) && c.MaCauHoiCha == null);

        // Filter
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (Bổ sung)
        // Logic: Câu hỏi -> Phần -> Môn -> Khoa
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            HoanVi = c.HoanVi,
            SoLanDung = c.SoLanDung,
            CapDo = c.CapDo,
            SoCauHoiCon = c.SoCauHoiCon,
            DoPhanCach = c.DoPhanCach,
            XoaTam = c.XoaTam,
            NgayTao = c.NgayTao,
            NgaySua = c.NgayCapNhat,
            LoaiCauHoi = c.LoaiCauHoi,
            CauHoiCons = c.CauHoiCons.Select(child => new CauHoiDto
            {
                MaCauHoi = child.MaCauHoi,
                NoiDung = child.NoiDung,
                LoaiCauHoi = child.LoaiCauHoi,
                CauTraLois = child.CauTraLois.Select(ct => new CauTraLoiDto
                {
                    MaCauTraLoi = ct.MaCauTraLoi,
                    NoiDung = ct.NoiDung,
                    LaDapAn = ct.LaDapAn,
                    HoanVi = ct.HoanVi
                }).ToList()
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public async Task<PagedResult<CauHoiDto>> GetPairingPagedAsync(int page, int pageSize, string? sort, string? search,
        Guid? khoaId, Guid? monHocId,
        Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Include(c => c.CauHoiCons)
            .Where(c => c.LoaiCauHoi == "GN" && (c.XoaTam == null || c.XoaTam == false) && c.MaCauHoiCha == null);

        // Filter
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (Bổ sung)
        // Logic: Câu hỏi -> Phần -> Môn -> Khoa
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            HoanVi = c.HoanVi,
            SoLanDung = c.SoLanDung,
            CapDo = c.CapDo,
            SoCauHoiCon = c.SoCauHoiCon,
            DoPhanCach = c.DoPhanCach,
            XoaTam = c.XoaTam,
            NgayTao = c.NgayTao,
            NgaySua = c.NgayCapNhat,
            LoaiCauHoi = c.LoaiCauHoi,
            CauHoiCons = c.CauHoiCons.Select(child => new CauHoiDto
            {
                MaCauHoi = child.MaCauHoi,
                NoiDung = child.NoiDung,
                LoaiCauHoi = child.LoaiCauHoi,
                CauTraLois = child.CauTraLois.Select(ct => new CauTraLoiDto
                {
                    MaCauTraLoi = ct.MaCauTraLoi,
                    NoiDung = ct.NoiDung,
                    LaDapAn = ct.LaDapAn,
                    HoanVi = ct.HoanVi,
                }).ToList()
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public async Task<PagedResult<CauHoiDto>> GetMultipleChoicePagedAsync(int page, int pageSize, string? sort,
        string? search, Guid? khoaId, Guid? monHocId,
        Guid? phanId)
    {
        var query = _context.CauHois
            .AsNoTracking()
            .Include(c => c.CauTraLois)
            .Include(c => c.Phan)
            .Where(c => c.LoaiCauHoi == "MN"
                        && (c.XoaTam == null || c.XoaTam == false)
                        && c.MaCauHoiCha == null);

        // Filter + Sort giống trên
        if (phanId.HasValue) query = query.Where(c => c.MaPhan == phanId);
        if (monHocId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MaMonHoc == monHocId);

        // 3. Lọc theo Khoa (Bổ sung)
        // Logic: Câu hỏi -> Phần -> Môn -> Khoa
        if (khoaId.HasValue)
            query = query.Where(c => c.Phan != null && c.Phan.MonHoc != null && c.Phan.MonHoc.MaKhoa == khoaId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NoiDung != null && c.NoiDung.Contains(search));

        query = ApplySorting(query, sort);

        var result = query.Select(c => new CauHoiDto
        {
            MaCauHoi = c.MaCauHoi,
            MaPhan = c.MaPhan,
            TenPhan = c.Phan.TenPhan,
            MaSoCauHoi = c.MaSoCauHoi,
            NoiDung = c.NoiDung,
            SoLanDung = c.SoLanDung,
            HoanVi = c.HoanVi,
            CapDo = c.CapDo,
            LoaiCauHoi = c.LoaiCauHoi,
            CLO = c.CLO,
            NgayTao = c.NgayTao,
            CauTraLois = c.CauTraLois.Select(ct => new CauTraLoiDto
            {
                MaCauTraLoi = ct.MaCauTraLoi,
                NoiDung = ct.NoiDung,
                LaDapAn = ct.LaDapAn,
                HoanVi = ct.HoanVi,
            }).ToList()
        });

        return await result.ToPagedResultAsync(page, pageSize);
    }

    public IQueryable<CauHoi> ApplySorting(IQueryable<CauHoi> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(c => c.NgayTao);

        var parts = sort.Split(',');
        var col = parts[0].Trim().ToLower();
        var dir = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

        return (col, dir) switch
        {
            ("ngaytao", "desc") => query.OrderByDescending(c => c.NgayTao),
            ("ngaytao", "asc") => query.OrderBy(c => c.NgayTao),
            ("capdo", "desc") => query.OrderByDescending(c => c.CapDo),
            ("capdo", "asc") => query.OrderBy(c => c.CapDo),
            _ => query.OrderByDescending(c => c.NgayTao)
        };
    }
}