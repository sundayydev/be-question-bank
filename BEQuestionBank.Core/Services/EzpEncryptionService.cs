using System.Security.Cryptography;
using System.Text;

namespace BEQuestionBank.Core.Services
{
    /// <summary>
    /// Service để mã hóa và giải mã nội dung file EZP với password
    /// Sử dụng AES-256 encryption
    /// </summary>
    public class EzpEncryptionService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000; // PBKDF2 iterations

        /// <summary>
        /// Mã hóa nội dung với password
        /// </summary>
        /// <param name="plainText">Nội dung cần mã hóa (JSON string)</param>
        /// <param name="password">Mật khẩu để mã hóa</param>
        /// <returns>Chuỗi đã được mã hóa (Base64)</returns>
        public string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            // Generate salt
            byte[] salt = GenerateSalt();
            
            // Derive key from password
            byte[] key = DeriveKeyFromPassword(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Write salt (16 bytes) + IV (16 bytes) to the beginning of the stream
                    msEncrypt.Write(salt, 0, salt.Length);
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    // Encrypt the data
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    // Return as Base64 string with a marker to identify encrypted files
                    return "EZP_ENCRYPTED_V1:" + Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Giải mã nội dung với password
        /// </summary>
        /// <param name="cipherText">Nội dung đã mã hóa (Base64)</param>
        /// <param name="password">Mật khẩu để giải mã</param>
        /// <returns>Nội dung gốc (JSON string)</returns>
        public string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            // Check if the file is encrypted
            if (!IsEncrypted(cipherText))
                throw new InvalidOperationException("File không được mã hóa hoặc định dạng không hợp lệ.");

            // Remove the marker
            string base64Data = cipherText.Replace("EZP_ENCRYPTED_V1:", "");
            byte[] cipherBytes = Convert.FromBase64String(base64Data);

            using (var msDecrypt = new MemoryStream(cipherBytes))
            {
                // Read salt (16 bytes)
                byte[] salt = new byte[16];
                msDecrypt.Read(salt, 0, salt.Length);

                // Read IV (16 bytes)
                byte[] iv = new byte[16];
                msDecrypt.Read(iv, 0, iv.Length);

                // Derive key from password
                byte[] key = DeriveKeyFromPassword(password, salt);

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    try
                    {
                        using (var decryptor = aes.CreateDecryptor())
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                    catch (CryptographicException)
                    {
                        throw new UnauthorizedAccessException("Mật khẩu không đúng!");
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem nội dung có được mã hóa hay không
        /// </summary>
        /// <param name="content">Nội dung cần kiểm tra</param>
        /// <returns>True nếu được mã hóa, False nếu không</returns>
        public bool IsEncrypted(string content)
        {
            return !string.IsNullOrEmpty(content) && content.StartsWith("EZP_ENCRYPTED_V1:");
        }

        /// <summary>
        /// Generate random salt
        /// </summary>
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16]; // 128 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// Derive encryption key from password using PBKDF2
        /// </summary>
        private byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(KeySize / 8); // 32 bytes for AES-256
            }
        }
    }
}
