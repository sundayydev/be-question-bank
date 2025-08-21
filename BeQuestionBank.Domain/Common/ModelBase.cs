using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Common
{
    /// <summary>
    /// Base class for all domain models.
    /// </summary>
    public class ModelBase
    {
        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
    }
}
