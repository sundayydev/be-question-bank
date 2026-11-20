using BeQuestionBank.Application.Common.Interfaces;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using File = BeQuestionBank.Domain.Models.File;

namespace BeQuestionBank.Core.Configurations
{
    public class AppDbContext : DbContext, IApplicationDbContext, IUnitOfWork
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Khoa> Khoas { get; set; }
        public DbSet<MonHoc> MonHocs { get; set; }
        public DbSet<Phan> Phans { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<CauHoi> CauHois { get; set; }
        public DbSet<CauTraLoi> CauTraLois { get; set; }
        public DbSet<DeThi> DeThis { get; set; }
        public DbSet<ChiTietDeThi> ChiTietDeThis { get; set; }
        public DbSet<YeuCauRutTrich> YeuCauRutTrichs { get; set; }
        public DbSet<File> Files { get; set; }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            var tx = await Database.BeginTransactionAsync();
            return new EfDbTransaction(tx);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.Entity<MonHoc>()
            .HasIndex(m => m.MaSoMonHoc)
            .IsUnique();

            // KhoaSeed.Seed(modelBuilder);

            modelBuilder.Entity<CauHoi>()
                .HasOne(c => c.CauHoiCha)
                .WithMany(c => c.CauHoiCons)
                .HasForeignKey(c => c.MaCauHoiCha)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietDeThi>()
                .HasKey(ct => new { ct.MaDeThi, ct.MaCauHoi });

        }
    }
}
