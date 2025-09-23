using System.ComponentModel.DataAnnotations;

namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class CreateYeuCauRutTrichDto
{
    [Required(ErrorMessage = "Mã người dùng không được để trống.")]
    public Guid MaNguoiDung { get; set; }
    [Required(ErrorMessage = "Mã môn học không được để trống.")]
    public Guid MaMonHoc { get; set; }
    public string? NoiDungRutTrich { get; set; }
    public bool? DaXuLy { get; set; } = false;
    public string? GhiChu { get; set; }
    public string? MaTran { get; set; }
}