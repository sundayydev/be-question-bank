namespace BeQuestionBank.Shared.Configuration
{
    /// <summary>
    /// Configuration settings cho EZP encryption
    /// </summary>
    public class EzpSettings
    {
        /// <summary>
        /// Password dùng để mã hóa/giải mã file EZP
        /// </summary>
        public string EncryptionPassword { get; set; } = string.Empty;

        /// <summary>
        /// Bật/tắt tính năng mã hóa
        /// </summary>
        public bool EnableEncryption { get; set; } = true;
    }
}
