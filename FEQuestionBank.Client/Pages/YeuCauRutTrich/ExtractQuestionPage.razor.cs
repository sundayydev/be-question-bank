using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.Phan;

namespace FEQuestionBank.Client.Pages.DeThi
{
    public partial class ExtractQuestionPage : ComponentBase
    {
        [Inject] IYeuCauRutTrichApiClient YeuCauApi { get; set; } = default!;
        [Inject] IDeThiApiClient DeThiApi { get; set; } = default!;
        [Inject] IMonHocApiClient MonHocApi { get; set; } = default!;
        [Inject] IPhanApiClient PhanApi { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] NavigationManager Navigation { get; set; } = default!;
        protected int? _soCauHoi = null;

        protected MudForm _form = default!;
        protected CreateYeuCauRutTrichDto _model = new();
        protected MaTranDto _maTran = new()
        {
            CloPerPart = false,
            Clos = new(),
            QuestionTypes = new(),
            Parts = new()
        };

        protected List<MonHocDto> _monHocs = new();
        protected List<PhanDto> _availableParts = new();
        protected HashSet<Guid> _selectedPartIds = new();

        protected bool _isChecking = false;
        protected bool _isProcessing = false;
        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", href: "/"),
            new("Yêu cầu rút trích", href: "#", disabled: true),
            new("Tao rut trich", href: "/tools/exam-extract")
        };

        protected override async Task OnInitializedAsync()
        {
            var res = await MonHocApi.GetAllMonHocsAsync();
            if (res.Success && res.Data != null && res.Data.Any())
            {
                _monHocs = res.Data;

                _model.MaMonHoc = _monHocs.First().MaMonHoc;
                
                await OnMonHocChanged(_model.MaMonHoc);
            }

            InitializeDefaultData();
        }

        private void InitializeDefaultData()
        {
            if (!_maTran.CloPerPart)
            {
                if (_maTran.Clos.Count == 0) AddClo();
                if (_maTran.QuestionTypes.Count == 0) AddQuestionType();
            }
        }

        protected void AddClo() => _maTran.Clos.Add(new CloDto { Clo = 1, Num = 0 });
        protected void RemoveClo(CloDto x) => _maTran.Clos.Remove(x);

        protected void AddQuestionType() => _maTran.QuestionTypes.Add(new QuestionTypeDto { Loai = "Trắc nghiệm", Num = 0 });
        protected void RemoveQuestionType(QuestionTypeDto x) => _maTran.QuestionTypes.Remove(x);

        protected void AddPart()
        {
            var newPart = new PartDto
            {
                MaPhan = Guid.NewGuid(),
                NumQuestions = 0,
                Clos = new List<CloDto> { new() { Clo = 1, Num = 0 } },
                QuestionTypes = new List<QuestionTypeDto> { new() { Loai = "Trắc nghiệm", Num = 0 } }
            };
            _maTran.Parts.Add(newPart);
            _selectedPartIds.Add(newPart.MaPhan);
        }

        protected void RemovePart(PartDto part)
        {
            _maTran.Parts.Remove(part);
            _selectedPartIds.Remove(part.MaPhan);
            StateHasChanged();
        }

        private void UpdateMaTranParts()
        {
            // Xóa các phần không còn được chọn
            var toRemove = _maTran.Parts.Where(p => !_selectedPartIds.Contains(p.MaPhan)).ToList();
            foreach (var p in toRemove) _maTran.Parts.Remove(p);

            // Thêm các phần mới được chọn
            foreach (var id in _selectedPartIds)
            {
                if (!_maTran.Parts.Any(p => p.MaPhan == id))
                {
                    _maTran.Parts.Add(new PartDto
                    {
                        MaPhan = id,
                        NumQuestions = 0,
                        Clos = new() { new CloDto { Clo = 1, Num = 0 } },
                        QuestionTypes = new() { new QuestionTypeDto { Loai = "Trắc nghiệm", Num = 0 } }
                    });
                }
            }
            StateHasChanged();
        }
        protected async Task OnPartSelectionChanged(IEnumerable<Guid> selectedIds)
        {
            _selectedPartIds = selectedIds?.ToHashSet() ?? new HashSet<Guid>();
            UpdateMaTranParts();
        }

        protected async Task OnMonHocChanged(Guid maMonHoc)
        {
            _maTran.CloPerPart = false;
            _maTran.Parts.Clear();
            _selectedPartIds.Clear();
            _availableParts.Clear();

            if (maMonHoc == Guid.Empty) return;

            var res = await PhanApi.GetPhanByMonHocAsync(maMonHoc);

            if (res.Success && res.Data != null && res.Data.Any())
            {
                _availableParts = res.Data
                    .Where(p => p.XoaTam != true) 
                    .OrderBy(p => p.ThuTu)
                    .ThenBy(p => p.NgayTao)
                    .ToList();

                Snackbar.Add($"Tải {_availableParts.Count} phần.", Severity.Info);
            }
            else
            {
                Snackbar.Add("Chưa có phần nào cho môn này.", Severity.Warning);
            }

            StateHasChanged();
        }
        

        protected async Task KiemTraMaTran()
        {
            if (_model.MaMonHoc == Guid.Empty) { Snackbar.Add("Chọn môn!", Severity.Warning); return; }
            _isChecking = true;
            StateHasChanged();

            try
            {
                var res = await DeThiApi.CheckQuestionsAsync(_maTran, _model.MaMonHoc);
                Snackbar.Add(res.Success ? "Hợp lệ!" : res.Message ?? "Không đủ câu!", res.Success ? Severity.Success : Severity.Error);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally { _isChecking = false; }
        }

        protected async Task RutTrichDeThi()
        {
            if (_model.MaMonHoc == Guid.Empty) { Snackbar.Add("Chọn môn!", Severity.Warning); return; }

            _isProcessing = true;
            StateHasChanged();

            try
            {
                _model.MaTran = _maTran; // Gán trực tiếp MaTranDto
                var res = await YeuCauApi.CreateAndRutTrichDeThiAsync(_model);

                if (res.Success && res.Data?.MaDeThi != Guid.Empty)
                {
                    Snackbar.Add($"Thành công! Đề: {res.Data.TenDeThi}", Severity.Success);
                    Navigation.NavigateTo($"/dethi/{res.Data.MaDeThi}");
                }
                else
                {
                    Snackbar.Add(res.Message ?? "Lỗi!", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally { _isProcessing = false; }
        }
    }
}
