using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Backend.Classifier;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.DTO;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public sealed class CategoryListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ApplicationCount { get; init; }

    public string DescriptionPreview =>
        string.IsNullOrWhiteSpace(Description)
            ? "Fără descriere"
            : Description;

    public string UsageSummary =>
        ApplicationCount == 1
            ? "1 aplicație asociată"
            : $"{ApplicationCount} aplicații asociate";
}

public sealed class CategoryRuleListItem
{
    public CategoryRule Rule { get; init; } = new();
    public int MatchCount { get; init; }
    public string MatchPreview { get; init; } = string.Empty;

    public string Title =>
        string.IsNullOrWhiteSpace(Rule.Name)
            ? Rule.Pattern
            : Rule.Name;

    public string Summary =>
        $"{FormatTarget(Rule.Target)} \u00b7 {FormatField(Rule.Field)} \u00b7 {FormatMatchType(Rule.MatchType)} \u00b7 prioritate {Rule.Priority}";

    public string StatusLabel => Rule.Enabled ? "Activa" : "Pauza";

    private static string FormatTarget(CategoryRuleTarget target) =>
        target == CategoryRuleTarget.Application ? "Aplica\u021bie" : "Website";

    private static string FormatField(CategoryRuleField field) =>
        field switch
        {
            CategoryRuleField.Any => "Orice camp",
            CategoryRuleField.ProcessName => "Proces",
            CategoryRuleField.WindowTitle => "Titlu fereastra",
            CategoryRuleField.ClassName => "Clasa fereastra",
            CategoryRuleField.Url => "URL",
            CategoryRuleField.Host => "Domeniu",
            CategoryRuleField.Path => "Path",
            _ => field.ToString()
        };

    private static string FormatMatchType(CategoryRuleMatchType matchType) =>
        matchType switch
        {
            CategoryRuleMatchType.Contains => "Contine",
            CategoryRuleMatchType.Exact => "Exact",
            CategoryRuleMatchType.StartsWith => "Incepe cu",
            CategoryRuleMatchType.EndsWith => "Se termina cu",
            CategoryRuleMatchType.Regex => "Regex",
            _ => matchType.ToString()
        };
}

