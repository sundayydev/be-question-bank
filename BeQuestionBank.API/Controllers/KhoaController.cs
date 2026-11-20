using BeQuestionBank.Application.Features.Khoa.Commands;
using BeQuestionBank.Application.Features.Khoa.Queries;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class KhoaController(ISender sender, ILogger<KhoaController>? logger = null) : ControllerBase
{
    private readonly ISender _sender = sender;
    private readonly ILogger<KhoaController>? _logger = logger;

    // GET: api/Khoa/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Tìm Khoa theo Mã Khoa")]
    public async Task<ActionResult<Khoa>> GetKhoaByIdAsync(string id)
    {
        var khoa = await _sender.Send(new GetKhoaByIdQuery(Guid.Parse(id)));
        if (khoa == null)
        {
            return NotFound(ApiResponseFactory.NotFound<Khoa>($"Không tìm thấy Khoa với mã {id}", new { MaKhoa = id }));
        }

        return Ok(ApiResponseFactory.Success(khoa, "Lấy dữ liệu thành công"));
    }

    // GET: api/Khoa
    [HttpGet]
    [SwaggerOperation("Lấy danh sách tất cả Khoa")]
    public async Task<ActionResult<KhoaDto>> GetAllAsync()
    {
        var list = await _sender.Send(new GetAllKhoaQuery());

        var result = list.Select(k => new KhoaDto
        {
            MaKhoa = k.MaKhoa,
            TenKhoa = k.TenKhoa,
            XoaTam = k.XoaTam,
            MoTa = k.MoTa,
            NgayTao = k.NgayTao,
            NgayCapNhat = k.NgayCapNhat,
            DanhSachMonHoc = k.MonHocs?.Select(m => new MonHocDto
            {
                MaSoMonHoc = m.MaSoMonHoc,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                SoTinChi = m.SoTinChi
            }).ToList() ?? new()
        });

        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách Khoa thành công"));
    }

    // POST: api/Khoa
    [HttpPost]
    [SwaggerOperation("Thêm một Khoa")]
    public async Task<IActionResult> CreateKhoaAsync([FromBody] CreateKhoaCommand command)
    {
        var created = await _sender.Send(command);
        return Ok(ApiResponseFactory.Success(created, "Thêm Khoa thành công"));
    }

    // PUT: api/Khoa/{id}
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Cập nhật Khoa")]
    public async Task<IActionResult> UpdateKhoaAsync(string id, [FromBody] UpdateKhoaCommand command)
    {
        command.MaKhoa = Guid.Parse(id);
        var updated = await _sender.Send(command);
        return Ok(ApiResponseFactory.Updated(updated, "Cập nhật Khoa thành công"));
    }

    // DELETE: api/Khoa/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Xóa Khoa (xóa cứng)")]
    public async Task<IActionResult> DeleteKhoaAsync(string id)
    {
        await _sender.Send(new DeleteKhoaCommand(Guid.Parse(id)));
        return Ok(ApiResponseFactory.Success($"Đã xóa Khoa với mã {id} thành công."));
    }

    // PATCH: api/Khoa/{id}/XoaTam
    [HttpPatch("{id}/XoaTam")]
    [SwaggerOperation(Summary = "Xóa Khoa tạm thời")]
    public async Task<IActionResult> SoftDelete(string id)
    {
        var updated = await _sender.Send(new UpdateKhoaCommand { MaKhoa = Guid.Parse(id), XoaTam = true });
        return Ok(ApiResponseFactory.Success($"Đã xóa tạm {updated.TenKhoa} thành công"));
    }

    // PATCH: api/Khoa/{id}/KhoiPhuc
    [HttpPatch("{id}/KhoiPhuc")]
    [SwaggerOperation(Summary = "Khôi phục Khoa đã xóa tạm")]
    public async Task<IActionResult> Restore(string id)
    {
        var updated = await _sender.Send(new UpdateKhoaCommand { MaKhoa = Guid.Parse(id), XoaTam = false });
        return Ok(ApiResponseFactory.Success($"Đã khôi phục {updated.TenKhoa} thành công"));
    }

    // GET: api/Khoa/paged
    [HttpGet("paged")]
    [SwaggerOperation(Summary = "Lấy danh sách Khoa phân trang, tìm kiếm, sắp xếp")]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null)
    {
        var result = await _sender.Send(new GetKhoaPagedQuery(page, pageSize, search, sort));
        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách khoa thành công"));
    }

    // GET: api/Khoa/trashed
    [HttpGet("trashed")]
    [SwaggerOperation("Lấy danh sách Khoa đã xóa tạm (dùng cho thùng rác)")]
    public async Task<IActionResult> GetTrashedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(new GetKhoaTrashedPagedQuery(page, pageSize));
        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách khoa trong thùng rác thành công"));
    }
}
