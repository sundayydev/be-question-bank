using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.Phan;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Phan.Queries;

public record GetPhanByIdQuery(Guid Id) : IRequest<Domain.Models.Phan?>;

public class GetPhanByIdQueryHandler(IPhanRepository repository) : IRequestHandler<GetPhanByIdQuery, Domain.Models.Phan?>
{
    private readonly IPhanRepository _repository = repository;

    public Task<Domain.Models.Phan?> Handle(GetPhanByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id);
    }
}

public record GetPhanTreeByMonHocQuery(Guid MaMonHoc) : IRequest<List<PhanDto>>;

public class GetPhanTreeByMonHocQueryHandler(IPhanRepository repository) : IRequestHandler<GetPhanTreeByMonHocQuery, List<PhanDto>>
{
    private readonly IPhanRepository _repository = repository;

    public async Task<List<PhanDto>> Handle(GetPhanTreeByMonHocQuery request, CancellationToken cancellationToken)
    {
        var phans = (await _repository.GetByMaMonHocAsync(request.MaMonHoc)).Where(p => p != null).Select(p => p!).ToList();
        return BuildTree(phans);
    }

    private static List<PhanDto> BuildTree(IEnumerable<Domain.Models.Phan> phans)
    {
        var phanList = phans.ToList();
        if (phanList.Count == 0) return new List<PhanDto>();

        var lookup = phanList.ToDictionary(p => p.MaPhan, MapToDto);
        var rootNodes = new List<PhanDto>();

        foreach (var p in phanList)
        {
            if (p.MaPhanCha.HasValue && lookup.TryGetValue(p.MaPhanCha.Value, out var parentDto))
            {
                parentDto.PhanCons ??= new List<PhanDto>();
                parentDto.PhanCons.Add(lookup[p.MaPhan]);
            }
            else
            {
                rootNodes.Add(lookup[p.MaPhan]);
            }
        }

        return rootNodes;
    }

    private static PhanDto MapToDto(Domain.Models.Phan p) => new()
    {
        MaPhan = p.MaPhan,
        MaMonHoc = p.MaMonHoc,
        TenPhan = p.TenPhan,
        NoiDung = p.NoiDung,
        ThuTu = p.ThuTu,
        SoLuongCauHoi = p.SoLuongCauHoi,
        MaPhanCha = p.MaPhanCha,
        LaCauHoiNhom = p.LaCauHoiNhom,
        XoaTam = p.XoaTam ?? false,
        MaSoPhan = p.MaSoPhan
    };
}

public record GetPhanTrashedPagedQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<PhanDto>>;

public class GetPhanTrashedPagedQueryHandler(IPhanRepository repository) : IRequestHandler<GetPhanTrashedPagedQuery, PagedResult<PhanDto>>
{
    private readonly IPhanRepository _repository = repository;

    public Task<PagedResult<PhanDto>> Handle(GetPhanTrashedPagedQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetTrashedAsync(request.Page, request.PageSize);
    }
}
