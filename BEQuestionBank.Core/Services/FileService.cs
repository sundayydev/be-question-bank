using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using File = BeQuestionBank.Domain.Models.File;

namespace BEQuestionBank.Core.Services;

public class FileService
{
    private readonly IFileRepository _fileRepository;
    private readonly ICauHoiRepository _cauHoiRepository;
    private readonly string _storagePath;

    public FileService(IFileRepository fileRepository, ICauHoiRepository cauHoiRepository)
    {
        _fileRepository = fileRepository;
        _cauHoiRepository = cauHoiRepository;
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    /// <summary>
    /// Lấy danh sách file có phân trang
    /// </summary>
    public async Task<PagedResult<FileDto>> GetFilesPagedAsync(int page, int pageSize, string? sort, string? search, FileType? fileType)
    {
        var allFiles = await _fileRepository.GetAllAsync();
        IEnumerable<File> files = allFiles;

        // Lọc theo loại file
        if (fileType.HasValue)
        {
            files = files.Where(f => f.LoaiFile == fileType.Value);
        }

        // Tìm kiếm theo tên file
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            files = files.Where(f => f.TenFile != null && f.TenFile.ToLower().Contains(searchLower));
        }

        // Sắp xếp
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var sortParts = sort.Split(',');
            var sortField = sortParts[0].Trim();
            var sortDirection = sortParts.Length > 1 ? sortParts[1].Trim().ToLower() : "asc";

            files = sortField.ToLower() switch
            {
                "tenfile" => sortDirection == "desc" ? files.OrderByDescending(f => f.TenFile) : files.OrderBy(f => f.TenFile),
                "macauhoi" => sortDirection == "desc" ? files.OrderByDescending(f => f.MaCauHoi) : files.OrderBy(f => f.MaCauHoi),
                _ => files.OrderByDescending(f => f.MaFile)
            };
        }
        else
        {
            files = files.OrderByDescending(f => f.MaFile);
        }

        // Đếm tổng số
        var totalCount = files.Count();

        // Phân trang
        var pagedFiles = files
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map sang DTO
        var fileDtos = pagedFiles.Select(f => new FileDto
        {
            MaFile = f.MaFile,
            TenFile = f.TenFile ?? string.Empty,
            LoaiFile = f.LoaiFile ?? FileType.Audio,
            Url = f.LoaiFile == FileType.Audio ? $"/media/{f.TenFile}" : null,
            MaCauHoi = f.MaCauHoi,
            MaCauTraLoi = f.MaCauTraLoi,
            XoaTam = false
        }).ToList();

        return new PagedResult<FileDto>
        {
            Items = fileDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Xóa file (soft delete)
    /// </summary>
    public async Task<bool> DeleteFileAsync(Guid maFile)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(maFile);
            if (file == null)
                return false;

            // Xóa file vật lý nếu có
            if (!string.IsNullOrEmpty(file.TenFile))
            {
                var filePath = Path.Combine(_storagePath, "media", file.TenFile);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Xóa khỏi database
            await _fileRepository.DeleteAsync(file);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lấy ID câu hỏi liên kết với file
    /// </summary>
    public async Task<Guid?> GetCauHoiIdByFileIdAsync(Guid maFile)
    {
        var file = await _fileRepository.GetByIdAsync(maFile);
        return file?.MaCauHoi;
    }
}
