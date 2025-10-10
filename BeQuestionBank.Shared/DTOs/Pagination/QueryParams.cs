namespace BeQuestionBank.Shared.DTOs.Pagination;

public class QueryParams
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Sort { get; set; }
    public string? Filter { get; set; }
}