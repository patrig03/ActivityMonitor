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

public sealed class ApplicationCategoryRow
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

    public string SecondaryLabel =>
        string.IsNullOrWhiteSpace(WindowTitle) || string.Equals(WindowTitle, PrimaryLabel, StringComparison.Ordinal)
            ? ClassSummary
            : WindowTitle;

    public string IdentitySummary =>
        string.IsNullOrWhiteSpace(ClassName)
            ? $"ID aplicație: {AppId}"
            : $"Clasa: {ClassName} | ID aplicație: {AppId}";

    public string ClassSummary =>
        string.IsNullOrWhiteSpace(ClassName)
            ? "Clasa fereastră indisponibilă"
            : ClassName;
}

public sealed class CategoryAssignmentOption
{
    public int? CategoryId { get; init; }
    public string Label { get; init; } = string.Empty;

    public override string ToString() => Label;
}

public enum ApplicationScope
{
    All,
    SelectedCategory,
    Uncategorized
}

public sealed class ApplicationScopeOption
{
    public ApplicationScope Scope { get; init; }
    public string Label { get; init; } = string.Empty;

    public override string ToString() => Label;
}

public enum CategoriesModalKind
{
    None,
    CategoryEditor,
    DeleteCategory,
    ApplicationAssignment,
    RuleEditor,
    DeleteRule
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
        $"{FormatTarget(Rule.Target)} · {FormatField(Rule.Field)} · {FormatMatchType(Rule.MatchType)} · prioritate {Rule.Priority}";

    public string StatusLabel => Rule.Enabled ? "Activa" : "Pauza";

    private static string FormatTarget(CategoryRuleTarget target) =>
        target == CategoryRuleTarget.Application ? "Aplicație" : "Website";

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

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly ClassifierRuleStore _ruleStore = new();

    private List<ApplicationCategoryRow> _allApplications = [];
    private List<BrowserRecord> _allBrowserActivities = [];
    private List<CategoryRule> _allRules = [];
    private bool _isUpdatingRuleDraft;
    private bool _isEditingCategory;

    private string _pageSubtitle = "Administrarea categoriilor și clasificarea aplicațiilor monitorizate.";
    private string _statusMessage = "Se încarcă categoriile...";
    private string _assignmentStatus = "Selectează o aplicație pentru a-i modifica categoria.";
    private string _ruleStatusMessage = "Regulile personalizate se aplică înaintea clasificării implicite.";
    private string _lastRefreshLabel = "Actualizare in curs";
    private string _categoryCount = "0";
    private string _assignedApplications = "0";
    private string _uncategorizedApplications = "0";
    private string _applicationSearchText = string.Empty;
    private string _selectedCategoryTitle = "Nicio categorie selectată";
    private string _selectedCategoryDescription = "Alege o categorie din listă pentru a vedea impactul ei asupra aplicațiilor monitorizate.";
    private string _selectedCategoryUsage = "Aplicațiile asociate vor apărea în lista de mai jos.";
    private string _selectedCategoryRuleSummary = "Selectează o categorie pentru a administra regulile ei personalizate.";
    private string _selectedCategoryAutomationSummary = "Regulile existente vor afisa aici acoperirea lor estimata.";
    private string _selectedApplicationTitle = "Nicio aplicație selectată";
    private string _selectedApplicationDetail = "Selectează o aplicație monitorizată pentru a-i atribui o categorie.";
    private string _selectedApplicationIdentity = "Detaliile tehnice vor apărea aici.";
    private string _selectedApplicationCategoryLabel = "Categoria curentă va fi afișată după selecție.";
    private CategoriesModalKind _activeModal = CategoriesModalKind.None;
    private string _categoryEditorTitle = "Adaugă categorie";
    private string _categoryEditorDescription = "Definește o categorie clară pe care o poți reutiliza în clasificare și raportare.";
    private string _categoryDraftName = string.Empty;
    private string _categoryDraftDescription = string.Empty;
    private string _deleteCategoryMessage = "Selectează o categorie pentru ștergere.";
    private string _applicationAssignmentTitle = "Nicio aplicație selectată";
    private string _applicationAssignmentDescription = "Alege categoria potrivită pentru aplicația selectată.";
    private string _ruleEditorTitle = "Regula noua";
    private string _ruleEditorDescription = "Configurează o regulă nouă pentru categoria selectată.";
    private string _deleteRuleMessage = "Selectează o regulă pentru ștergere.";
    private CategoryListItem? _selectedCategory;
    private ApplicationCategoryRow? _selectedApplication;
    private CategoryAssignmentOption? _selectedApplicationCategory;
    private ApplicationScopeOption? _selectedApplicationScope;
    private CategoryRuleListItem? _selectedCategoryRule;
    private RuleTargetOption? _selectedRuleTarget;
    private RuleFieldOption? _selectedRuleField;
    private RuleMatchTypeOption? _selectedRuleMatchType;
    private string _ruleName = string.Empty;
    private string _rulePattern = string.Empty;
    private string _rulePriorityText = "100";
    private string _ruleNotes = string.Empty;
    private string _rulePreviewSummary = "Previzualizarea regulii va apărea după completarea câmpurilor.";
    private bool _ruleEnabled = true;
    private bool _ruleIgnoreCase = true;