public sealed class RuleTargetOption
{
    public CategoryRuleTarget Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public sealed class RuleFieldOption
{
    public CategoryRuleField Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public sealed class RuleMatchTypeOption
{
    public CategoryRuleMatchType Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public enum CategoriesModalKind
{
    None,
    CategoryEditor,
    RuleEditor,
    DeleteConfirm
}

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly ClassifierRuleStore _ruleStore = new();

    private List<ApplicationCategoryRow> _allApplications = [];
    private List<BrowserRecord> _allBrowserActivities = [];
    private List<CategoryRule> _allRules = [];
    private bool _isUpdatingRuleDraft;
    private bool _isEditingCategory;
    private bool _isDeletingCategory;

    private string _pageSubtitle = "Administrarea categoriilor \u0219i clasificarea aplica\u021biilor monitorizate.";
    private string _statusMessage = "Se \u00eencarc\u0103 categoriile...";
    private string _lastRefreshLabel = "Actualizare in curs";
    private string _categoryCount = "0";
    private string _assignedApplications = "0";
    private string _uncategorizedApplications = "0";
    private CategoriesModalKind _activeModal = CategoriesModalKind.None;
    private CategoryListItem? _selectedCategory;
    private CategoryRuleListItem? _selectedCategoryRule;

    private string _categoryEditorTitle = "Adaug\u0103 categorie";
    private string _categoryEditorDescription = "Define\u0219te o categorie clar\u0103 pe care o po\u021bi reutiliza \u00een clasificare \u0219i raportare.";
    private string _categoryDraftName = string.Empty;
    private string _categoryDraftDescription = string.Empty;

    private string _deleteConfirmTitle = "\u0218terge";
    private string _deleteConfirmMessage = string.Empty;

    private string _ruleEditorTitle = "Regula noua";
    private string _ruleEditorDescription = "Configureaz\u0103 o regul\u0103 nou\u0103 pentru categoria selectat\u0103.";
    private string _ruleName = string.Empty;
    private string _rulePattern = string.Empty;
    private string _rulePriorityText = "100";
    private string _ruleNotes = string.Empty;
    private string _rulePreviewSummary = "Previzualizarea regulii va ap\u0103rea dup\u0103 completarea c\u00e2mpurilor.";
    private bool _ruleEnabled = true;
    private bool _ruleIgnoreCase = true;
    private RuleTargetOption? _selectedRuleTarget;
    private RuleFieldOption? _selectedRuleField;
    private RuleMatchTypeOption? _selectedRuleMatchType;

    public CategoriesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        OpenAddCategoryModalCommand = new RelayCommand(_ => OpenCategoryEditor(isEditing: false));
        OpenEditCategoryModalCommand = new RelayCommand(_ => OpenCategoryEditor(isEditing: true));
        SaveCategoryCommand = new RelayCommand(_ => SaveCategory());
        OpenDeleteCategoryModalCommand = new RelayCommand(_ => OpenDeleteConfirmModal(isDeletingCategory: true));
        DeleteSelectedCategoryCommand = new RelayCommand(_ => DeleteSelectedCategory());
        ConfirmDeleteCommand = new RelayCommand(_ =>
        {
            if (_isDeletingCategory) DeleteSelectedCategory();
            else DeleteSelectedRule();
        });
        NewRuleCommand = new RelayCommand(_ => BeginNewRule());
        OpenEditRuleModalCommand = new RelayCommand(_ => OpenSelectedRuleForEditing());
        SaveRuleCommand = new RelayCommand(_ => SaveRule());
        OpenDeleteRuleModalCommand = new RelayCommand(_ => OpenDeleteConfirmModal(isDeletingCategory: false));
        DeleteSelectedRuleCommand = new RelayCommand(_ => DeleteSelectedRule());
        CloseModalCommand = new RelayCommand(_ => CloseModal());

        RuleTargetOptions.Add(new RuleTargetOption { Value = CategoryRuleTarget.Application, Label = "Aplica\u021bie desktop" });
        RuleTargetOptions.Add(new RuleTargetOption { Value = CategoryRuleTarget.Website, Label = "Website / tab browser" });

        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Contains, Label = "Contine textul" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Exact, Label = "Potrivire exact\u0103" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.StartsWith, Label = "Incepe cu" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.EndsWith, Label = "Se termina cu" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Regex, Label = "Regex" });

