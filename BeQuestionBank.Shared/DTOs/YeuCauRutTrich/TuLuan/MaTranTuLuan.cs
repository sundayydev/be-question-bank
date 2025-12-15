// File: BeQuestionBank.Shared.DTOs.YeuCauRutTrich/MaTranTuLuan.cs

using BEQuestionBank.Shared.DTOs.MaTran;

namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class MaTranTuLuan
{
    public int TotalQuestions { get; set; }

    public List<PartTuLuanDto>? Parts { get; set; } = new();
}

public class PartTuLuanDto
{
    public Guid Part { get; set; }

    public List<CloDto> Clos { get; set; } = new(); // Mỗi cặp = 1 câu lớn có đúng Num câu con
}