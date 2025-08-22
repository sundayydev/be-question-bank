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
    [Table("MonHoc")]
    public class MonHoc : ModelBase
    {
        [Key]
        public Guid MaMonHoc { get; set; } = Guid.NewGuid();
        public string TenMonHoc { get; set; } = string.Empty;
        public string MaSoMonHoc { get; set; } = string.Empty;
        public int? SoTinChi { get; set; } = 0;
        [Column("XoaTamMonHoc")]
        public bool? XoaTam { get; set; } = false;
        [ForeignKey("Khoa")]
        public Guid MaKhoa { get; set; }

        // Navigation property cho khoa
        [ForeignKey("MaKhoa")]
        public Khoa? Khoa { get; set; }

        // Navigation property cho các phần thuộc môn học này
        public ICollection<Phan> Phans { get; set; } = new List<Phan>();

    }
}
