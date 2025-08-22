using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeQuestionBank.Domain.Models;

[Table("CauTraLoi")]
public class CauTraLoi
{
    [Key]
    public Guid MaCauTraLoi { get; set; } = Guid.NewGuid();
    [ForeignKey("CauHoi")]
    public Guid MaCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public bool? HoanVi { get; set; }
    public bool LaDapAn { get; set; }

    public virtual CauHoi? CauHoi { get; set; }
    public ICollection<File>? FileDinhKems { get; set; }
}