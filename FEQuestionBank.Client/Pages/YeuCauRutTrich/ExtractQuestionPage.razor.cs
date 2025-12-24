using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using BeQuestionBank.Shared.DTOs.Khoa;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Security.Claims;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Implementation;

namespace FEQuestionBank.Client.Pages.DeThi
{
    public partial class ExtractQuestionPage : ComponentBase
    {
        [Inject] IYeuCauRutTrichApiClient YeuCauApi { get; set; } = default!;
        [Inject] CustomAuthStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] IDeThiApiClient DeThiApi { get; set; } = default!;
        [Inject] IMonHocApiClient MonHocApi { get; set; } = default!;
        [Inject] IKhoaApiClient KhoaApi { get; set; } = default!;
        [Inject] IPhanApiClient PhanApi { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] NavigationManager Navigation { get; set; } = default!;

        protected MudForm _form = default!;
        protected CreateYeuCauRutTrichDto _model = new();
        protected MaTranDto _maTran = new()
        {
            TotalQuestions = 0,
            CloPerPart = false,
            Clos = new(),
            QuestionTypes = new(),
            Parts = new()
        };

        protected List<KhoaDto> _khoas = new();
        protected Guid _selectedKhoaId = Guid.Empty;
        protected List<MonHocDto> _monHocs = new();
        protected List<PhanDto> _availableParts = new();
        protected HashSet<Guid> _selectedPartIds = new();
        protected bool _isLoadingMonHoc = false;

        // Ma trận CLO x Loại câu hỏi
        protected List<int> _matrixClos = new() { 1, 2, 3 }; // CLO1, CLO2, CLO3 mặc định
        protected Dictionary<(int clo, string loai), int> _matrixData = new();

        // Các loại câu hỏi cần hiển thị
        // NH: Câu hỏi nhóm - đếm số câu hỏi con + số câu đơn
        // TL: Tự luận - đếm số câu hỏi con
        // TN, MN, GN, DT: Mỗi câu tính là 1
        protected Dictionary<string, string> _questionTypes = new()
        {
            { "NH", "Câu hỏi nhóm" },
            { "TL", "Tự luận" },
            { "TN", "Trắc nghiệm 1 đáp án" },
            { "MN", "Trắc nghiệm nhiều đáp án" },
            { "GN", "Ghép nối" },
            { "DT", "Điền từ" }
        };

        // Các loại câu hỏi đếm theo câu con (NH, TL)
        // NH: đếm số câu hỏi con + số câu đơn trong nhóm
        // TL: đếm số câu hỏi con
        protected static readonly HashSet<string> _countChildQuestionTypes = new() { "NH", "TL" };

        // Số câu hỏi con trung bình cho mỗi loại (NH, TL)
        // Key: loại câu hỏi, Value: số câu con trung bình
        protected Dictionary<string, int> _childQuestionCounts = new()
        {
            { "NH", 3 }, // Mặc định mỗi nhóm có 3 câu con
            { "TL", 2 }  // Mặc định mỗi câu TL có 2 câu con
        };

        // Ma trận theo phần: Dictionary<MaPhan, Dictionary<(clo, loai), int>>
        protected Dictionary<Guid, Dictionary<(int clo, string loai), int>> _partMatrixData = new();
        protected Dictionary<Guid, List<int>> _partClos = new(); // CLO cho từng phần

        // State management
        protected bool _isChecking = false;
        protected bool _isProcessing = false;
        protected bool _isValidated = false;
        protected string? _validationMessage = null;

