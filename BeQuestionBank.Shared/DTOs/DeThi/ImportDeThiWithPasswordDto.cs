namespace BeQuestionBank.Shared.DTOs.DeThi
{
    /// <summary>
    /// DTO cho việc import đề thi có bảo vệ password
    /// </summary>
    public class ImportDeThiWithPasswordDto
    {
        /// <summary>
        /// Mật khẩu để giải mã file EZP (nếu file được bảo vệ)
        /// </summary>
        public string? Password { get; set; }
    }
}