        SetRuleDraftDefaults();
        Load();
    }

    public ObservableCollection<CategoryListItem> Categories { get; } = [];
    public ObservableCollection<CategoryRuleListItem> CategoryRules { get; } = [];
    public ObservableCollection<RuleTargetOption> RuleTargetOptions { get; } = [];
    public ObservableCollection<RuleFieldOption> RuleFieldOptions { get; } = [];
    public ObservableCollection<RuleMatchTypeOption> RuleMatchTypeOptions { get; } = [];

    public ICommand RefreshCommand { get; }
    public ICommand OpenAddCategoryModalCommand { get; }
    public ICommand OpenEditCategoryModalCommand { get; }
    public ICommand SaveCategoryCommand { get; }
    public ICommand OpenDeleteCategoryModalCommand { get; }
    public ICommand DeleteSelectedCategoryCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand NewRuleCommand { get; }
    public ICommand OpenEditRuleModalCommand { get; }
    public ICommand SaveRuleCommand { get; }
    public ICommand OpenDeleteRuleModalCommand { get; }
    public ICommand DeleteSelectedRuleCommand { get; }
    public ICommand CloseModalCommand { get; }

    public string PageSubtitle
    {
        get => _pageSubtitle;
        set => SetProperty(ref _pageSubtitle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string LastRefreshLabel
    {
        get => _lastRefreshLabel;
        set => SetProperty(ref _lastRefreshLabel, value);
    }

    public string CategoryCount
    {
        get => _categoryCount;
        set => SetProperty(ref _categoryCount, value);
    }

    public string AssignedApplications
    {
        get => _assignedApplications;
        set => SetProperty(ref _assignedApplications, value);
    }

    public string UncategorizedApplications
    {
        get => _uncategorizedApplications;
        set => SetProperty(ref _uncategorizedApplications, value);
    }

    public CategoryListItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value))
                return;

            RefreshRulesForSelectedCategory();
            OnPropertyChanged(nameof(HasSelectedCategory));
        }
    }

    public CategoryRuleListItem? SelectedCategoryRule
    {
        get => _selectedCategoryRule;
        set
        {
            if (!SetProperty(ref _selectedCategoryRule, value))
                return;

            if (value == null)
                SetRuleDraftDefaults();
            else
                LoadRuleDraft(value.Rule);

            OnPropertyChanged(nameof(HasSelectedCategoryRule));
        }
    }

    public CategoriesModalKind ActiveModal
    {
        get => _activeModal;
        private set
        {
            if (!SetProperty(ref _activeModal, value))
                return;
            OnPropertyChanged(nameof(IsModalVisible));
        }
    }

    public bool IsModalVisible => ActiveModal != CategoriesModalKind.None;

    public string CategoryEditorTitle
    {
        get => _categoryEditorTitle;
        set => SetProperty(ref _categoryEditorTitle, value);
    }

    public string CategoryEditorDescription
    {
        get => _categoryEditorDescription;
        set => SetProperty(ref _categoryEditorDescription, value);
    }

    public string CategoryDraftName
    {
        get => _categoryDraftName;
        set => SetProperty(ref _categoryDraftName, value);
    }

    public string CategoryDraftDescription
    {
        get => _categoryDraftDescription;
        set => SetProperty(ref _categoryDraftDescription, value);
    }

    public string DeleteConfirmTitle
    {
        get => _deleteConfirmTitle;
        set => SetProperty(ref _deleteConfirmTitle, value);
    }

    public string DeleteConfirmMessage
    {
        get => _deleteConfirmMessage;
        set => SetProperty(ref _deleteConfirmMessage, value);
    }

    public string RuleEditorTitle
    {
        get => _ruleEditorTitle;
        set => SetProperty(ref _ruleEditorTitle, value);
    }

    public string RuleEditorDescription
    {
        get => _ruleEditorDescription;
        set => SetProperty(ref _ruleEditorDescription, value);
    }

    public string RuleName
    {
        get => _ruleName;
        set => SetRuleDraftProperty(ref _ruleName, value, updatePreview: false);
    }

    public string RulePattern
    {
        get => _rulePattern;
        set => SetRuleDraftProperty(ref _rulePattern, value);
    }

    public string RulePriorityText
    {
        get => _rulePriorityText;
        set => SetRuleDraftProperty(ref _rulePriorityText, value);
    }

    public string RuleNotes
    {
        get => _ruleNotes;
        set => SetRuleDraftProperty(ref _ruleNotes, value, updatePreview: false);
    }

    public string RulePreviewSummary
    {
        get => _rulePreviewSummary;
        set => SetProperty(ref _rulePreviewSummary, value);
    }

    public bool RuleEnabled
    {
        get => _ruleEnabled;
        set => SetRuleDraftProperty(ref _ruleEnabled, value);
    }

    public bool RuleIgnoreCase
    {
        get => _ruleIgnoreCase;
        set => SetRuleDraftProperty(ref _ruleIgnoreCase, value);
    }

    public RuleTargetOption? SelectedRuleTarget
    {
        get => _selectedRuleTarget;
        set
        {
            if (!SetProperty(ref _selectedRuleTarget, value))
                return;
            RefreshRuleFieldOptions();
            UpdateRulePreview();
        }
    }

    public RuleFieldOption? SelectedRuleField
    {
        get => _selectedRuleField;
        set
        {
            if (!SetProperty(ref _selectedRuleField, value))
                return;
            UpdateRulePreview();
        }
    }

    public RuleMatchTypeOption? SelectedRuleMatchType
    {
        get => _selectedRuleMatchType;
        set
        {
            if (!SetProperty(ref _selectedRuleMatchType, value))
                return;
            UpdateRulePreview();
        }
    }

    public bool HasSelectedCategory => SelectedCategory != null;
    public bool HasSelectedCategoryRule => SelectedCategoryRule != null;

    private void Load(int? selectedCategoryId = null, string? selectedRuleId = null)
    {
        var currentCategoryId = selectedCategoryId ?? SelectedCategory?.Id;
        var currentRuleId = selectedRuleId ?? SelectedCategoryRule?.Rule.Id;

        var categories = _db.GetAllCategories()
            .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var applications = _db.GetAllApplications()
            .Where(app => app.Id.HasValue)
            .ToList();

        var categoryLookup = categories.ToDictionary(category => category.CategoryId);
        var applicationCounts = applications
            .Where(app => app.CategoryId.HasValue)
            .GroupBy(app => app.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(new CategoryListItem
            {
                Id = category.CategoryId,
                Name = category.Name,
                Description = category.Description ?? string.Empty,
                ApplicationCount = applicationCounts.GetValueOrDefault(category.CategoryId)
            });
        }

        _allApplications = applications
            .Select(app => new ApplicationCategoryRow
            {
                AppId = app.Id ?? 0,
                CategoryId = app.CategoryId,
                ProcessName = app.ProcessName ?? string.Empty,
                WindowTitle = app.WindowTitle ?? string.Empty,
                ClassName = app.ClassName ?? string.Empty,
                CategoryName = app.CategoryId.HasValue && categoryLookup.TryGetValue(app.CategoryId.Value, out var category)
                    ? category.Name
                    : "Neasignată"
            })
            .OrderBy(app => app.CategoryId.HasValue ? 0 : 1)
            .ThenBy(app => app.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(app => app.PrimaryLabel, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _allBrowserActivities = _db.GetAllBrowserActivity()
            .Select(BrowserRecord.FromDto)
            .ToList();

        _allRules = _ruleStore.LoadRules().ToList();

        CategoryCount = Categories.Count.ToString();
        AssignedApplications = _allApplications.Count(app => app.CategoryId.HasValue).ToString();
        UncategorizedApplications = _allApplications.Count(app => !app.CategoryId.HasValue).ToString();
        LastRefreshLabel = $"Actualizat la {DateTime.Now:HH:mm}";
        PageSubtitle = _allApplications.Count == 0
            ? "Nu exist\u0103 aplica\u021bii monitorizate \u00een baza de date. Categoriile \u0219i regulile pot fi preg\u0103tite anticipat."
            : "Revizuie\u0219te categoriile existente \u0219i transform\u0103 deciziile repetate \u00een reguli reutilizabile.";

        SelectedCategory = currentCategoryId.HasValue
            ? Categories.FirstOrDefault(category => category.Id == currentCategoryId.Value)
            : Categories.FirstOrDefault();

        RefreshRulesForSelectedCategory(currentRuleId);

        StatusMessage = Categories.Count == 0
            ? "Nu exist\u0103 categorii definite. Adaug\u0103 prima categorie pentru a \u00eencepe clasificarea."
            : $"Sunt disponibile {Categories.Count} categorii, {_allApplications.Count} aplica\u021bii \u0219i {_allRules.Count} reguli personalizate.";
    }

    private void OpenCategoryEditor(bool isEditing)
    {
        if (isEditing && SelectedCategory == null)
        {
            StatusMessage = "Selecteaz\u0103 o categorie \u00eenainte de modificare.";
            return;
        }

        _isEditingCategory = isEditing;
        CategoryEditorTitle = isEditing ? "Modific\u0103 categoria" : "Adaug\u0103 categorie";
        CategoryEditorDescription = isEditing
            ? "Actualizeaz\u0103 numele sau descrierea categoriei selectate."
            : "Define\u0219te o categorie clar\u0103 pe care o po\u021bi reutiliza \u00een clasificare \u0219i raportare.";
        CategoryDraftName = isEditing ? SelectedCategory?.Name ?? string.Empty : string.Empty;
        CategoryDraftDescription = isEditing ? SelectedCategory?.Description ?? string.Empty : string.Empty;
        ActiveModal = CategoriesModalKind.CategoryEditor;
    }

    private void SaveCategory()
    {
        var name = (CategoryDraftName ?? string.Empty).Trim();
        var description = (CategoryDraftDescription ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Numele categoriei este obligatoriu.";
            return;
        }

        var duplicateExists = Categories.Any(category =>
            string.Equals(category.Name, name, StringComparison.CurrentCultureIgnoreCase) &&
            (!_isEditingCategory || category.Id != SelectedCategory?.Id));

        if (duplicateExists)
        {
            StatusMessage = $"Categoria \"{name}\" exist\u0103 deja.";
            return;
        }

        if (_isEditingCategory)
        {
            if (SelectedCategory == null)
            {
                StatusMessage = "Selecteaz\u0103 o categorie \u00eenainte de modificare.";
                return;
            }

            var result = _db.UpdateCategory(new CategoryDto
            {
                CategoryId = SelectedCategory.Id,
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? null : description
            });

            if (result == 0)
            {
                StatusMessage = $"Categoria \"{SelectedCategory.Name}\" nu a putut fi actualizat\u0103.";
                return;
            }

            Load(SelectedCategory.Id, SelectedCategoryRule?.Rule.Id);
            CloseModal();
            StatusMessage = $"Categoria \"{name}\" a fost actualizat\u0103.";
            return;
        }

        var categoryId = _db.InsertCategory(new CategoryDto
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        });

        Load(categoryId, SelectedCategoryRule?.Rule.Id);
        CloseModal();
        StatusMessage = $"Categoria \"{name}\" a fost ad\u0103ugat\u0103.";
    }

    private void OpenDeleteConfirmModal(bool isDeletingCategory)
    {
        _isDeletingCategory = isDeletingCategory;

        if (isDeletingCategory)
        {
            if (SelectedCategory == null)
            {
                StatusMessage = "Selecteaz\u0103 o categorie \u00eenainte de \u0219tergere.";
                return;
            }

            var affectedApps = _allApplications.Count(app => app.CategoryId == SelectedCategory.Id);
            var affectedRules = _allRules.Count(rule => rule.CategoryId == SelectedCategory.Id);
            DeleteConfirmTitle = "\u0218terge categoria";
            DeleteConfirmMessage = BuildDeleteMessage(SelectedCategory.Name, affectedApps, affectedRules);
        }
        else
        {
            if (SelectedCategoryRule == null)
            {
                StatusMessage = "Selecteaz\u0103 o regul\u0103 \u00eenainte de \u0219tergere.";
                return;
            }

            DeleteConfirmTitle = "\u0218terge regula";
            DeleteConfirmMessage = $"Regula \"{SelectedCategoryRule.Title}\" va fi eliminat\u0103. Acoperirea estimat\u0103 curent\u0103 este: {SelectedCategoryRule.MatchPreview}";
        }

        ActiveModal = CategoriesModalKind.DeleteConfirm;
    }

    private void DeleteSelectedCategory()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selecteaz\u0103 o categorie \u00eenainte de \u0219tergere.";
            return;
        }

        var deletedCategory = SelectedCategory;
        var affectedApplications = _allApplications.Count(app => app.CategoryId == deletedCategory.Id);

        var removedRules = _allRules
            .Where(rule => rule.CategoryId == deletedCategory.Id)
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.Ordinal);

        var result = _db.DeleteCategory(deletedCategory.Id);

        if (result == 0)
        {
            StatusMessage = $"Categoria \"{deletedCategory.Name}\" nu a putut fi \u0219tears\u0103.";
            return;
        }

        if (removedRules.Count > 0)
        {
            _allRules = _allRules
                .Where(rule => !removedRules.Contains(rule.Id))
                .ToList();
            _ruleStore.SaveRules(_allRules);
        }

        Load();
        CloseModal();
        StatusMessage = affectedApplications == 0
            ? $"Categoria \"{deletedCategory.Name}\" a fost \u0219tears\u0103."
            : $"Categoria \"{deletedCategory.Name}\" a fost \u0219tears\u0103, iar {affectedApplications} aplica\u021bii au r\u0103mas neasignate.";
    }

    private void BeginNewRule()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selecteaz\u0103 mai \u00eent\u00e2i categoria pentru care creezi regula.";
            return;
        }

        SelectedCategoryRule = null;
        RuleEditorTitle = "Regul\u0103 nou\u0103";
        RuleEditorDescription = $"Configureaz\u0103 o regul\u0103 nou\u0103 pentru categoria \"{SelectedCategory.Name}\".";
        ActiveModal = CategoriesModalKind.RuleEditor;
    }

    private void OpenSelectedRuleForEditing()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selecteaz\u0103 mai \u00eent\u00e2i categoria pentru care vrei s\u0103 modifici regula.";
            return;
        }

        if (SelectedCategoryRule == null)
        {
            StatusMessage = "Selecteaz\u0103 o regul\u0103 \u00eenainte de modificare.";
            return;
        }

        RuleEditorTitle = "Modific\u0103 regula";
        RuleEditorDescription = $"Revizuie\u0219te regula \"{SelectedCategoryRule.Title}\" pentru categoria \"{SelectedCategory.Name}\".";
        ActiveModal = CategoriesModalKind.RuleEditor;
    }

    private void SaveRule()
    {
        if (!TryBuildRuleFromDraft(out var rule, out var error))
        {
            StatusMessage = error;
            RulePreviewSummary = error;
            return;
        }

        if (!CategoryRuleMatcher.TryValidate(rule, out error))
        {
            StatusMessage = error;
            RulePreviewSummary = error;
            return;
        }

        var existingIndex = _allRules.FindIndex(existingRule => string.Equals(existingRule.Id, rule.Id, StringComparison.Ordinal));
        if (existingIndex >= 0)
            _allRules[existingIndex] = rule;
        else
            _allRules.Add(rule);

        _ruleStore.SaveRules(_allRules);
        _allRules = _ruleStore.LoadRules().ToList();
        RefreshRulesForSelectedCategory(rule.Id);
        CloseModal();
        StatusMessage = existingIndex >= 0
            ? "Regula a fost actualizat\u0103 \u0219i va avea prioritate fa\u021b\u0103 de clasificarea implicit\u0103."
            : "Regula a fost salvat\u0103 \u0219i va fi folosit\u0103 la urm\u0103toarea clasificare.";
    }

    private void DeleteSelectedRule()
    {
        if (SelectedCategoryRule == null)
        {
            StatusMessage = "Selecteaz\u0103 o regul\u0103 \u00eenainte de \u0219tergere.";
            return;
        }

        var removedRule = SelectedCategoryRule.Rule;
        var removedTitle = SelectedCategoryRule.Title;
        _allRules = _allRules
            .Where(rule => !string.Equals(rule.Id, removedRule.Id, StringComparison.Ordinal))
            .ToList();

        _ruleStore.SaveRules(_allRules);
        _allRules = _ruleStore.LoadRules().ToList();
        RefreshRulesForSelectedCategory();
        CloseModal();
        StatusMessage = $"Regula \"{removedTitle}\" a fost \u0219tears\u0103.";
    }

    private void CloseModal()
    {
        ActiveModal = CategoriesModalKind.None;
    }

    private void RefreshRulesForSelectedCategory(string? selectedRuleId = null)
    {
        CategoryRules.Clear();

        if (SelectedCategory == null)
        {
            SelectedCategoryRule = null;
            return;
        }

        var rules = _allRules
            .Where(rule => rule.CategoryId == SelectedCategory.Id)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(rule => rule.Pattern, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var rule in rules)
        {
            var (matchCount, preview) = BuildRuleCoverage(rule);
            CategoryRules.Add(new CategoryRuleListItem
            {
                Rule = rule,
                MatchCount = matchCount,
                MatchPreview = preview
            });
        }

        SelectedCategoryRule = !string.IsNullOrWhiteSpace(selectedRuleId)
            ? CategoryRules.FirstOrDefault(rule => string.Equals(rule.Rule.Id, selectedRuleId, StringComparison.Ordinal))
            : null;

        if (SelectedCategoryRule == null)
            SetRuleDraftDefaults();
    }

    private void LoadRuleDraft(CategoryRule rule)
    {
        _isUpdatingRuleDraft = true;
        SelectedRuleTarget = RuleTargetOptions.FirstOrDefault(option => option.Value == rule.Target);
        SelectedRuleMatchType = RuleMatchTypeOptions.FirstOrDefault(option => option.Value == rule.MatchType);
        SelectedRuleField = RuleFieldOptions.FirstOrDefault(option => option.Value == rule.Field);
        RuleName = rule.Name;
        RulePattern = rule.Pattern;
        RulePriorityText = rule.Priority.ToString();
        RuleNotes = rule.Notes ?? string.Empty;
        RuleEnabled = rule.Enabled;
        RuleIgnoreCase = rule.IgnoreCase;
        _isUpdatingRuleDraft = false;

        UpdateRulePreview();
    }

    private void SetRuleDraftDefaults()
    {
        _isUpdatingRuleDraft = true;
        SelectedRuleTarget = RuleTargetOptions.FirstOrDefault(option => option.Value == CategoryRuleTarget.Application) ?? RuleTargetOptions.FirstOrDefault();
        SelectedRuleMatchType = RuleMatchTypeOptions.FirstOrDefault(option => option.Value == CategoryRuleMatchType.Contains) ?? RuleMatchTypeOptions.FirstOrDefault();
        RefreshRuleFieldOptions();
        SelectedRuleField = RuleFieldOptions.FirstOrDefault(option => option.Value == CategoryRuleField.ProcessName) ?? RuleFieldOptions.FirstOrDefault();
        RuleName = string.Empty;
        RulePattern = string.Empty;
        RulePriorityText = "100";
        RuleNotes = string.Empty;
        RuleEnabled = true;
        RuleIgnoreCase = true;
        _isUpdatingRuleDraft = false;

        UpdateRulePreview();
    }

    private void RefreshRuleFieldOptions()
    {
        var target = SelectedRuleTarget?.Value ?? CategoryRuleTarget.Application;
        var previousField = SelectedRuleField?.Value;

        RuleFieldOptions.Clear();
        foreach (var option in GetFieldOptionsForTarget(target))
            RuleFieldOptions.Add(option);

        SelectedRuleField = RuleFieldOptions.FirstOrDefault(option => option.Value == previousField) ?? RuleFieldOptions.FirstOrDefault();
    }

    private void UpdateRulePreview()
    {
        if (_isUpdatingRuleDraft)
            return;

        if (!TryBuildRuleFromDraft(out var rule, out var error))
        {
            RulePreviewSummary = error;
            return;
        }

        if (!CategoryRuleMatcher.TryValidate(rule, out error))
        {
            RulePreviewSummary = error;
            return;
        }

        var (matchCount, preview) = BuildRuleCoverage(rule);
        RulePreviewSummary = matchCount == 0
            ? preview
            : $"{preview} Prioritatea mic\u0103 \u00eenseamn\u0103 c\u0103 regula este testat\u0103 mai devreme.";
    }

    private (int MatchCount, string Preview) BuildRuleCoverage(CategoryRule rule)
    {
        if (rule.Target == CategoryRuleTarget.Application)
        {
            var matches = _allApplications
                .Where(app => CategoryRuleMatcher.IsMatch(rule, ToApplicationRecord(app)))
                .ToList();

            if (matches.Count == 0)
                return (0, "Previzualizare: nicio aplica\u021bie existent\u0103 nu se potrive\u0219te \u00een acest moment.");

            var labelsPrimary = string.Join(", ", matches.Take(3).Select(app => app.PrimaryLabel));
            var suffix = matches.Count > 3 ? ", ..." : string.Empty;
            return (matches.Count, $"Previzualizare: {matches.Count} aplica\u021bii s-ar potrivi ({labelsPrimary}{suffix}).");
        }

        var browserMatches = _allBrowserActivities
            .Where(browser => CategoryRuleMatcher.IsMatch(rule, browser))
            .ToList();

        if (browserMatches.Count == 0)
            return (0, "Previzualizare: nicio activitate web existent\u0103 nu se potrive\u0219te \u00een acest moment.");

        var labels = string.Join(", ", browserMatches
            .Select(browser => string.IsNullOrWhiteSpace(browser.Domain) ? browser.Url : browser.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3));
        var domainCount = browserMatches
            .Select(browser => string.IsNullOrWhiteSpace(browser.Domain) ? browser.Url : browser.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var browserSuffix = domainCount > 3 ? ", ..." : string.Empty;
        return (browserMatches.Count, $"Previzualizare: {browserMatches.Count} intr\u0103ri web din {domainCount} domenii s-ar potrivi ({labels}{browserSuffix}).");

    }

    private bool TryBuildRuleFromDraft(out CategoryRule rule, out string error)
    {
        rule = new CategoryRule();

        if (SelectedCategory == null)
        {
            error = "Selecteaz\u0103 mai \u00eent\u00e2i categoria pentru regul\u0103.";
            return false;
        }

        if (SelectedRuleTarget == null || SelectedRuleField == null || SelectedRuleMatchType == null)
        {
            error = "Completeaz\u0103 tipul, c\u00e2mpul \u0219i modul de potrivire pentru regul\u0103.";
            return false;
        }

        var pattern = (RulePattern ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "Introdu textul sau regex-ul care va declan\u0219a clasificarea.";
            return false;
        }

        var priorityText = (RulePriorityText ?? string.Empty).Trim();
        if (!int.TryParse(string.IsNullOrWhiteSpace(priorityText) ? "100" : priorityText, out var priority))
        {
            error = "Prioritatea trebuie s\u0103 fie un num\u0103r \u00eentreg.";
            return false;
        }

        rule = new CategoryRule
        {
            Id = SelectedCategoryRule?.Rule.Id ?? Guid.NewGuid().ToString("N"),
            CategoryId = SelectedCategory.Id,
            Name = (RuleName ?? string.Empty).Trim(),
            Target = SelectedRuleTarget.Value,
            Field = SelectedRuleField.Value,
            MatchType = SelectedRuleMatchType.Value,
            Pattern = pattern,
            Priority = priority,
            Enabled = RuleEnabled,
            IgnoreCase = RuleIgnoreCase,
            Notes = string.IsNullOrWhiteSpace(RuleNotes) ? null : RuleNotes.Trim()
        };

        error = string.Empty;
        return true;
    }

    private void SetRuleDraftProperty<T>(ref T field, T value, bool updatePreview = true)
    {
        if (!SetProperty(ref field, value))
            return;

        if (updatePreview)
            UpdateRulePreview();
    }

    private static string BuildDeleteMessage(string categoryName, int affectedApplications, int affectedRules)
    {
        var appMessage = affectedApplications == 0
            ? "Nicio aplica\u021bie nu depinde de aceast\u0103 categorie."
            : affectedApplications == 1
                ? "1 aplica\u021bie va r\u0103m\u00e2ne neasignat\u0103."
                : $"{affectedApplications} aplica\u021bii vor r\u0103m\u00e2ne neasignate.";

        var ruleMessage = affectedRules == 0
            ? "Nu exist\u0103 reguli personalizate asociate."
            : affectedRules == 1
                ? "1 regul\u0103 personalizat\u0103 va fi eliminat\u0103."
                : $"{affectedRules} reguli personalizate vor fi eliminate.";

        return $"Categoria \"{categoryName}\" va fi \u0219tears\u0103. {appMessage} {ruleMessage}";
    }

    private static IEnumerable<RuleFieldOption> GetFieldOptionsForTarget(CategoryRuleTarget target)
    {
        return target == CategoryRuleTarget.Application
            ?
            [
                new RuleFieldOption { Value = CategoryRuleField.ProcessName, Label = "Proces" },
                new RuleFieldOption { Value = CategoryRuleField.ClassName, Label = "Clasa fereastra" },
                new RuleFieldOption { Value = CategoryRuleField.WindowTitle, Label = "Titlu fereastra" },
                new RuleFieldOption { Value = CategoryRuleField.Any, Label = "Orice c\u00e2mp aplica\u021bie" }
            ]
            :
            [
                new RuleFieldOption { Value = CategoryRuleField.Host, Label = "Domeniu" },
                new RuleFieldOption { Value = CategoryRuleField.Path, Label = "Path URL" },
                new RuleFieldOption { Value = CategoryRuleField.Url, Label = "URL complet" },
                new RuleFieldOption { Value = CategoryRuleField.Any, Label = "Orice camp website" }
            ];
    }

    private static ApplicationRecord ToApplicationRecord(ApplicationCategoryRow app)
    {
        return new ApplicationRecord
        {
            Id = app.AppId,
            CategoryId = app.CategoryId,
            ProcessName = app.ProcessName,
            WindowName = app.WindowTitle,
            ClassName = app.ClassName
        };
    }

    private sealed class ApplicationCategoryRow
    {
        public int AppId { get; init; }
        public int? CategoryId { get; init; }
        public string ProcessName { get; init; } = string.Empty;
        public string WindowTitle { get; init; } = string.Empty;
        public string ClassName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = "Neasignată";

        public string PrimaryLabel =>
            !string.IsNullOrWhiteSpace(ProcessName)
                ? ProcessName
                : !string.IsNullOrWhiteSpace(WindowTitle)
                    ? WindowTitle
                    : $"Aplicatia #{AppId}";
    }
}
