using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Phan;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Services;

public class PhanService
{
    // Assuming you have a repository for Phan
    private readonly IPhanRepository _phanRepository;
    public PhanService(IPhanRepository phanRepository)
    {
        _phanRepository = phanRepository;
    }

    public async Task<List<PhanDto>> GetTreeByMonHocAsync(Guid maMonHoc)
    {
        var phans = await _phanRepository.GetByMaMonHocAsync(maMonHoc);
        return BuildTree(phans);
    }

    public async Task<List<PhanDto>> GetTreeAsync()
    {
        var phans = await _phanRepository.GetAllAsync();
    return BuildTree(phans);
    }
    private List<PhanDto> BuildTree(IEnumerable<Phan> phans)
    {
        if (!phans.Any()) return new List<PhanDto>();

        var lookup = phans.ToDictionary(p => p.MaPhan, p => MapToDto(p));

        foreach (var phan in lookup.Values)
        {
            if (phan.MaPhanCha.HasValue && lookup.TryGetValue(phan.MaPhanCha.Value, out var parent))
            {
                parent.PhanCons.Add(phan);
            }
        }

        return lookup.Values
            .Where(p => !p.MaPhanCha.HasValue || p.MaPhanCha == Guid.Empty)
            .OrderBy(p => p.ThuTu)
            .ToList();
    }
    private PhanDto MapToDto(Phan phan)
    {
        return new PhanDto
        {
            MaPhan = phan.MaPhan,
            MaMonHoc = phan.MaMonHoc,
            TenPhan = phan.TenPhan,
            NoiDung = phan.NoiDung,
            ThuTu = phan.ThuTu,
            SoLuongCauHoi = phan.SoLuongCauHoi,
            MaPhanCha = phan.MaPhanCha,
            MaSoPhan = phan.MaSoPhan,
            XoaTam = phan.XoaTam,
            LaCauHoiNhom = phan.LaCauHoiNhom,
            NgayTao = phan.NgayTao,
            NgayCapNhat = phan.NgayCapNhat,
            PhanCons = new List<PhanDto>() // sẽ gắn sau
        };
    }

    public async Task<IEnumerable<Phan?>> GetPhansByMaMonHocAsync(Guid maMonHoc)
    {
        return await _phanRepository.GetByMaMonHocAsync(maMonHoc);
    }

