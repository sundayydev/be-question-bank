namespace BeQuestionBank.Shared.DTOs.DeThi
{
    /// <summary>
    /// DTO cho việc export đề thi với bảo vệ password
    /// </summary>
    public class ExportDeThiWithPasswordDto
    {
        /// <summary>
        /// Mật khẩu để bảo vệ file EZP (optional)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Có sử dụng password hay không
        /// </summary>
        public bool UsePassword { get; set; } = false;
    }
}
