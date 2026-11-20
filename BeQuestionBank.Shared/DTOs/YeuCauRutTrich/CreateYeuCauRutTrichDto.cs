using System.ComponentModel.DataAnnotations;
using BEQuestionBank.Shared.DTOs.MaTran;

namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class CreateYeuCauRutTrichDto
{
    [Required(ErrorMessage = "Mã ngu?i dùng không du?c d? tr?ng.")]
    public Guid MaNguoiDung { get; set; }
    [Required(ErrorMessage = "Mã môn h?c không du?c d? tr?ng.")]
    public Guid MaMonHoc { get; set; }
    public string? NoiDungRutTrich { get; set; }
    public bool? DaXuLy { get; set; } = false;
    public string? GhiChu { get; set; }
    public MaTranDto MaTran { get; set; }
}
