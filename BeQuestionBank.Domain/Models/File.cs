using BeQuestionBank.Shared.Enums;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Models;
public class File
{
    [Key]
    public Guid MaFile { get; set; } = Guid.NewGuid();
    [ForeignKey("CauHoi")]
    public Guid? MaCauHoi { get; set; }
    public string? TenFile { get; set; }
    public FileType? LoaiFile { get; set; }
    [ForeignKey("CauTraLoi")]
    public Guid? MaCauTraLoi { get; set; }

    public virtual CauHoi? CauHoi { get; set; }
    public virtual CauTraLoi? CauTraLoi { get; set; }
}

