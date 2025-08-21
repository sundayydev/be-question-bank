using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Models
{
    [Table("YeuCauRutTrich")]
    public class YeuCauRutTrich
    {
        [Key]
        public Guid MaYeuCau { get; set; } = Guid.NewGuid();
        [Required]
        [ForeignKey("NguoiDung")]
        public Guid MaNguoiDung { get; set; }
        [Required]
        [ForeignKey("MonHoc")]
        public Guid MaMonHoc { get; set; }
        public string? NoiDungRutTrich { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayYeuCau { get; set; }
        public DateTime? NgayXuLy { get; set; }
        public bool? DaXuLy { get; set; }

        public virtual MonHoc? MonHoc { get; set; }
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
