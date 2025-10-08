namespace FEQuestionBank.Client.Share;

public class KhoaDto
{
    public Guid MaKhoa { get; set; }
    public required string TenKhoa { get; set; }
    public bool? XoaTam { get; set; }
}