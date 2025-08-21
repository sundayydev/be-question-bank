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
    [Table("Khoa")]
    public class Khoa : ModelBase
    {
        [Key]
        public Guid MaKhoa { get; set; } = Guid.NewGuid();
        [Required]
        public string TenKhoa { get; set; } = string.Empty;
        public string? MoTa { get; set; } = string.Empty;

        [Column("XoaTamKhoa")]
        public bool? XoaTam { get; set; } = false;

        // Navigation property cho các môn học thuộc khoa này
        public ICollection<MonHoc> MonHocs { get; set; } = new List<MonHoc>();
    }
}
