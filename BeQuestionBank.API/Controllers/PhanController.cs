using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Phan;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Numerics;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PhanController(PhanService service) : ControllerBase
{
    private readonly PhanService _service = service;

    // GET: api/Phan/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Lấy Phần theo ID")]
    public async Task<IActionResult> GetPhanById(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
            }
            var phan = await _service.GetTreeByMonHocAsync(id);
            if (phan == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần với ID đã cho."));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>(phan, "Lấy phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }

    }

    // GET: api/Phan
    [HttpGet]
    [SwaggerOperation("Lấy tất cả Phần")]
    public async Task<IActionResult> GetAllPhans()
    {
        var list = await _service.GetTreeAsync();
        if (list == null || !list.Any())
        {
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần nào."));
        }
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(list, "Lấy danh sách phần thành công!"));
    }

    // POST: api/Phan
    [HttpPost]
    [SwaggerOperation("Thêm Phần mới")]
    public async Task<IActionResult> AddPhan([FromBody] CreatePhanDto phanCreateDto)
    {
        if (phanCreateDto == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ."));
        }
        try
        {

            (bool success, string message) = (false, string.Empty);
            // Kiểm tra dữ liệu đầu vào
            if (phanCreateDto.MaPhanCha != null && phanCreateDto.MaPhanCha != Guid.Empty)
            {
                (success, message) = await _service.AddPhanWithChildrenAsync(phanCreateDto);
            }
            else
            {
                (success, message) = await _service.AddPhanAsync(phanCreateDto);
            }

            if (!success)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>(message));
            }

            return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success<Object>(phanCreateDto, "Thêm phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // PATCH: api/Phan/{id}
    [HttpPatch("{id}")]
    [SwaggerOperation("Cập nhật Phần")]
    public async Task<IActionResult> UpdatePhan(Guid id, [FromBody] UpdatePhanDto phanUpdateDto)
    {
        if (phanUpdateDto == null || id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ hoặc ID không hợp lệ."));
        }
        try
        {
            // Cập nhật thông tin phần
            var existingPhan = await _service.GetPhanByIdAsync(id);
            if (existingPhan == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần với ID đã cho."));
            }
            // Cập nhật các trường cần thiết
            existingPhan.MaMonHoc = phanUpdateDto.MaMonHoc;
            existingPhan.TenPhan = phanUpdateDto.TenPhan;
            existingPhan.NoiDung = phanUpdateDto.NoiDung;
            existingPhan.ThuTu = phanUpdateDto.ThuTu;
            existingPhan.SoLuongCauHoi = phanUpdateDto.SoLuongCauHoi;
            existingPhan.MaPhanCha = phanUpdateDto.MaPhanCha;
            existingPhan.LaCauHoiNhom = phanUpdateDto.LaCauHoiNhom;
            existingPhan.MaSoPhan = phanUpdateDto.MaSoPhan;
            existingPhan.XoaTam = phanUpdateDto.XoaTam;
            existingPhan.NgayCapNhat = DateTime.UtcNow;
            // Lưu thay đổi
            await _service.UpdatePhanAsync(existingPhan);
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>(phanUpdateDto, "Cập nhật phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // DELETE: api/Phan/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa Phần")]
    public async Task<IActionResult> DeletePhan(Guid id)
    {
        if (id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
        }
        try
        {
            // Xóa phần
            var phan = await _service.GetPhanByIdAsync(id);
            if (phan == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần với ID đã cho."));
            }
            await _service.DeletePhanAsync(phan);
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>("Xóa phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // PATCH: api/Phan/{id}/XoaTam
    [HttpPatch("{id}/XoaTam")]
    [SwaggerOperation("Xóa tạm Phần")]
    public async Task<IActionResult> SoftDeletePhan(Guid id)
    {
        if (id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
        }
        try
        {
            // Xóa tạm phần
            var phan = await _service.GetPhanByIdAsync(id);
            if (phan == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần với ID đã cho."));
            }
            phan.XoaTam = true;
            await _service.UpdatePhanAsync(phan);
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>("Xóa tạm phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // PATCH: api/Phan/{id}/XoaTam
    [HttpPatch("{id}/KhoiPhuc")]
    [SwaggerOperation("Xóa tạm Phần")]
    public async Task<IActionResult> Restore(Guid id)
    {
        if (id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
        }
        try
        {
            // Xóa tạm phần
            var phan = await _service.GetPhanByIdAsync(id);
            if (phan == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy phần với ID đã cho."));
            }
            phan.XoaTam = false;
            await _service.UpdatePhanAsync(phan);
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>("Xóa tạm phần thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
    [HttpGet("monhoc/{maMonHoc}")]
    public async Task<IActionResult> GetByMonHoc(Guid maMonHoc)
    {
        if (maMonHoc == Guid.Empty)
        {
            return BadRequest(ApiResponseFactory.ValidationError<object>("Mã môn học không hợp lệ."));
        }

    
        var result = await _service.GetTreeByMonHocAsync(maMonHoc);

        return Ok(new ApiResponse<List<PhanDto>>
        {
            StatusCode = 200,
            Message = "Lấy danh sách phần theo môn học thành công",
            Data = result,
        });
    }
    
}