    public CategoriesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        OpenAddCategoryModalCommand = new RelayCommand(_ => OpenCategoryEditor(isEditing: false));
        OpenEditCategoryModalCommand = new RelayCommand(_ => OpenCategoryEditor(isEditing: true));
        SaveCategoryCommand = new RelayCommand(_ => SaveCategory());
        OpenDeleteCategoryModalCommand = new RelayCommand(_ => OpenDeleteCategoryModal());
        DeleteSelectedCategoryCommand = new RelayCommand(_ => DeleteSelectedCategory());
        OpenApplicationAssignmentModalCommand = new RelayCommand(_ => OpenApplicationAssignmentModal());
        SaveApplicationCategoryCommand = new RelayCommand(_ => SaveApplicationCategory());
        ClearApplicationSelectionCommand = new RelayCommand(_ => ClearApplicationSelection());
        AssignSelectedCategoryToApplicationCommand = new RelayCommand(_ => AssignSelectedCategoryToApplication());
        NewRuleCommand = new RelayCommand(_ => BeginNewRule());
        OpenEditRuleModalCommand = new RelayCommand(_ => OpenSelectedRuleForEditing());
        SaveRuleCommand = new RelayCommand(_ => SaveRule());
        OpenDeleteRuleModalCommand = new RelayCommand(_ => OpenDeleteRuleModal());
        DeleteSelectedRuleCommand = new RelayCommand(_ => DeleteSelectedRule());
        CreateRuleFromProcessCommand = new RelayCommand(_ => PrepareRuleFromSelectedApplication(CategoryRuleField.ProcessName));
        CreateRuleFromClassCommand = new RelayCommand(_ => PrepareRuleFromSelectedApplication(CategoryRuleField.ClassName));
        CreateRuleFromWindowTitleCommand = new RelayCommand(_ => PrepareRuleFromSelectedApplication(CategoryRuleField.WindowTitle));
        CloseModalCommand = new RelayCommand(_ => CloseModal());

        ApplicationScopeOptions.Add(new ApplicationScopeOption { Scope = ApplicationScope.All, Label = "Toate aplicațiile" });
        ApplicationScopeOptions.Add(new ApplicationScopeOption { Scope = ApplicationScope.SelectedCategory, Label = "Doar categoria selectată" });
        ApplicationScopeOptions.Add(new ApplicationScopeOption { Scope = ApplicationScope.Uncategorized, Label = "Doar neasignate" });
        _selectedApplicationScope = ApplicationScopeOptions[0];