        // Expected question count
        protected int? _expectedTotalQuestions = null;

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", href: "/"),
            new("Yêu cầu rút trích", href: "#", disabled: true),
            new("Tạo rút trích", href: "/tools/exam-extract")
        };

        protected override async Task OnInitializedAsync()
        {
            // Lấy authentication state
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated ?? false)
            {
                // Lấy claim
                var userIdClaim = user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _model.MaNguoiDung = userId;
                    Console.WriteLine($"Current user id: {_model.MaNguoiDung}");
                }
                else
                {
                    Console.WriteLine("WARNING: No valid user id claim found.");
                }
            }
            else
            {
                Console.WriteLine("User is not authenticated.");
            }

            // Load danh sách Khoa
            var resKhoa = await KhoaApi.GetAllKhoasAsync();
            if (resKhoa.Success && resKhoa.Data != null && resKhoa.Data.Any())
            {
                _khoas = resKhoa.Data.Where(k => k.XoaTam != true).ToList();
                if (_khoas.Any())
                {
                    _selectedKhoaId = _khoas.First().MaKhoa;
                    await OnKhoaChanged(_selectedKhoaId);
                }
            }

            InitializeDefaultMatrix();
        }

        protected async Task OnKhoaChanged(Guid maKhoa)
        {
            _selectedKhoaId = maKhoa;
            _monHocs = new List<MonHocDto>();
            _model.MaMonHoc = Guid.Empty;

            // Reset tất cả state liên quan đến môn học
            _availableParts = new List<PhanDto>();
            _selectedPartIds = new HashSet<Guid>();
            _maTran = new MaTranDto
            {
                TotalQuestions = 0,
                CloPerPart = false,
                Clos = new List<CloDto>(),
                QuestionTypes = new List<QuestionTypeDto>(),
                Parts = new List<PartDto>()
            };
            _matrixData = new Dictionary<(int clo, string loai), int>();
            _partMatrixData = new Dictionary<Guid, Dictionary<(int clo, string loai), int>>();
            _partClos = new Dictionary<Guid, List<int>>();
            _matrixClos = new List<int> { 1, 2, 3 };
            _isValidated = false;
            _validationMessage = null;

            if (maKhoa == Guid.Empty)
            {
                StateHasChanged();
                return;
            }

            _isLoadingMonHoc = true;
            StateHasChanged();

            try
            {
                var res = await MonHocApi.GetMonHocsByMaKhoaAsync(maKhoa);
                if (res.Success && res.Data != null && res.Data.Any())
                {
                    _monHocs = res.Data;
                    _model.MaMonHoc = _monHocs.First().MaMonHoc;
                    await OnMonHocChanged(_model.MaMonHoc);
                }
                else
                {
                    Snackbar.Add("Khoa này chưa có môn học nào.", Severity.Info);
                }
            }
            finally
            {
                _isLoadingMonHoc = false;
                StateHasChanged();
            }
        }

        private void InitializeDefaultMatrix()
        {
            // Khởi tạo ma trận mặc định với giá trị 0
            foreach (var clo in _matrixClos)
            {
                foreach (var loai in _questionTypes.Keys)
                {
                    _matrixData[(clo, loai)] = 0;
                }
            }
        }

        #region Logic đếm câu hỏi

        /// <summary>
        /// Kiểm tra xem loại câu hỏi có đếm theo câu con không
        /// NH: đếm số câu hỏi con + số câu đơn trong nhóm
        /// TL: đếm số câu hỏi con
        /// </summary>
        protected bool IsCountingChildQuestions(string loai)
        {
            return _countChildQuestionTypes.Contains(loai);
        }

        /// <summary>
        /// Lấy màu cho icon info dựa trên loại câu hỏi
        /// </summary>
        protected Color GetQuestionTypeInfoColor(string loai)
        {
            return IsCountingChildQuestions(loai) ? Color.Warning : Color.Info;
        }

        #endregion

        #region Ma trận toàn đề

        protected int GetMatrixValue(int clo, string loai)
        {
            return _matrixData.TryGetValue((clo, loai), out var val) ? val : 0;
        }

        protected void SetMatrixValue(int clo, string loai, int value)
        {
            _matrixData[(clo, loai)] = value;
            _isValidated = false;
            UpdateMaTranFromMatrix();
            StateHasChanged();
        }

        protected void AddClo()
        {
            var nextClo = _matrixClos.Any() ? _matrixClos.Max() + 1 : 1;
            if (nextClo <= 5) // Giới hạn CLO từ 1-5
            {
                _matrixClos.Add(nextClo);
                foreach (var loai in _questionTypes.Keys)
                {
                    _matrixData[(nextClo, loai)] = 0;
                }
                _isValidated = false;
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("CLO tối đa là 5!", Severity.Warning);
            }
        }

        protected void RemoveCloCloumn(int clo)
        {
            if (_matrixClos.Count <= 1)
            {
                Snackbar.Add("Phải có ít nhất 1 CLO!", Severity.Warning);
                return;
            }

            _matrixClos.Remove(clo);
            foreach (var loai in _questionTypes.Keys)
            {
                _matrixData.Remove((clo, loai));
            }
            _isValidated = false;
            UpdateMaTranFromMatrix();
            StateHasChanged();
        }

        protected int GetRowTotal(string loai)
        {
            return _matrixClos.Sum(clo => GetMatrixValue(clo, loai));
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của một loại (đã nhân với số câu con nếu là NH/TL)
        /// </summary>
        protected int GetRowTotalWithChildren(string loai)
        {
            var rawTotal = GetRowTotal(loai);
            if (IsCountingChildQuestions(loai))
            {
                var childCount = GetChildQuestionCount(loai);
                return rawTotal * childCount;
            }
            return rawTotal;
        }

        protected int GetColumnTotal(int clo)
        {
            return _questionTypes.Keys.Sum(loai => GetMatrixValue(clo, loai));
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của một CLO (đã tính số câu con)
        /// </summary>
        protected int GetColumnTotalWithChildren(int clo)
        {
            return _questionTypes.Keys.Sum(loai =>
            {
                var value = GetMatrixValue(clo, loai);
                if (IsCountingChildQuestions(loai))
                {
                    return value * GetChildQuestionCount(loai);
                }
                return value;
            });
        }

        /// <summary>
        /// Lấy tổng số câu hỏi gốc (chưa tính câu con)
        /// </summary>
        protected int GetGrandTotal()
        {
            return _matrixData.Values.Sum();
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế (đã tính câu con cho NH/TL)
        /// </summary>
        protected int GetGrandTotalWithChildren()
        {
            return _questionTypes.Keys.Sum(loai => GetRowTotalWithChildren(loai));
        }

        /// <summary>
        /// Lấy số câu con cho loại câu hỏi
        /// </summary>
        protected int GetChildQuestionCount(string loai)
        {
            return _childQuestionCounts.TryGetValue(loai, out var count) ? count : 1;
        }

        /// <summary>
        /// Đặt số câu con cho loại câu hỏi
        /// </summary>
        protected void SetChildQuestionCount(string loai, int count)
        {
            if (count < 1) count = 1;
            _childQuestionCounts[loai] = count;
            _isValidated = false;
            StateHasChanged();
        }

        private void UpdateMaTranFromMatrix()
        {
            // Cập nhật _maTran từ ma trận
            // Tạo MatrixCells chi tiết cho từng ô (CLO × Loại)
            var matrixCells = new List<MatrixCellDto>();

            foreach (var clo in _matrixClos)
            {
                foreach (var loai in _questionTypes.Keys)
                {
                    var value = GetMatrixValue(clo, loai);
                    if (value > 0)
                    {
                        // Với loại NH/TL, gán SubQuestionCount từ cấu hình
                        int? subQuestionCount = null;
                        if (IsCountingChildQuestions(loai))
                        {
                            subQuestionCount = GetChildQuestionCount(loai);
                        }

                        matrixCells.Add(new MatrixCellDto
                        {
                            Clo = clo,
                            Loai = loai,
                            Num = value,
                            SubQuestionCount = subQuestionCount
                        });
                    }
                }
            }

            _maTran.MatrixCells = matrixCells;

            // Gộp theo CLO như cấu trúc ban đầu (cho tương thích ngược)
            _maTran.Clos = _matrixClos
                .Select(clo =>
                {
                    // Kiểm tra xem CLO này có loại NH hoặc TL không
                    bool hasNH = GetMatrixValue(clo, "NH") > 0;
                    bool hasTL = GetMatrixValue(clo, "TL") > 0;

                    // Lấy SubQuestionCount nếu có loại NH/TL
                    int? subQuestionCount = null;
                    if (hasNH)
                    {
                        subQuestionCount = GetChildQuestionCount("NH");
                    }
                    else if (hasTL)
                    {
                        subQuestionCount = GetChildQuestionCount("TL");
                    }

                    return new CloDto
                    {
                        Clo = clo,
                        Num = GetColumnTotal(clo),
                        SubQuestionCount = subQuestionCount
                    };
                })
                .Where(c => c.Num > 0)
                .ToList();

            _maTran.QuestionTypes = _questionTypes.Keys
                .Select(loai => new QuestionTypeDto
                {
                    Loai = loai,
                    Num = GetRowTotal(loai)
                })
                .Where(q => q.Num > 0)
                .ToList();

            _maTran.TotalQuestions = GetGrandTotal();
        }

        #endregion

        #region Ma trận theo phần

        protected List<int> GetPartClos(PartDto part)
        {
            if (!_partClos.ContainsKey(part.MaPhan))
            {
                _partClos[part.MaPhan] = new List<int> { 1, 2, 3 };
            }
            return _partClos[part.MaPhan];
        }

        protected void AddPartClo(PartDto part)
        {
            var clos = GetPartClos(part);
            var nextClo = clos.Any() ? clos.Max() + 1 : 1;
            if (nextClo <= 5)
            {
                clos.Add(nextClo);
                if (!_partMatrixData.ContainsKey(part.MaPhan))
                {
                    _partMatrixData[part.MaPhan] = new Dictionary<(int clo, string loai), int>();
                }
                foreach (var loai in _questionTypes.Keys)
                {
                    _partMatrixData[part.MaPhan][(nextClo, loai)] = 0;
                }
                _isValidated = false;
                StateHasChanged();
            }
            else
            {
                Snackbar.Add("CLO tối đa là 5!", Severity.Warning);
            }
        }

        protected void RemovePartClo(PartDto part, int clo)
        {
            var clos = GetPartClos(part);
            if (clos.Count <= 1)
            {
                Snackbar.Add("Phải có ít nhất 1 CLO!", Severity.Warning);
                return;
            }

            clos.Remove(clo);
            if (_partMatrixData.ContainsKey(part.MaPhan))
            {
                foreach (var loai in _questionTypes.Keys)
                {
                    _partMatrixData[part.MaPhan].Remove((clo, loai));
                }
            }
            _isValidated = false;
            UpdatePartMaTranFromMatrix(part);
            StateHasChanged();
        }

        protected int GetPartMatrixValue(PartDto part, int clo, string loai)
        {
            if (_partMatrixData.TryGetValue(part.MaPhan, out var matrix))
            {
                return matrix.TryGetValue((clo, loai), out var val) ? val : 0;
            }
            return 0;
        }

        protected void SetPartMatrixValue(PartDto part, int clo, string loai, int value)
        {
            if (!_partMatrixData.ContainsKey(part.MaPhan))
            {
                _partMatrixData[part.MaPhan] = new Dictionary<(int clo, string loai), int>();
            }
            _partMatrixData[part.MaPhan][(clo, loai)] = value;
            _isValidated = false;
            UpdatePartMaTranFromMatrix(part);
            StateHasChanged();
        }

        protected int GetPartRowTotal(PartDto part, string loai)
        {
            var clos = GetPartClos(part);
            return clos.Sum(clo => GetPartMatrixValue(part, clo, loai));
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của một loại trong phần (đã nhân với số câu con nếu là NH/TL)
        /// </summary>
        protected int GetPartRowTotalWithChildren(PartDto part, string loai)
        {
            var rawTotal = GetPartRowTotal(part, loai);
            if (IsCountingChildQuestions(loai))
            {
                return rawTotal * GetChildQuestionCount(loai);
            }
            return rawTotal;
        }

        protected int GetPartColumnTotal(PartDto part, int clo)
        {
            return _questionTypes.Keys.Sum(loai => GetPartMatrixValue(part, clo, loai));
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của một CLO trong phần (đã tính số câu con)
        /// </summary>
        protected int GetPartColumnTotalWithChildren(PartDto part, int clo)
        {
            return _questionTypes.Keys.Sum(loai =>
            {
                var value = GetPartMatrixValue(part, clo, loai);
                if (IsCountingChildQuestions(loai))
                {
                    return value * GetChildQuestionCount(loai);
                }
                return value;
            });
        }

        protected int GetPartTotal(PartDto part)
        {
            if (_partMatrixData.TryGetValue(part.MaPhan, out var matrix))
            {
                return matrix.Values.Sum();
            }
            return 0;
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của phần (đã tính câu con cho NH/TL)
        /// </summary>
        protected int GetPartTotalWithChildren(PartDto part)
        {
            return _questionTypes.Keys.Sum(loai => GetPartRowTotalWithChildren(part, loai));
        }

        protected int GetAllPartsTotal()
        {
            return _maTran.Parts?.Sum(p => GetPartTotal(p)) ?? 0;
        }

        /// <summary>
        /// Lấy tổng số câu hỏi thực tế của tất cả các phần (đã tính câu con cho NH/TL)
        /// </summary>
        protected int GetAllPartsTotalWithChildren()
        {
            return _maTran.Parts?.Sum(p => GetPartTotalWithChildren(p)) ?? 0;
        }

        private void UpdatePartMaTranFromMatrix(PartDto part)
        {
            var clos = GetPartClos(part);

            // Tạo MatrixCells chi tiết cho từng ô (CLO × Loại)
            var matrixCells = new List<MatrixCellDto>();

            foreach (var clo in clos)
            {
                foreach (var loai in _questionTypes.Keys)
                {
                    var value = GetPartMatrixValue(part, clo, loai);
                    if (value > 0)
                    {
                        // Với loại NH/TL, gán SubQuestionCount từ cấu hình
                        int? subQuestionCount = null;
                        if (IsCountingChildQuestions(loai))
                        {
                            subQuestionCount = GetChildQuestionCount(loai);
                        }

                        matrixCells.Add(new MatrixCellDto
                        {
                            Clo = clo,
                            Loai = loai,
                            Num = value,
                            SubQuestionCount = subQuestionCount
                        });
                    }
                }
            }

            part.MatrixCells = matrixCells;

            // Gộp theo CLO như cấu trúc ban đầu (cho tương thích ngược)
            part.Clos = clos
                .Select(clo =>
                {
                    // Kiểm tra xem CLO này có loại NH hoặc TL không
                    bool hasNH = GetPartMatrixValue(part, clo, "NH") > 0;
                    bool hasTL = GetPartMatrixValue(part, clo, "TL") > 0;

                    // Lấy SubQuestionCount nếu có loại NH/TL
                    int? subQuestionCount = null;
                    if (hasNH)
                    {
                        subQuestionCount = GetChildQuestionCount("NH");
                    }
                    else if (hasTL)
                    {
                        subQuestionCount = GetChildQuestionCount("TL");
                    }

                    return new CloDto
                    {
                        Clo = clo,
                        Num = GetPartColumnTotal(part, clo),
                        SubQuestionCount = subQuestionCount
                    };
                })
                .Where(c => c.Num > 0)
                .ToList();

            part.QuestionTypes = _questionTypes.Keys
                .Select(loai => new QuestionTypeDto
                {
                    Loai = loai,
                    Num = GetPartRowTotal(part, loai)
                })
                .Where(q => q.Num > 0)
                .ToList();

            part.NumQuestions = GetPartTotal(part);
            _maTran.TotalQuestions = GetAllPartsTotal();
        }

        #endregion

        #region Event Handlers

        protected async Task OnMonHocChanged(Guid maMonHoc)
        {
            _model.MaMonHoc = maMonHoc;

            // Reset hoàn toàn
            _maTran = new MaTranDto
            {
                TotalQuestions = 0,
                CloPerPart = false,
                Clos = new List<CloDto>(),
                QuestionTypes = new List<QuestionTypeDto>(),
                Parts = new List<PartDto>()
            };

            _selectedPartIds = new HashSet<Guid>();
            _availableParts = new List<PhanDto>();
            _matrixData = new Dictionary<(int clo, string loai), int>();
            _partMatrixData = new Dictionary<Guid, Dictionary<(int clo, string loai), int>>();
            _partClos = new Dictionary<Guid, List<int>>();
            _matrixClos = new List<int> { 1, 2, 3 };
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

                Snackbar.Add($"Đã tải {_availableParts.Count} chương/phần.", Severity.Info);
            }
            else
            {
                Snackbar.Add("Môn này chưa có chương/phần → Dùng chế độ toàn đề.", Severity.Info);
            }

            InitializeDefaultMatrix();
            StateHasChanged();
        }

        protected void OnCloPerPartChanged(bool newValue)
        {
            if (newValue && (!_availableParts?.Any() ?? true))
            {
                Snackbar.Add("Môn này chưa có chương/phần nào → Tự động dùng chế độ toàn đề.", Severity.Info);
                _maTran.CloPerPart = false;
                return;
            }

            _maTran.CloPerPart = newValue;

            if (newValue)
            {
                // Chuyển sang theo phần
                _maTran.Clos?.Clear();
                _maTran.QuestionTypes?.Clear();
                _matrixData.Clear();

                if (_selectedPartIds.Any())
                {
                    UpdateMaTranParts();
                }
            }
            else
            {
                // Chuyển về toàn đề
                _maTran.Parts?.Clear();
                _selectedPartIds.Clear();
                _partMatrixData.Clear();
                _partClos.Clear();
                _matrixClos = new List<int> { 1, 2, 3 };
                InitializeDefaultMatrix();
            }

            _isValidated = false;
            _validationMessage = null;
            StateHasChanged();
        }

        protected async Task OnPartSelectionChanged(IEnumerable<Guid> selectedIds)
        {
            _selectedPartIds = selectedIds?.ToHashSet() ?? new HashSet<Guid>();
            UpdateMaTranParts();
        }

        private void UpdateMaTranParts()
        {
            var toRemove = _maTran.Parts?.Where(p => !_selectedPartIds.Contains(p.MaPhan)).ToList() ?? new();
            foreach (var p in toRemove)
            {
                _maTran.Parts?.Remove(p);
                _partMatrixData.Remove(p.MaPhan);
                _partClos.Remove(p.MaPhan);
            }

            foreach (var id in _selectedPartIds)
            {
                if (_maTran.Parts?.Any(p => p.MaPhan == id) != true)
                {
                    var newPart = new PartDto
                    {
                        MaPhan = id,
                        NumQuestions = 0,
                        Clos = new List<CloDto>(),
                        QuestionTypes = new List<QuestionTypeDto>()
                    };
                    _maTran.Parts?.Add(newPart);

                    // Khởi tạo ma trận cho phần mới
                    _partClos[id] = new List<int> { 1, 2, 3 };
                    _partMatrixData[id] = new Dictionary<(int clo, string loai), int>();
                    foreach (var clo in _partClos[id])
                    {
                        foreach (var loai in _questionTypes.Keys)
                        {
                            _partMatrixData[id][(clo, loai)] = 0;
                        }
                    }
                }
            }

            _isValidated = false;
            StateHasChanged();
        }

        protected void RemovePart(PartDto part)
        {
            _maTran.Parts?.Remove(part);
            _selectedPartIds.Remove(part.MaPhan);
            _partMatrixData.Remove(part.MaPhan);
            _partClos.Remove(part.MaPhan);
            _isValidated = false;
            StateHasChanged();
        }

        protected void ResetPage()
        {
            _selectedPartIds.Clear();
            _maTran.Parts?.Clear();
            _maTran.CloPerPart = false;
            _maTran.Clos?.Clear();
            _maTran.QuestionTypes?.Clear();
            _matrixData.Clear();
            _partMatrixData.Clear();
            _partClos.Clear();
            _matrixClos = new List<int> { 1, 2, 3 };
            _isValidated = false;
            _validationMessage = null;
            InitializeDefaultMatrix();
            StateHasChanged();
        }

        #endregion

        #region Helpers

        protected Color GetQuestionTypeColor(string loai)
        {
            return loai switch
            {
                "NH" => Color.Warning,
                "TL" => Color.Primary,
                "TN" => Color.Success,
                "MN" => Color.Info,
                "GN" => Color.Secondary,
                "DT" => Color.Tertiary,
                _ => Color.Default
            };
        }

        protected string GetQuestionTypeTooltip(string loai)
        {
            return loai switch
            {
                "NH" => "Câu hỏi nhóm: Nhập TỔNG SỐ CÂU HỎI CON + SỐ CÂU ĐƠN trong các nhóm. Hệ thống sẽ chọn các nhóm câu hỏi phù hợp.",
                "TL" => "Tự luận: Nhập TỔNG SỐ CÂU HỎI CON trong các câu hỏi cha TL. Hệ thống sẽ chọn các câu cha có đủ số câu con.",
                "TN" => "Trắc nghiệm 1 đáp án: Mỗi câu hỏi tính là 1. Nhập số lượng câu hỏi cần rút trích.",
                "MN" => "Trắc nghiệm nhiều đáp án: Mỗi câu hỏi tính là 1. Nhập số lượng câu hỏi cần rút trích.",
                "GN" => "Ghép nối: Mỗi câu hỏi tính là 1. Nhập số lượng câu hỏi cần rút trích.",
                "DT" => "Điền từ: Mỗi câu hỏi tính là 1. Nhập số lượng câu hỏi cần rút trích.",
                _ => ""
            };
        }

        #endregion

        #region Actions

        protected async Task XemTruocMaTran()
        {
            // Cập nhật ma trận trước khi xem
            if (!_maTran.CloPerPart)
            {
                UpdateMaTranFromMatrix();
            }

            var json = System.Text.Json.JsonSerializer.Serialize(_maTran, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

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

        protected async Task KiemTraMaTran()
        {
            if (_model.MaMonHoc == Guid.Empty)
            {
                Snackbar.Add("Vui lòng chọn môn học!", Severity.Warning);
                return;
            }

            // Cập nhật ma trận trước khi kiểm tra
            if (!_maTran.CloPerPart)
            {
                UpdateMaTranFromMatrix();
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

                string messageToShow;
                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(res.Message ?? "");
                    messageToShow = jsonDoc.RootElement.GetProperty("message").GetString() ?? "Ma trận không hợp lệ!";
                }
                catch
                {
                    messageToShow = res.Message ?? "Ma trận không hợp lệ!";
                }

                _validationMessage = messageToShow;

                if (res.Success)
                {
                    Snackbar.Add($"✓ {messageToShow}", Severity.Success);
                }
                else
                {
                    Snackbar.Add($"✗ {messageToShow}", Severity.Error);
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
                var total = GetGrandTotal();
                if (total <= 0)
                {
                    errorMessage = "Tổng số câu hỏi phải lớn hơn 0. Hãy nhập số lượng vào các ô trong ma trận.";
                    return false;
                }

                // Kiểm tra có ít nhất 1 CLO có giá trị
                bool hasClolValue = _matrixClos.Any(clo => GetColumnTotal(clo) > 0);
                if (!hasClolValue)
                {
                    errorMessage = "Vui lòng nhập số câu hỏi cho ít nhất 1 CLO";
                    return false;
                }

                // Kiểm tra có ít nhất 1 loại câu hỏi có giá trị
                bool hasTypeValue = _questionTypes.Keys.Any(loai => GetRowTotal(loai) > 0);
                if (!hasTypeValue)
                {
                    errorMessage = "Vui lòng nhập số câu hỏi cho ít nhất 1 loại câu hỏi";
                    return false;
                }
            }
            else
            {
                if (_maTran.Parts == null || !_maTran.Parts.Any())
                {
                    errorMessage = "Vui lòng chọn ít nhất một chương/phần";
                    return false;
                }

                var allPartsTotal = GetAllPartsTotal();
                if (allPartsTotal <= 0)
                {
                    errorMessage = "Tổng số câu hỏi của tất cả các chương phải lớn hơn 0";
                    return false;
                }

                foreach (var part in _maTran.Parts)
                {
                    var partTotal = GetPartTotal(part);
                    if (partTotal <= 0)
                    {
                        var phanInfo = _availableParts.FirstOrDefault(p => p.MaPhan == part.MaPhan);
                        var tenPhan = phanInfo?.TenPhan ?? "Chương";
                        errorMessage = $"Chương '{tenPhan}' phải có ít nhất 1 câu hỏi";
                        return false;
                    }
                }
            }

            return true;
        }

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

            // Đảm bảo UserId luôn có
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userIdClaim = user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _model.MaNguoiDung = userId;
            }
            else
            {
                Snackbar.Add("Không xác định được UserId.", Severity.Error);
                return;
            }

            _isProcessing = true;
            StateHasChanged();

            try
            {
                _model.MaTran = _maTran;
                var res = await YeuCauApi.CreateAsync(_model);

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

        #endregion
    }
}