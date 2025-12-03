using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Common
{
    public static class ApiResponseFactory
    {
        public static ApiResponse<T?> Success<T>(T data, string message = "Thành công")
            => new ApiResponse<T?>(HttpStatusCodes.OK, message, data);

        public static ApiResponse<T?> Created<T>(T data, string message = "Tạo thành công")
            => new ApiResponse<T?>(HttpStatusCodes.Created, message, data);

        public static ApiResponse<T?> Updated<T>(T data, string message = "Cập nhật thành công")
            => new ApiResponse<T?>(HttpStatusCodes.OK, message, data);

        public static ApiResponse<object> Deleted(string message = "Xóa thành công")
            => new ApiResponse<object>(HttpStatusCodes.OK, message);

        public static ApiResponse<T?> NotFound<T>(string message = "Không tìm thấy", object? errors = null)
            => new ApiResponse<T?>(HttpStatusCodes.NotFound, message, default, errors);

        public static ApiResponse<T?> ValidationError<T>(string message = "Dữ liệu không hợp lệ", object? errors = null)
            => new ApiResponse<T?>(HttpStatusCodes.BadRequest, message, default, errors);

        public static ApiResponse<object> Unauthorized(string message = "Không có quyền truy cập")
            => new ApiResponse<object>(HttpStatusCodes.Unauthorized, message);

        public static ApiResponse<object> Forbidden(string message = "Bị chặn truy cập")
            => new ApiResponse<object>(HttpStatusCodes.Forbidden, message);

        public static ApiResponse<object> ServerError(string message = "Lỗi hệ thống")
            => new ApiResponse<object>(HttpStatusCodes.InternalServerError, message);
        
        public static ApiResponse<object> ServerErrorOb(string message = "Lỗi hệ thống", object? errors = null)
            => new ApiResponse<object>(HttpStatusCodes.InternalServerError, message, null, errors);

        public static ApiResponse<T> BadRequest<T>(T data, string message = "Yêu cầu không hợp lệ", object? errors = null)
            => new ApiResponse<T>(HttpStatusCodes.BadRequest, message, data, errors);

        public static ApiResponse<T> Error<T>(int statusCode, string message)
        {
            return new ApiResponse<T>(statusCode, message, default, null);
        }
    }
}
