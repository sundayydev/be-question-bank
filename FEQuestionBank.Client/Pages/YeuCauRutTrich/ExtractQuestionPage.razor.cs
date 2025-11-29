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
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] NavigationManager Navigation { get; set; } = default!;

        protected MudForm _form = default!;
        protected CreateYeuCauRutTrichDto _model = new();
        protected MaTranDto _maTran = new()
        {
            TotalQuestions = 10, // Mặc định 10 câu
            CloPerPart = false,
            Clos = new(),
            QuestionTypes = new(),
            Parts = new()
        };
        protected int CalculatedTotalQuestions() => _maTran.Parts.Sum(p => p.NumQuestions);

        protected List<MonHocDto> _monHocs = new();
        protected List<PhanDto> _availableParts = new();
        protected HashSet<Guid> _selectedPartIds = new();

        // State management
        protected bool _isChecking = false;
        protected bool _isProcessing = false;
        protected bool _isValidated = false;
        protected string? _validationMessage = null;
        
        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", href: "/"),
            new("Yêu cầu rút trích", href: "#", disabled: true),
            new("Tạo rút trích", href: "/tools/exam-extract")
        };

        protected override async Task OnInitializedAsync()
        {
            // Bước 1: Load môn học
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
            // Bước 2: Khởi tạo ma trận mặc định (không theo phần)
            if (!_maTran.CloPerPart)
            {
                _maTran.TotalQuestions = 10; // Mặc định 10 câu
                if (_maTran.Clos.Count == 0) AddClo();
                if (_maTran.QuestionTypes.Count == 0) AddQuestionType();
            }
        }

        protected void AddClo() => _maTran.Clos.Add(new CloDto { Clo = 1, Num = 0 });
        protected void RemoveClo(CloDto x)
        {
            _maTran.Clos.Remove(x);
            _isValidated = false; // Reset validation khi thay đổi
        }

        protected void AddQuestionType() => _maTran.QuestionTypes.Add(new QuestionTypeDto { Loai = "TN", Num = 0 });
        protected void RemoveQuestionType(QuestionTypeDto x)
        {
            _maTran.QuestionTypes.Remove(x);
            _isValidated = false;
        }

        protected void AddPart()
        {
            var newPart = new PartDto
            {
                MaPhan = Guid.NewGuid(),
                NumQuestions = 0,
                Clos = new List<CloDto> { new() { Clo = 1, Num = 0 } },
                QuestionTypes = new List<QuestionTypeDto> { new() { Loai = "TN", Num = 0 } }
            };
            _maTran.Parts.Add(newPart);
            _selectedPartIds.Add(newPart.MaPhan);
            _isValidated = false;
        }

        protected void RemovePart(PartDto part)
        {
            _maTran.Parts.Remove(part);
            _selectedPartIds.Remove(part.MaPhan);
            _isValidated = false;
            StateHasChanged();
        }

        private void UpdateMaTranParts()
        {
            var toRemove = _maTran.Parts.Where(p => !_selectedPartIds.Contains(p.MaPhan)).ToList();
            foreach (var p in toRemove) _maTran.Parts.Remove(p);

            foreach (var id in _selectedPartIds)
            {
                if (!_maTran.Parts.Any(p => p.MaPhan == id))
                {
                    _maTran.Parts.Add(new PartDto
                    {
                        MaPhan = id,
                        NumQuestions = 0,
                        Clos = new() { new CloDto { Clo = 1, Num = 0 } },
                        QuestionTypes = new() { new QuestionTypeDto { Loai = "TN", Num = 0 } }
                    });
                }
            }
            _isValidated = false;
            StateHasChanged();
        }

        protected async Task OnPartSelectionChanged(IEnumerable<Guid> selectedIds)
        {
            _selectedPartIds = selectedIds?.ToHashSet() ?? new HashSet<Guid>();
            UpdateMaTranParts();
        }

        // protected async Task OnMonHocChanged(Guid maMonHoc)
        // {
        //     _maTran.CloPerPart = false;
        //     _maTran.Parts.Clear();
        //     _selectedPartIds.Clear();
        //     _availableParts.Clear();
        //     _isValidated = false;
        //
        //     if (maMonHoc == Guid.Empty) return;
        //
        //     var res = await PhanApi.GetPhanByMonHocAsync(maMonHoc);
        //
        //     if (res.Success && res.Data != null && res.Data.Any())
        //     {
        //         _availableParts = res.Data
        //             .Where(p => p.XoaTam != true)
        //             .OrderBy(p => p.ThuTu)
        //             .ThenBy(p => p.NgayTao)
        //             .ToList();
        //
        //         Snackbar.Add($"Đã tải {_availableParts.Count} phần.", Severity.Info);
        //     }
        //     else
        //     {
        //         Snackbar.Add("Chưa có phần nào cho môn này.", Severity.Warning);
        //     }
        //
        //     StateHasChanged();
        // }
        protected async Task OnMonHocChanged(Guid maMonHoc)
        {
            if (_model.MaMonHoc == maMonHoc) return;

            _model.MaMonHoc = maMonHoc;

            // RESET HOÀN TOÀN KHI ĐỔI MÔN – BẮT BUỘC PHẢI LÀM THẾ NÀY
            _maTran = new MaTranDto
            {
                TotalQuestions = 10,
                CloPerPart = false,
                Clos = new List<CloDto>(),
                QuestionTypes = new List<QuestionTypeDto>(),
                Parts = new List<PartDto>()
            };

            _selectedPartIds = new HashSet<Guid>();
            _availableParts = new List<PhanDto>();
            _isValidated = false;
            _validationMessage = null;

            if (maMonHoc == Guid.Empty)
            {
                StateHasChanged();
                return;
            }

            var res = await PhanApi.GetPhanByMonHocAsync(maMonHoc);

            if (res.Success && res.Data != null && res.Data.Any())
            {
                _availableParts = res.Data
                    .Where(p => p.XoaTam != true)
                    .OrderBy(p => p.ThuTu)
                    .ThenBy(p => p.NgayTao)
                    .ToList();

                Snackbar.Add($"Đã tải {_availableParts.Count} phần.", Severity.Info);
            }
            else
            {
                Snackbar.Add("Môn này chưa có phần → Tự động dùng chế độ toàn đề.", Severity.Info);
            }

            // TẠO LẠI DỮ LIỆU MẶC ĐỊNH CHO CHẾ ĐỘ TOÀN ĐỀ
            AddClo();
            AddQuestionType();

            StateHasChanged();
        }

        // Bước 3: Xem trước ma trận
        protected async Task XemTruocMaTran()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_maTran, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var parameters = new DialogParameters
            {
                ["ContentText"] = json,
                ["ButtonText"] = "Đóng",
                ["Color"] = Color.Primary
            };

            var options = new DialogOptions 
            { 
                MaxWidth = MaxWidth.Medium,
                CloseButton = true
            };

            await DialogService.ShowMessageBox(
                "Xem trước ma trận (JSON)", 
                (MarkupString)$"<pre style='background: #f5f5f5; padding: 16px; border-radius: 4px; overflow: auto; max-height: 60vh; font-size: 13px;'>{json}</pre>",
                "Đóng",
                null,
                null,
                options
            );
        }

        // Bước 4: Kiểm tra ma trận
        protected async Task KiemTraMaTran()
        {
            if (_model.MaMonHoc == Guid.Empty)
            {
                Snackbar.Add("Vui lòng chọn môn học!", Severity.Warning);
                return;
            }

            // Validate logic cơ bản
            if (!ValidateMatrixLogic(out var errorMsg))
            {
                Snackbar.Add(errorMsg, Severity.Error);
                return;
            }

            _isChecking = true;
            _isValidated = false;
            StateHasChanged();

            try
            {
                var res = await DeThiApi.CheckQuestionsAsync(_maTran, _model.MaMonHoc);
                
                _isValidated = res.Success;
                _validationMessage = res.Message;

                // Bước 5: Hiển thị kết quả kiểm tra
                if (res.Success)
                {
                    Snackbar.Add("✓ Ma trận hợp lệ! Bạn có thể tiến hành rút trích.", Severity.Success);
                }
                else
                {
                    Snackbar.Add($"✗ {res.Message ?? "Không đủ câu hỏi hoặc ma trận không hợp lệ!"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi kiểm tra: {ex.Message}", Severity.Error);
                _isValidated = false;
            }
            finally
            {
                _isChecking = false;
                StateHasChanged();
            }
        }

        private bool ValidateMatrixLogic(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_maTran.CloPerPart)
            {
                // Kiểm tra chế độ toàn đề
                if (_maTran.TotalQuestions <= 0)
                {
                    errorMessage = "Tổng số câu hỏi phải lớn hơn 0";
                    return false;
                }
                if (_maTran.CloPerPart)
                    _maTran.TotalQuestions = CalculatedTotalQuestions();

                var sumClo = _maTran.Clos.Sum(c => c.Num);
                var sumType = _maTran.QuestionTypes.Sum(q => q.Num);
                if (sumClo != _maTran.TotalQuestions) { errorMessage = $"Tổng CLO ({sumClo}) phải bằng tổng câu ({_maTran.TotalQuestions})"; return false; }
                if (sumType != _maTran.TotalQuestions) { errorMessage = $"Tổng loại câu ({sumType}) phải bằng tổng câu ({_maTran.TotalQuestions})"; return false; }

                if (sumClo != _maTran.TotalQuestions)
                {
                    errorMessage = $"Tổng số câu theo CLO ({sumClo}) phải bằng tổng số câu ({_maTran.TotalQuestions})";
                    return false;
                }

                if (sumType != _maTran.TotalQuestions)
                {
                    errorMessage = $"Tổng số câu theo loại ({sumType}) phải bằng tổng số câu ({_maTran.TotalQuestions})";
                    return false;
                }
            }
            else
            {
                // Kiểm tra chế độ theo phần
                if (!_maTran.Parts.Any())
                {
                    errorMessage = "Vui lòng chọn ít nhất một phần";
                    return false;
                }

                foreach (var part in _maTran.Parts)
                {
                    var sumClo = part.Clos.Sum(c => c.Num);
                    var sumType = part.QuestionTypes.Sum(q => q.Num);

                    if (part.NumQuestions <= 0)
                    {
                        errorMessage = $"Số câu hỏi của phần phải lớn hơn 0";
                        return false;
                    }
                    if (sumClo != part.NumQuestions) { errorMessage = $"Tổng CLO ({sumClo}) phải bằng số câu phần ({part.NumQuestions})"; return false; }
                    if (sumType != part.NumQuestions) { errorMessage = $"Tổng loại câu ({sumType}) phải bằng số câu phần ({part.NumQuestions})"; return false; }

                    if (sumClo != part.NumQuestions)
                    {
                        errorMessage = $"Tổng CLO ({sumClo}) phải bằng số câu của phần ({part.NumQuestions})";
                        return false;
                    }

                    if (sumType != part.NumQuestions)
                    {
                        errorMessage = $"Tổng loại câu ({sumType}) phải bằng số câu của phần ({part.NumQuestions})";
                        return false;
                    }
                }
            }

            return true;
        }

        // Bước 6: Rút trích đề thi (chỉ khi đã validate thành công)
        protected async Task RutTrichDeThi()
        {
            if (!_isValidated)
            {
                Snackbar.Add("Vui lòng kiểm tra ma trận trước khi rút trích!", Severity.Warning);
                return;
            }

            if (_model.MaMonHoc == Guid.Empty)
            {
                Snackbar.Add("Vui lòng chọn môn học!", Severity.Warning);
                return;
            }

            _isProcessing = true;
            StateHasChanged();

            try
            {
                _model.MaTran = _maTran;
                var res = await YeuCauApi.CreateAndRutTrichDeThiAsync(_model);

                if (res.Success && res.Data?.MaDeThi != Guid.Empty)
                {
                    Snackbar.Add($"✓ Rút trích thành công! Đề: {res.Data.TenDeThi}", Severity.Success);
                    Navigation.NavigateTo($"/dethi/{res.Data.MaDeThi}");
                }
                else
                {
                    Snackbar.Add($"✗ {res.Message ?? "Lỗi khi rút trích!"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isProcessing = false;
                StateHasChanged();
            }
        }

        protected void OnMaTranChanged()
        {
            if (_maTran.CloPerPart)
                _maTran.TotalQuestions = CalculatedTotalQuestions();
            _isValidated = false; // Reset validation khi có thay đổi
            StateHasChanged();
        }
        protected void OnCloPerPartChanged(bool newValue)
        {
            // Nếu muốn bật "Theo phần" nhưng môn hiện tại không có phần nào"
            if (newValue && (!_availableParts?.Any() ?? true))
            {
                Snackbar.Add("Môn này chưa có phần nào → Tự động chuyển về chế độ rút trích toàn đề.", Severity.Info);
                _maTran.CloPerPart = false; // ép về false
                InitializeDefaultData();
            }
            else
            {
                _maTran.CloPerPart = newValue;

                if (newValue)
                {
                    // Chuyển sang theo phần
                    _maTran.Clos.Clear();
                    _maTran.QuestionTypes.Clear();

                    if (_selectedPartIds.Any())
                    {
                        UpdateMaTranParts();
                    }
                    // Nếu không có phần nào thì vẫn cho dùng (rút từ toàn bộ ngân hàng)
                    else if (!_availableParts.Any())
                    {
                        Snackbar.Add("Chưa có phần nào. Ma trận sẽ áp dụng cho toàn bộ câu hỏi trong môn.", Severity.Info);
                    }
                }
                else
                {
                    // Chuyển về toàn đề
                    _maTran.Parts.Clear();
                    _selectedPartIds.Clear();
                    InitializeDefaultData();
                }
            }

            _isValidated = false;
            _validationMessage = null;
            OnMaTranChanged();
            StateHasChanged();
        }
    }
}