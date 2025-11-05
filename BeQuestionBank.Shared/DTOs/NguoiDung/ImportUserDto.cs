namespace BEQuestionBank.Shared.DTOs.user;

public class ImportUserDto
{
    
    public string TenDangNhap { get; set; }
    public string MatKhau { get; set; }
    public string HoTen { get; set; }
    public string Email { get; set; }
    public int VaiTro { get; set; } 
    public bool BiKhoa { get; set; }
    
    public string TenKhoa { get; set; } 
}