using BeQuestionBank.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Models;
[Table("DeThi")]
public class DeThi : ModelBase
{
    [Key]
    public Guid MaDeThi { get; set; } = Guid.NewGuid();
    [ForeignKey("MonHoc")]
    public Guid MaMonHoc { get; set; }
    public string TenDeThi { get; set; } = string.Empty;
    public bool DaDuyet { get; set; } = false;
    public int? SoCauHoi { get; set; }

    public virtual MonHoc? MonHoc { get; set; }
    public ICollection<ChiTietDeThi>? ChiTietDeThis { get; set; }
}