    public async Task<Phan?> GetPhanByIdAsync(Guid id)
    {
        return await _phanRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Phan?>> GetAllPhansAsync()
    {
        return await _phanRepository.GetAllAsync();
    }

    public async Task<(bool Success, string Message)> AddPhanAsync(CreatePhanDto newPhan)
    {
        await using var transaction = await _phanRepository.BeginTransactionAsync();
        try
        {
            // Lấy các phần trong môn học
            var phans = await _phanRepository.GetByMaMonHocAsync(newPhan.MaMonHoc);

            // Kiểm tra số thứ tự trùng
            if (newPhan.ThuTu.HasValue && phans.Any(p => p?.ThuTu == newPhan.ThuTu && (p.MaPhanCha == null || p.MaPhanCha == Guid.Empty)))
            {
                return (false, $"Số thứ tự {newPhan.ThuTu} đã tồn tại trong môn học.");
            }
            else
            {
                // Nếu null thì gán max + 1
                int maxSoThuTu = phans
                    .Where(p => p.MaPhanCha == null || p.MaPhanCha == Guid.Empty)
                    .Select(p => p.ThuTu)
                    .DefaultIfEmpty(0)
                    .Max();
                newPhan.ThuTu = maxSoThuTu + 1;
            }

            // Tạo Phan mới
            var phan = new Phan
            {
                MaMonHoc = newPhan.MaMonHoc,
                TenPhan = newPhan.TenPhan,
                NoiDung = newPhan.NoiDung,
                ThuTu = newPhan.ThuTu ?? 1,
                SoLuongCauHoi = newPhan.SoLuongCauHoi,
                MaPhanCha = newPhan.MaPhanCha,
                LaCauHoiNhom = newPhan.LaCauHoiNhom,
                XoaTam = newPhan.XoaTam
            };

            await _phanRepository.AddAsync(phan);

            await transaction.CommitAsync();
            return (true, "Thêm phần thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi khi thêm phần: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> AddPhanWithChildrenAsync(CreatePhanDto newPhan)
    {
        await using var transaction = await _phanRepository.BeginTransactionAsync();
        try
        {
            var phan = new Phan
            {
                MaMonHoc = newPhan.MaMonHoc,
                TenPhan = newPhan.TenPhan,
                NoiDung = newPhan.NoiDung,
                ThuTu = newPhan.ThuTu ?? 0,
                SoLuongCauHoi = newPhan.SoLuongCauHoi,
                MaPhanCha = newPhan.MaPhanCha,
                LaCauHoiNhom = newPhan.LaCauHoiNhom,
                XoaTam = newPhan.XoaTam
            };

            // Validate và gán số thứ tự cho Phan cha + con (theo cây)
            var validateResult = await ValidateAndAssignOrderAsync(phan, phan.MaMonHoc, phan.MaPhanCha);
            if (!validateResult.Success)
                return validateResult;

            // Add vào DbContext
            await _phanRepository.AddAsync(phan);

            await transaction.CommitAsync();
            return (true, "Thêm phần và phần con thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi khi thêm phần: {ex.Message}");
        }
    }

    /// <summary>
    /// Đệ quy validate và gán số thứ tự cho Phan + các PhanCon
    /// </summary>
    private async Task<(bool Success, string Message)> ValidateAndAssignOrderAsync(Phan phan, Guid maMonHoc, Guid? maPhanCha)
    {
        // Lấy các anh em (siblings) cùng cấp trong DB
        var siblings = await _phanRepository.GetSiblingsAsync(maMonHoc, maPhanCha);
        // Kiểm tra trùng số thứ tự
        if (phan.ThuTu > 0 && siblings.Any(s => s.ThuTu == phan.ThuTu))
        {
            return (false, $"Số thứ tự {phan.ThuTu} đã tồn tại trong cấp này.");
        }

        // Nếu chưa có thì gán max+1
        if (phan.ThuTu <= 0)
        {
            int maxOrder = siblings.Any() ? siblings.Max(s => s.ThuTu) : 0;
            phan.ThuTu = maxOrder + 1;
        }

        // Duyệt đệ quy cho các con
        if (phan.PhanCon != null && phan.PhanCon.Any())
        {
            int localMax = 0;
            foreach (var child in phan.PhanCon)
            {
                if (child.ThuTu <= 0)
                {
                    child.ThuTu = ++localMax;
                }
                else
                {
                    bool exists = phan.PhanCon.Count(x => x.ThuTu == child.ThuTu) > 1;
                    if (exists)
                        return (false, $"Số thứ tự {child.ThuTu} bị trùng trong danh sách phần con của {phan.TenPhan}.");
                }

                child.MaMonHoc = maMonHoc;
                child.MaPhanCha = phan.MaPhan;

                var result = await ValidateAndAssignOrderAsync(child, maMonHoc, child.MaPhanCha);
                if (!result.Success)
                    return result;
            }
        }

        return (true, "OK");
    }


    public async Task UpdatePhanAsync(Phan phan)
    {
        await _phanRepository.UpdateAsync(phan);
    }

    public async Task DeletePhanAsync(Phan phan)
    {
        await _phanRepository.DeleteAsync(phan);
    }

    public async Task<IEnumerable<Phan?>> FindPhansAsync(Expression<Func<Phan, bool>> predicate)
    {
        return await _phanRepository.FindAsync(predicate);
    }

    public async Task<Phan?> FirstOrDefaultPhanAsync(Expression<Func<Phan, bool>> predicate)
    {
        return await _phanRepository.FirstOrDefaultAsync(predicate);
    }
}