        RuleTargetOptions.Add(new RuleTargetOption { Value = CategoryRuleTarget.Application, Label = "Aplicație desktop" });
        RuleTargetOptions.Add(new RuleTargetOption { Value = CategoryRuleTarget.Website, Label = "Website / tab browser" });

        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Contains, Label = "Contine textul" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Exact, Label = "Potrivire exactă" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.StartsWith, Label = "Incepe cu" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.EndsWith, Label = "Se termina cu" });
        RuleMatchTypeOptions.Add(new RuleMatchTypeOption { Value = CategoryRuleMatchType.Regex, Label = "Regex" });

        SetRuleDraftDefaults();
        Load();
    }

    public ObservableCollection<CategoryListItem> Categories { get; } = [];

    public ObservableCollection<ApplicationCategoryRow> Applications { get; } = [];

    public ObservableCollection<CategoryAssignmentOption> CategoryOptions { get; } = [];

    public ObservableCollection<ApplicationScopeOption> ApplicationScopeOptions { get; } = [];

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

    public ICommand OpenApplicationAssignmentModalCommand { get; }

    public ICommand SaveApplicationCategoryCommand { get; }

    public ICommand ClearApplicationSelectionCommand { get; }

    public ICommand AssignSelectedCategoryToApplicationCommand { get; }

    public ICommand NewRuleCommand { get; }

    public ICommand OpenEditRuleModalCommand { get; }

    public ICommand SaveRuleCommand { get; }

    public ICommand OpenDeleteRuleModalCommand { get; }

    public ICommand DeleteSelectedRuleCommand { get; }

    public ICommand CreateRuleFromProcessCommand { get; }

    public ICommand CreateRuleFromClassCommand { get; }

    public ICommand CreateRuleFromWindowTitleCommand { get; }

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

    public string AssignmentStatus
    {
        get => _assignmentStatus;
        set => SetProperty(ref _assignmentStatus, value);
    }

    public string RuleStatusMessage
    {
        get => _ruleStatusMessage;
        set => SetProperty(ref _ruleStatusMessage, value);
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

    public string ApplicationSearchText
    {
        get => _applicationSearchText;
        set
        {
            if (!SetProperty(ref _applicationSearchText, value))
            {
                return;
            }

            FilterApplications();
        }
    }

    public CategoryListItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value))
            {
                return;
            }

            UpdateSelectedCategoryDetails();
            RefreshRulesForSelectedCategory();
            FilterApplications();
            UpdateRulePreview();
            OnPropertyChanged(nameof(HasSelectedCategory));
            OnPropertyChanged(nameof(CanBuildRuleFromSelectedApplication));
        }
    }

    public ApplicationCategoryRow? SelectedApplication
    {
        get => _selectedApplication;
        set
        {
            if (!SetProperty(ref _selectedApplication, value))
            {
                return;
            }

            SyncSelectedApplicationCategory();
            UpdateSelectedApplicationDetails();
            UpdateRulePreview();
            OnPropertyChanged(nameof(HasSelectedApplication));
            OnPropertyChanged(nameof(CanBuildRuleFromSelectedApplication));
        }
    }

    public CategoryAssignmentOption? SelectedApplicationCategory
    {
        get => _selectedApplicationCategory;
        set => SetProperty(ref _selectedApplicationCategory, value);
    }

    public ApplicationScopeOption? SelectedApplicationScope
    {
        get => _selectedApplicationScope;
        set
        {
            if (!SetProperty(ref _selectedApplicationScope, value))
            {
                return;
            }

            FilterApplications();
        }
    }

    public string SelectedCategoryTitle
    {
        get => _selectedCategoryTitle;
        set => SetProperty(ref _selectedCategoryTitle, value);
    }

    public string SelectedCategoryDescription
    {
        get => _selectedCategoryDescription;
        set => SetProperty(ref _selectedCategoryDescription, value);
    }

    public string SelectedCategoryUsage
    {
        get => _selectedCategoryUsage;
        set => SetProperty(ref _selectedCategoryUsage, value);
    }

    public string SelectedCategoryRuleSummary
    {
        get => _selectedCategoryRuleSummary;
        set => SetProperty(ref _selectedCategoryRuleSummary, value);
    }

    public string SelectedCategoryAutomationSummary
    {
        get => _selectedCategoryAutomationSummary;
        set => SetProperty(ref _selectedCategoryAutomationSummary, value);
    }

    public string SelectedApplicationTitle
    {
        get => _selectedApplicationTitle;
        set => SetProperty(ref _selectedApplicationTitle, value);
    }

    public string SelectedApplicationDetail
    {
        get => _selectedApplicationDetail;
        set => SetProperty(ref _selectedApplicationDetail, value);
    }

    public string SelectedApplicationIdentity
    {
        get => _selectedApplicationIdentity;
        set => SetProperty(ref _selectedApplicationIdentity, value);
    }

    public string SelectedApplicationCategoryLabel
    {
        get => _selectedApplicationCategoryLabel;
        set => SetProperty(ref _selectedApplicationCategoryLabel, value);
    }

    public CategoriesModalKind ActiveModal
    {
        get => _activeModal;
        private set
        {
            if (!SetProperty(ref _activeModal, value))
            {
                return;
            }

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

    public string DeleteCategoryMessage
    {
        get => _deleteCategoryMessage;
        set => SetProperty(ref _deleteCategoryMessage, value);
    }

    public string ApplicationAssignmentTitle
    {
        get => _applicationAssignmentTitle;
        set => SetProperty(ref _applicationAssignmentTitle, value);
    }

    public string ApplicationAssignmentDescription
    {
        get => _applicationAssignmentDescription;
        set => SetProperty(ref _applicationAssignmentDescription, value);
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

    public string DeleteRuleMessage
    {
        get => _deleteRuleMessage;
        set => SetProperty(ref _deleteRuleMessage, value);
    }

    public CategoryRuleListItem? SelectedCategoryRule
    {
        get => _selectedCategoryRule;
        set
        {
            if (!SetProperty(ref _selectedCategoryRule, value))
            {
                return;
            }

            if (value == null)
            {
                SetRuleDraftDefaults();
            }
            else
            {
                LoadRuleDraft(value.Rule);
            }

            OnPropertyChanged(nameof(HasSelectedCategoryRule));
        }
    }

    public RuleTargetOption? SelectedRuleTarget
    {
        get => _selectedRuleTarget;
        set
        {
            if (!SetProperty(ref _selectedRuleTarget, value))
            {
                return;
            }

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
            {
                return;
            }

            UpdateRulePreview();
        }
    }

    public RuleMatchTypeOption? SelectedRuleMatchType
    {
        get => _selectedRuleMatchType;
        set
        {
            if (!SetProperty(ref _selectedRuleMatchType, value))
            {
                return;
            }

            UpdateRulePreview();
        }
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

    public bool HasSelectedCategory => SelectedCategory != null;

    public bool HasSelectedApplication => SelectedApplication != null;

    public bool HasSelectedCategoryRule => SelectedCategoryRule != null;

    public bool CanBuildRuleFromSelectedApplication => HasSelectedCategory && HasSelectedApplication;

    private void Load(int? selectedCategoryId = null, int? selectedApplicationId = null, string? selectedRuleId = null)
    {
        var currentCategoryId = selectedCategoryId ?? SelectedCategory?.Id;
        var currentApplicationId = selectedApplicationId ?? SelectedApplication?.AppId;
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

        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryAssignmentOption
        {
            CategoryId = null,
            Label = "Neasignată"
        });

        foreach (var category in Categories)
        {
            CategoryOptions.Add(new CategoryAssignmentOption
            {
                CategoryId = category.Id,
                Label = category.Name
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
            ? "Nu există aplicații monitorizate în baza de date. Categoriile și regulile pot fi pregătite anticipat."
            : "Revizuiește categoriile existente, curăță asignările manuale și transformă deciziile repetate în reguli reutilizabile.";

        SelectedCategory = currentCategoryId.HasValue
            ? Categories.FirstOrDefault(category => category.Id == currentCategoryId.Value)
            : Categories.FirstOrDefault();

        RefreshRulesForSelectedCategory(currentRuleId);
        FilterApplications();

        SelectedApplication = currentApplicationId.HasValue
            ? Applications.FirstOrDefault(app => app.AppId == currentApplicationId.Value)
            : Applications.FirstOrDefault();

        if (SelectedCategory == null)
        {
            UpdateSelectedCategoryDetails();
        }

        if (SelectedApplication == null)
        {
            UpdateSelectedApplicationDetails();
        }

        StatusMessage = Categories.Count == 0
            ? "Nu există categorii definite. Adaugă prima categorie pentru a începe clasificarea."
            : $"Sunt disponibile {Categories.Count} categorii, {_allApplications.Count} aplicații și {_allRules.Count} reguli personalizate.";
    }

    private void OpenCategoryEditor(bool isEditing)
    {
        if (isEditing && SelectedCategory == null)
        {
            StatusMessage = "Selectează o categorie înainte de modificare.";
            return;
        }

        _isEditingCategory = isEditing;
        CategoryEditorTitle = isEditing ? "Modifică categoria" : "Adaugă categorie";
        CategoryEditorDescription = isEditing
            ? "Actualizează numele sau descrierea categoriei selectate fără să ieși din contextul paginii."
            : "Definește o categorie clară pe care o poți reutiliza în clasificare și raportare.";
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
            StatusMessage = $"Categoria \"{name}\" exista deja.";
            return;
        }

        if (_isEditingCategory)
        {
            if (SelectedCategory == null)
            {
                StatusMessage = "Selectează o categorie înainte de modificare.";
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
                StatusMessage = $"Categoria \"{SelectedCategory.Name}\" nu a putut fi actualizată.";
                return;
            }

            Load(SelectedCategory.Id, SelectedApplication?.AppId, SelectedCategoryRule?.Rule.Id);
            CloseModal();
            StatusMessage = $"Categoria \"{name}\" a fost actualizată.";
            return;
        }

        var categoryId = _db.InsertCategory(new CategoryDto
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        });

        Load(categoryId, SelectedApplication?.AppId, SelectedCategoryRule?.Rule.Id);
        CloseModal();
        StatusMessage = $"Categoria \"{name}\" a fost adăugată.";
    }

    private void OpenDeleteCategoryModal()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selectează o categorie înainte de ștergere.";
            return;
        }

        var affectedApplications = _allApplications.Count(app => app.CategoryId == SelectedCategory.Id);
        var affectedRules = _allRules.Count(rule => rule.CategoryId == SelectedCategory.Id);
        DeleteCategoryMessage = BuildDeleteCategoryMessage(SelectedCategory.Name, affectedApplications, affectedRules);
        ActiveModal = CategoriesModalKind.DeleteCategory;
    }

    private void DeleteSelectedCategory()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selectează o categorie înainte de ștergere.";
            return;
        }

        var deletedCategory = SelectedCategory;
        var affectedApplications = _allApplications.Count(app => app.CategoryId == deletedCategory.Id);
        var selectedApplicationId = SelectedApplication?.AppId;

        var removedRules = _allRules
            .Where(rule => rule.CategoryId == deletedCategory.Id)
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.Ordinal);

        var result = _db.DeleteCategory(deletedCategory.Id);

        if (result == 0)
        {
            StatusMessage = $"Categoria \"{deletedCategory.Name}\" nu a putut fi ștearsă.";
            return;
        }

        if (removedRules.Count > 0)
        {
            _allRules = _allRules
                .Where(rule => !removedRules.Contains(rule.Id))
                .ToList();
            _ruleStore.SaveRules(_allRules);
        }

        Load(selectedApplicationId: selectedApplicationId);
        CloseModal();
        StatusMessage = affectedApplications == 0
            ? $"Categoria \"{deletedCategory.Name}\" a fost stearsa."
            : $"Categoria \"{deletedCategory.Name}\" a fost ștearsă, iar {affectedApplications} aplicații au rămas neasignate.";
    }

    private void OpenApplicationAssignmentModal()
    {
        if (SelectedApplication == null)
        {
            AssignmentStatus = "Selectează o aplicație înainte de modificare.";
            return;
        }

        SyncSelectedApplicationCategory();
        ApplicationAssignmentTitle = SelectedApplication.PrimaryLabel;
        ApplicationAssignmentDescription = $"Ajusteaza categoria pentru aplicatia selectata. Categoria curenta este \"{SelectedApplication.CategoryName}\".";
        ActiveModal = CategoriesModalKind.ApplicationAssignment;
    }

    private void SaveApplicationCategory()
    {
        if (SelectedApplication == null)
        {
            AssignmentStatus = "Selectează o aplicație înainte de salvare.";
            return;
        }

        if (SelectedApplicationCategory == null)
        {
            AssignmentStatus = "Alege o categorie pentru aplicația selectată.";
            return;
        }

        if (SelectedApplication.CategoryId == SelectedApplicationCategory.CategoryId)
        {
            AssignmentStatus = SelectedApplicationCategory.CategoryId.HasValue
                ? $"Aplicatia este deja clasificata ca \"{SelectedApplicationCategory.Label}\"."
                : "Aplicația este deja neasignată.";
            return;
        }

        var result = _db.UpdateApplicationCategory(SelectedApplication.AppId, SelectedApplicationCategory.CategoryId);
        if (result == 0)
        {
            AssignmentStatus = "Asignarea categoriei nu a putut fi salvată.";
            return;
        }

        var statusLabel = SelectedApplicationCategory.CategoryId.HasValue
            ? $"Aplicația a fost asignată categoriei \"{SelectedApplicationCategory.Label}\"."
            : "Categoria aplicației a fost eliminată.";

        Load(SelectedCategory?.Id, SelectedApplication.AppId, SelectedCategoryRule?.Rule.Id);
        CloseModal();
        AssignmentStatus = statusLabel;
    }

    private void ClearApplicationSelection()
    {
        SelectedApplication = null;
        AssignmentStatus = "Selecția aplicației a fost resetată.";
    }

    private void AssignSelectedCategoryToApplication()
    {
        if (SelectedCategory == null)
        {
            AssignmentStatus = "Selectează mai întâi categoria care trebuie aplicată.";
            return;
        }

        if (SelectedApplication == null)
        {
            AssignmentStatus = "Selectează o aplicație pentru asignare rapidă.";
            return;
        }

        SelectedApplicationCategory = CategoryOptions.FirstOrDefault(option => option.CategoryId == SelectedCategory.Id);
        SaveApplicationCategory();
    }

    private void BeginNewRule()
    {
        if (SelectedCategory == null)
        {
            RuleStatusMessage = "Selectează mai întâi categoria pentru care creezi regula.";
            return;
        }

        SelectedCategoryRule = null;
        RuleEditorTitle = "Regula noua";
        RuleEditorDescription = $"Configureaza o regula noua pentru categoria \"{SelectedCategory.Name}\".";
        RuleStatusMessage = $"Configureaza o regula noua pentru categoria \"{SelectedCategory.Name}\".";
        ActiveModal = CategoriesModalKind.RuleEditor;
    }

    private void OpenSelectedRuleForEditing()
    {
        if (SelectedCategory == null)
        {
            RuleStatusMessage = "Selectează mai întâi categoria pentru care vrei să modifici regula.";
            return;
        }

        if (SelectedCategoryRule == null)
        {
            RuleStatusMessage = "Selectează o regulă înainte de modificare.";
            return;
        }

        RuleEditorTitle = "Modifică regula";
        RuleEditorDescription = $"Revizuieste regula \"{SelectedCategoryRule.Title}\" pentru categoria \"{SelectedCategory.Name}\".";
        ActiveModal = CategoriesModalKind.RuleEditor;
    }

    private void SaveRule()
    {
        if (!TryBuildRuleFromDraft(out var rule, out var error))
        {
            RuleStatusMessage = error;
            RulePreviewSummary = error;
            return;
        }

        if (!CategoryRuleMatcher.TryValidate(rule, out error))
        {
            RuleStatusMessage = error;
            RulePreviewSummary = error;
            return;
        }

        var existingIndex = _allRules.FindIndex(existingRule => string.Equals(existingRule.Id, rule.Id, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            _allRules[existingIndex] = rule;
        }
        else
        {
            _allRules.Add(rule);
        }

        _ruleStore.SaveRules(_allRules);
        _allRules = _ruleStore.LoadRules().ToList();
        RefreshRulesForSelectedCategory(rule.Id);
        CloseModal();
        RuleStatusMessage = existingIndex >= 0
            ? "Regula a fost actualizata si va avea prioritate fata de clasificarea implicita."
            : "Regula a fost salvata si va fi folosita la urmatoarea clasificare.";
    }

    private void OpenDeleteRuleModal()
    {
        if (SelectedCategoryRule == null)
        {
            RuleStatusMessage = "Selectează o regulă înainte de ștergere.";
            return;
        }

        DeleteRuleMessage = $"Regula \"{SelectedCategoryRule.Title}\" va fi eliminata. Acoperirea estimata curenta este: {SelectedCategoryRule.MatchPreview}";
        ActiveModal = CategoriesModalKind.DeleteRule;
    }

    private void DeleteSelectedRule()
    {
        if (SelectedCategoryRule == null)
        {
            RuleStatusMessage = "Selectează o regulă înainte de ștergere.";
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
        RuleStatusMessage = $"Regula \"{removedTitle}\" a fost stearsa.";
    }

    private void PrepareRuleFromSelectedApplication(CategoryRuleField field)
    {
        if (SelectedCategory == null)
        {
            RuleStatusMessage = "Selectează mai întâi categoria pentru care vrei să construiești regula.";
            return;
        }

        if (SelectedApplication == null)
        {
            RuleStatusMessage = "Selectează o aplicație din listă pentru a genera regula.";
            return;
        }

        var pattern = field switch
        {
            CategoryRuleField.ProcessName => SelectedApplication.ProcessName,
            CategoryRuleField.ClassName => SelectedApplication.ClassName,
            CategoryRuleField.WindowTitle => SelectedApplication.WindowTitle,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(pattern))
        {
            RuleStatusMessage = "Aplicația selectată nu are suficient context pentru tipul de regulă ales.";
            return;
        }

        _isUpdatingRuleDraft = true;
        SelectedCategoryRule = null;
        SelectedRuleTarget = RuleTargetOptions.First(option => option.Value == CategoryRuleTarget.Application);
        SelectedRuleMatchType = RuleMatchTypeOptions.First(option => option.Value == (field == CategoryRuleField.WindowTitle ? CategoryRuleMatchType.Contains : CategoryRuleMatchType.Exact));
        SelectedRuleField = RuleFieldOptions.First(option => option.Value == field);
        RuleName = field switch
        {
            CategoryRuleField.ProcessName => $"Proces: {pattern}",
            CategoryRuleField.ClassName => $"Clasa: {pattern}",
            CategoryRuleField.WindowTitle => $"Titlu: {TrimForLabel(pattern)}",
            _ => string.Empty
        };
        RulePattern = pattern;
        RulePriorityText = "10";
        RuleIgnoreCase = true;
        RuleEnabled = true;
        RuleNotes = $"Generată din aplicația selectată: {SelectedApplication.PrimaryLabel}";
        _isUpdatingRuleDraft = false;

        UpdateRulePreview();
        RuleEditorTitle = "Regulă nouă din aplicație";
        RuleEditorDescription = "Câmpurile au fost completate din aplicația selectată. Revizuiește potrivirea și salvează.";
        RuleStatusMessage = "Câmpurile regulii au fost completate din aplicația selectată. Revizuiește potrivirea și salvează.";
        ActiveModal = CategoriesModalKind.RuleEditor;
    }

    private void CloseModal()
    {
        ActiveModal = CategoriesModalKind.None;
    }

    private void FilterApplications()
    {
        var query = (ApplicationSearchText ?? string.Empty).Trim();
        IEnumerable<ApplicationCategoryRow> filtered = _allApplications;

        filtered = SelectedApplicationScope?.Scope switch
        {
            ApplicationScope.SelectedCategory when SelectedCategory != null => filtered.Where(app => app.CategoryId == SelectedCategory.Id),
            ApplicationScope.Uncategorized => filtered.Where(app => !app.CategoryId.HasValue),
            _ => filtered
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(app =>
                Contains(app.PrimaryLabel, query) ||
                Contains(app.WindowTitle, query) ||
                Contains(app.ClassName, query) ||
                Contains(app.CategoryName, query));
        }

        var selectedApplicationId = SelectedApplication?.AppId;

        Applications.Clear();
        foreach (var app in filtered)
        {
            Applications.Add(app);
        }

        SelectedApplication = selectedApplicationId.HasValue
            ? Applications.FirstOrDefault(app => app.AppId == selectedApplicationId.Value)
            : null;

        if (SelectedApplication == null)
        {
            UpdateSelectedApplicationDetails();
        }
    }

    private void RefreshRulesForSelectedCategory(string? selectedRuleId = null)
    {
        CategoryRules.Clear();

        if (SelectedCategory == null)
        {
            SelectedCategoryRule = null;
            SelectedCategoryRuleSummary = "Selectează o categorie pentru a administra regulile ei personalizate.";
            SelectedCategoryAutomationSummary = "Acoperirea regulilor va fi calculată după selecția unei categorii.";
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

        var matchedApplications = rules
            .Where(rule => rule.Target == CategoryRuleTarget.Application)
            .SelectMany(rule => _allApplications.Where(app => CategoryRuleMatcher.IsMatch(rule, ToApplicationRecord(app))))
            .Select(app => app.AppId)
            .Distinct()
            .Count();

        var matchedWebsites = rules
            .Where(rule => rule.Target == CategoryRuleTarget.Website)
            .SelectMany(rule => _allBrowserActivities.Where(browser => CategoryRuleMatcher.IsMatch(rule, browser)))
            .Select(browser => browser.Id)
            .Distinct()
            .Count();

        SelectedCategoryRuleSummary = rules.Count == 0
            ? "Categoria nu are reguli personalizate. Se vor folosi doar regulile implicite din backend."
            : rules.Count == 1
                ? "Categoria are 1 regulă personalizată."
                : $"Categoria are {rules.Count} reguli personalizate.";

        SelectedCategoryAutomationSummary = rules.Count == 0
            ? "Creează reguli pentru proces, clasă sau titlu de fereastră atunci când faci aceeași asignare în mod repetat."
            : $"Regulile actuale ar potrivi {matchedApplications} aplicații și {matchedWebsites} activități web deja stocate.";

        SelectedCategoryRule = !string.IsNullOrWhiteSpace(selectedRuleId)
            ? CategoryRules.FirstOrDefault(rule => string.Equals(rule.Rule.Id, selectedRuleId, StringComparison.Ordinal))
            : null;

        if (SelectedCategoryRule == null)
        {
            SetRuleDraftDefaults();
        }
    }

    private void UpdateSelectedCategoryDetails()
    {
        if (SelectedCategory == null)
        {
            SelectedCategoryTitle = "Nicio categorie selectată";
            SelectedCategoryDescription = "Alege o categorie din listă pentru a vedea impactul ei asupra aplicațiilor monitorizate.";
            SelectedCategoryUsage = "Aplicațiile asociate vor apărea în lista de mai jos.";
            return;
        }

        SelectedCategoryTitle = SelectedCategory.Name;
        SelectedCategoryDescription = SelectedCategory.DescriptionPreview;
        SelectedCategoryUsage = SelectedCategory.ApplicationCount == 0
            ? "Nicio aplicație nu este asociată în prezent acestei categorii."
            : SelectedCategory.UsageSummary;
    }

    private void UpdateSelectedApplicationDetails()
    {
        if (SelectedApplication == null)
        {
            SelectedApplicationTitle = "Nicio aplicație selectată";
            SelectedApplicationDetail = "Selectează o aplicație monitorizată pentru a-i atribui o categorie sau pentru a genera o regulă nouă.";
            SelectedApplicationIdentity = "Detaliile tehnice vor apărea aici.";
            SelectedApplicationCategoryLabel = "Categoria curentă va fi afișată după selecție.";
            return;
        }

        SelectedApplicationTitle = SelectedApplication.PrimaryLabel;
        SelectedApplicationDetail = SelectedApplication.SecondaryLabel;
        SelectedApplicationIdentity = SelectedApplication.IdentitySummary;
        SelectedApplicationCategoryLabel = $"Categoria curentă: {SelectedApplication.CategoryName}";
    }

    private void SyncSelectedApplicationCategory()
    {
        if (SelectedApplication == null)
        {
            SelectedApplicationCategory = CategoryOptions.FirstOrDefault(option => option.CategoryId == null);
            return;
        }

        SelectedApplicationCategory = CategoryOptions.FirstOrDefault(option => option.CategoryId == SelectedApplication.CategoryId)
            ?? CategoryOptions.FirstOrDefault(option => option.CategoryId == null);
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
        {
            RuleFieldOptions.Add(option);
        }

        SelectedRuleField = RuleFieldOptions.FirstOrDefault(option => option.Value == previousField) ?? RuleFieldOptions.FirstOrDefault();
    }

    private void UpdateRulePreview()
    {
        if (_isUpdatingRuleDraft)
        {
            return;
        }

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
            : $"{preview} Prioritatea mica inseamna ca regula este testata mai devreme.";
    }

    private (int MatchCount, string Preview) BuildRuleCoverage(CategoryRule rule)
    {
        if (rule.Target == CategoryRuleTarget.Application)
        {
            var matches = _allApplications
                .Where(app => CategoryRuleMatcher.IsMatch(rule, ToApplicationRecord(app)))
                .ToList();

            if (matches.Count == 0)
            {
                return (0, "Previzualizare: nicio aplicație existentă nu se potrivește în acest moment.");
            }

            var labelsPrimary = string.Join(", ", matches.Take(3).Select(app => app.PrimaryLabel));
            var labelsPrimarySuffix = matches.Count > 3 ? ", ..." : string.Empty;
            return (matches.Count, $"Previzualizare: {matches.Count} aplicații s-ar potrivi ({labelsPrimary}{labelsPrimarySuffix}).");
        }

        var browserMatches = _allBrowserActivities
            .Where(browser => CategoryRuleMatcher.IsMatch(rule, browser))
            .ToList();

        if (browserMatches.Count == 0)
        {
            return (0, "Previzualizare: nicio activitate web existentă nu se potrivește în acest moment.");
        }

        var labels = string.Join(", ", browserMatches
            .Select(browser => string.IsNullOrWhiteSpace(browser.Domain) ? browser.Url : browser.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3));
        var domainCount = browserMatches
            .Select(browser => string.IsNullOrWhiteSpace(browser.Domain) ? browser.Url : browser.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var suffix = domainCount > 3 ? ", ..." : string.Empty;
        return (browserMatches.Count, $"Previzualizare: {browserMatches.Count} intrari web din {domainCount} domenii s-ar potrivi ({labels}{suffix}).");
    }

    private bool TryBuildRuleFromDraft(out CategoryRule rule, out string error)
    {
        rule = new CategoryRule();

        if (SelectedCategory == null)
        {
            error = "Selectează mai întâi categoria pentru regulă.";
            return false;
        }

        if (SelectedRuleTarget == null || SelectedRuleField == null || SelectedRuleMatchType == null)
        {
            error = "Completează tipul, câmpul și modul de potrivire pentru regulă.";
            return false;
        }

        var pattern = (RulePattern ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "Introdu textul sau regex-ul care va declansa clasificarea.";
            return false;
        }

        var priorityText = (RulePriorityText ?? string.Empty).Trim();
        if (!int.TryParse(string.IsNullOrWhiteSpace(priorityText) ? "100" : priorityText, out var priority))
        {
            error = "Prioritatea trebuie să fie un număr întreg.";
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
        {
            return;
        }

        if (updatePreview)
        {
            UpdateRulePreview();
        }
    }

    private static string BuildDeleteCategoryMessage(string categoryName, int affectedApplications, int affectedRules)
    {
        var appMessage = affectedApplications == 0
            ? "Nicio aplicație nu depinde de această categorie."
            : affectedApplications == 1
                ? "1 aplicație va rămâne neasignată."
                : $"{affectedApplications} aplicații vor rămâne neasignate.";

        var ruleMessage = affectedRules == 0
            ? "Nu exista reguli personalizate asociate."
            : affectedRules == 1
                ? "1 regulă personalizată va fi eliminată."
                : $"{affectedRules} reguli personalizate vor fi eliminate.";

        return $"Categoria \"{categoryName}\" va fi stearsa. {appMessage} {ruleMessage}";
    }

    private static IEnumerable<RuleFieldOption> GetFieldOptionsForTarget(CategoryRuleTarget target)
    {
        return target == CategoryRuleTarget.Application
            ?
            [
                new RuleFieldOption { Value = CategoryRuleField.ProcessName, Label = "Proces" },
                new RuleFieldOption { Value = CategoryRuleField.ClassName, Label = "Clasa fereastra" },
                new RuleFieldOption { Value = CategoryRuleField.WindowTitle, Label = "Titlu fereastra" },
                new RuleFieldOption { Value = CategoryRuleField.Any, Label = "Orice câmp aplicație" }
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

    private static string TrimForLabel(string value)
    {
        return value.Length <= 40 ? value : $"{value[..37]}...";
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }
}
