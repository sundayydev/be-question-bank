using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Services;

public class KhoaService(IKhoaRepository khoaRepository)
{
    private readonly IKhoaRepository _khoaRepository = khoaRepository;

    public async Task<IEnumerable<Khoa>> GetAllKhoasAsync()
    {
        return await _khoaRepository.GetAllAsync();
    }

    public async Task<Khoa?> GetKhoaByIdAsync(Guid id)
    {
        return await _khoaRepository.GetByIdAsync(id);
    }

    public async Task<Khoa?> GetKhoaByTenKhoaAsync(string tenKhoa)
    {
        return await _khoaRepository.GetByTenKhoaAsync(tenKhoa);
    }

    public async Task AddKhoaAsync(Khoa khoa)
    {
        await _khoaRepository.AddAsync(khoa);
    }

    public async Task UpdateKhoaAsync(Khoa khoa)
    {
        await _khoaRepository.UpdateAsync(khoa);
    }

    public async Task DeleteKhoaAsync(Khoa khoa)
    {
        await _khoaRepository.DeleteAsync(khoa);
    }

    public async Task<IEnumerable<Khoa>> FindKhoasAsync(Expression<Func<Khoa, bool>> predicate)
    {
        return await _khoaRepository.FindAsync(predicate);
    }

}

