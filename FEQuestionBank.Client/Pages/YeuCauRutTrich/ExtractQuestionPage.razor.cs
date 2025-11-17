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
        protected List<PartDto> _availableParts = new();
        protected HashSet<Guid> _selectedPartIds = new();

        protected bool _isChecking = false;
        protected bool _isProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            var res = await MonHocApi.GetAllMonHocsAsync();
            if (res.Success) _monHocs = res.Data ?? new();
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
            // Xóa phần không còn được chọn
            var toRemove = _maTran.Parts.Where(p => !_selectedPartIds.Contains(p.MaPhan)).ToList();
            foreach (var p in toRemove)
                _maTran.Parts.Remove(p);

            // Thêm phần mới được chọn
            foreach (var id in _selectedPartIds)
            {
                if (!_maTran.Parts.Any(p => p.MaPhan == id))
                {
                    var availPart = _availableParts.FirstOrDefault(x => x.MaPhan == id);
                    var displayName = availPart?.MaPhan.ToString("N").Substring(0, 8) ?? "PHAN";

                    var newPart = new PartDto
                    {
                        MaPhan = id,
                        NumQuestions = 0,
                        Clos = new List<CloDto> { new() { Clo = 1, Num = 0 } },
                        QuestionTypes = new List<QuestionTypeDto> { new() { Loai = "Trắc nghiệm", Num = 0 } }
                    };
                    _maTran.Parts.Add(newPart);
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
            _availableParts.Clear(); // Reset

            if (maMonHoc == Guid.Empty)
            {
                StateHasChanged();
                return;
            }

            try
            {
                var res = await PhanApi.GetPhanByMonHocAsync(maMonHoc);

                if (res.Success && res.Data != null && res.Data.Any())
                {
                    _availableParts = res.Data.Select(p => new PartDto
                    {
                        MaPhan = p.MaPhan,
                        NumQuestions = 0,
                        Clos = new(),
                        QuestionTypes = new()
                    }).ToList();

                    Console.WriteLine($"[DEBUG] Tải {_availableParts.Count} phần thành công.");
                    Snackbar.Add($"Tải {_availableParts.Count} phần.", Severity.Info);
                }
                else
                {
                    Console.WriteLine("[DEBUG] API trả về rỗng hoặc thất bại.");
                    Snackbar.Add("Không có phần nào.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Snackbar.Add("Lỗi kết nối API!", Severity.Error);
            }

            StateHasChanged(); // BẮT BUỘC: Cập nhật UI
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
