using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeQuestionBank.Domain.Models;

[Table("ChiTietDeThi")]
public class ChiTietDeThi
{
    [Key]
    [ForeignKey("DeThi")]
    public Guid MaDeThi { get; set; } = Guid.NewGuid();
    [Required]
    [ForeignKey("Phan")]
    public Guid MaPhan { get; set; }
    [Required]
    [ForeignKey("CauHoi")]
    public Guid MaCauHoi { get; set; }
    public int? ThuTu { get; set; }

    public virtual DeThi? DeThi { get; set; }
    public virtual Phan? Phan { get; set; }
    public virtual CauHoi? CauHoi { get; set; }
}