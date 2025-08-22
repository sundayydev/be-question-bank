using BeQuestionBank.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Models
{
    [Table("Phan")]
    public class Phan : ModelBase
    {
        [Key]
        public Guid MaPhan { get; set; } = Guid.NewGuid();
        [ForeignKey("MonHoc")]
        public Guid MaMonHoc { get; set; }
        [Required]
        public string TenPhan { get; set; } = string.Empty;
        public string? NoiDung { get; set; } = string.Empty;
        public int ThuTu { get; set; } = 0;
        public int SoLuongCauHoi { get; set; } = 0;
        public Guid? MaPhanCha { get; set; } = Guid.Empty;
        public int? MaSoPhan { get; set; } = 0;
        [Column("XoaTamPhan")]
        public bool? XoaTam { get; set; }
        public bool LaCauHoiNhom { get; set; } = false;


        // Navigation property cho phần cha (optional)
        [ForeignKey("MaPhanCha")]
        public Phan? PhanCha { get; set; }
        // Navigation property cho các phần con
        public ICollection<Phan> PhanCon { get; set; } = new List<Phan>();
    }
}
