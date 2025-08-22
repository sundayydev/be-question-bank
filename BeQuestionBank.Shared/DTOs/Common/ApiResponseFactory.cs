using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Common
{
    public class ApiResponseFactory
    {
        public static ApiResponse<T?> Success<T>(T data, string message = "Thành công")
       => new ApiResponse<T?>(StatusCodes.Status200OK, message, data);

        public static ApiResponse<T?> Created<T>(T data, string message = "Tạo thành công")
            => new ApiResponse<T?>(StatusCodes.Status201Created, message, data);

        public static ApiResponse<T?> Updated<T>(T data, string message = "Cập nhật thành công")
            => new ApiResponse<T?>(StatusCodes.Status200OK, message, data);

        public static ApiResponse<object> Deleted(string message = "Xóa thành công")
            => new ApiResponse<object>(StatusCodes.Status200OK, message);

        public static ApiResponse<T?> NotFound<T>(string message = "Không tìm thấy", object? errors = null)
            => new ApiResponse<T?>(StatusCodes.Status404NotFound, message, default, errors);

        public static ApiResponse<T?> ValidationError<T>(string message = "Dữ liệu không hợp lệ", object? errors = null)
            => new ApiResponse<T?>(StatusCodes.Status400BadRequest, message, default, errors);

        public static ApiResponse<object> Unauthorized(string message = "Không có quyền truy cập")
            => new ApiResponse<object>(StatusCodes.Status401Unauthorized, message);

        public static ApiResponse<object> Forbidden(string message = "Bị chặn truy cập")
            => new ApiResponse<object>(StatusCodes.Status403Forbidden, message);

        public static ApiResponse<object> ServerError(string message = "Lỗi hệ thống")
            => new ApiResponse<object>(StatusCodes.Status500InternalServerError, message);
    }
}
