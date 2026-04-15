using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
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
            ? "Fara descriere"
            : Description;

    public string UsageSummary =>
        ApplicationCount == 1
            ? "1 aplicatie asociata"
            : $"{ApplicationCount} aplicatii asociate";
}

public sealed class ApplicationCategoryRow
{
    public int AppId { get; init; }
    public int? CategoryId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = "Neasignata";

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
            ? $"ID aplicatie: {AppId}"
            : $"Clasa: {ClassName} | ID aplicatie: {AppId}";

    public string ClassSummary =>
        string.IsNullOrWhiteSpace(ClassName)
            ? "Clasa fereastra indisponibila"
            : ClassName;
}

public sealed class CategoryAssignmentOption
{
    public int? CategoryId { get; init; }
    public string Label { get; init; } = string.Empty;

    public override string ToString() => Label;
}

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private List<ApplicationCategoryRow> _allApplications = [];

    private string _pageSubtitle = "Administrarea categoriilor si clasificarea aplicatiilor monitorizate.";
    private string _statusMessage = "Se incarca categoriile...";
    private string _assignmentStatus = "Selecteaza o aplicatie pentru a-i modifica categoria.";
    private string _lastRefreshLabel = "Actualizare in curs";
    private string _categoryCount = "0";
    private string _assignedApplications = "0";
    private string _uncategorizedApplications = "0";
    private string _newCategoryName = string.Empty;
    private string _newCategoryDescription = string.Empty;
    private string _applicationSearchText = string.Empty;
    private CategoryListItem? _selectedCategory;
    private ApplicationCategoryRow? _selectedApplication;
    private CategoryAssignmentOption? _selectedApplicationCategory;
    private string _selectedCategoryTitle = "Nicio categorie selectata";
    private string _selectedCategoryDescription = "Alege o categorie din lista pentru a vedea impactul ei asupra aplicatiilor monitorizate.";
    private string _selectedCategoryUsage = "Aplicatiile asociate vor aparea in panoul din dreapta.";
    private string _selectedApplicationTitle = "Nicio aplicatie selectata";
    private string _selectedApplicationDetail = "Selecteaza o aplicatie monitorizata pentru a-i atribui o categorie.";
    private string _selectedApplicationIdentity = "Detaliile tehnice vor aparea aici.";
    private string _selectedApplicationCategoryLabel = "Categoria curenta va fi afisata dupa selectie.";

    public CategoriesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        AddCategoryCommand = new RelayCommand(_ => AddCategory());
        DeleteSelectedCategoryCommand = new RelayCommand(_ => DeleteSelectedCategory());
        SaveApplicationCategoryCommand = new RelayCommand(_ => SaveApplicationCategory());
        ClearApplicationSelectionCommand = new RelayCommand(_ => ClearApplicationSelection());

        Load();
    }

    public ObservableCollection<CategoryListItem> Categories { get; } = [];

    public ObservableCollection<ApplicationCategoryRow> Applications { get; } = [];

    public ObservableCollection<CategoryAssignmentOption> CategoryOptions { get; } = [];

    public ICommand RefreshCommand { get; }

    public ICommand AddCategoryCommand { get; }

    public ICommand DeleteSelectedCategoryCommand { get; }

    public ICommand SaveApplicationCategoryCommand { get; }

    public ICommand ClearApplicationSelectionCommand { get; }

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

    public string NewCategoryName
    {
        get => _newCategoryName;
        set => SetProperty(ref _newCategoryName, value);
    }

    public string NewCategoryDescription
    {
        get => _newCategoryDescription;
        set => SetProperty(ref _newCategoryDescription, value);
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
            OnPropertyChanged(nameof(HasSelectedCategory));
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
            OnPropertyChanged(nameof(HasSelectedApplication));
        }
    }

    public CategoryAssignmentOption? SelectedApplicationCategory
    {
        get => _selectedApplicationCategory;
        set => SetProperty(ref _selectedApplicationCategory, value);
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

    public bool HasSelectedCategory => SelectedCategory != null;

    public bool HasSelectedApplication => SelectedApplication != null;

    private void Load(int? selectedCategoryId = null, int? selectedApplicationId = null)
    {
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
            Label = "Neasignata"
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
                    : "Neasignata"
            })
            .OrderBy(app => app.CategoryName == "Neasignata" ? 1 : 0)
            .ThenBy(app => app.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(app => app.PrimaryLabel, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FilterApplications();

        CategoryCount = Categories.Count.ToString();
        AssignedApplications = _allApplications.Count(app => app.CategoryId.HasValue).ToString();
        UncategorizedApplications = _allApplications.Count(app => !app.CategoryId.HasValue).ToString();
        LastRefreshLabel = $"Actualizat la {DateTime.Now:HH:mm}";
        PageSubtitle = _allApplications.Count == 0
            ? "Nu exista aplicatii monitorizate in baza de date. Categoriile pot fi pregatite anticipat."
            : "Revizuieste categoriile existente si aliniaza aplicatiile monitorizate cu clasificarea dorita.";

        SelectedCategory = selectedCategoryId.HasValue
            ? Categories.FirstOrDefault(category => category.Id == selectedCategoryId.Value)
            : Categories.FirstOrDefault();

        SelectedApplication = selectedApplicationId.HasValue
            ? Applications.FirstOrDefault(app => app.AppId == selectedApplicationId.Value)
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
            ? "Nu exista categorii definite. Adauga prima categorie pentru a incepe clasificarea."
            : $"Sunt disponibile {Categories.Count} categorii pentru {_allApplications.Count} aplicatii monitorizate.";
    }

    private void AddCategory()
    {
        var name = (NewCategoryName ?? string.Empty).Trim();
        var description = (NewCategoryDescription ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Numele categoriei este obligatoriu.";
            return;
        }

        if (Categories.Any(category => string.Equals(category.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            StatusMessage = $"Categoria \"{name}\" exista deja.";
            return;
        }

        var categoryId = _db.InsertCategory(new CategoryDto
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        });

        NewCategoryName = string.Empty;
        NewCategoryDescription = string.Empty;

        Load(categoryId, SelectedApplication?.AppId);
        StatusMessage = $"Categoria \"{name}\" a fost adaugata.";
    }

    private void DeleteSelectedCategory()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Selecteaza o categorie inainte de stergere.";
            return;
        }

        var deletedCategory = SelectedCategory;
        var affectedApplications = _allApplications.Count(app => app.CategoryId == deletedCategory.Id);
        var selectedApplicationId = SelectedApplication?.AppId;
        var result = _db.DeleteCategory(deletedCategory.Id);

        if (result == 0)
        {
            StatusMessage = $"Categoria \"{deletedCategory.Name}\" nu a putut fi stearsa.";
            return;
        }

        Load(selectedApplicationId: selectedApplicationId);
        StatusMessage = affectedApplications == 0
            ? $"Categoria \"{deletedCategory.Name}\" a fost stearsa."
            : $"Categoria \"{deletedCategory.Name}\" a fost stearsa, iar {affectedApplications} aplicatii au ramas neasignate.";
    }

    private void SaveApplicationCategory()
    {
        if (SelectedApplication == null)
        {
            AssignmentStatus = "Selecteaza o aplicatie inainte de salvare.";
            return;
        }

        if (SelectedApplicationCategory == null)
        {
            AssignmentStatus = "Alege o categorie pentru aplicatia selectata.";
            return;
        }

        if (SelectedApplication.CategoryId == SelectedApplicationCategory.CategoryId)
        {
            AssignmentStatus = SelectedApplicationCategory.CategoryId.HasValue
                ? $"Aplicatia este deja clasificata ca \"{SelectedApplicationCategory.Label}\"."
                : "Aplicatia este deja neasignata.";
            return;
        }

        var result = _db.UpdateApplicationCategory(SelectedApplication.AppId, SelectedApplicationCategory.CategoryId);
        if (result == 0)
        {
            AssignmentStatus = "Asignarea categoriei nu a putut fi salvata.";
            return;
        }

        var statusLabel = SelectedApplicationCategory.CategoryId.HasValue
            ? $"Aplicatia a fost asignata categoriei \"{SelectedApplicationCategory.Label}\"."
            : "Categoria aplicatiei a fost eliminata.";

        var selectedCategoryId = SelectedApplicationCategory.CategoryId;
        var selectedApplicationId = SelectedApplication.AppId;

        Load(selectedCategoryId, selectedApplicationId);
        AssignmentStatus = statusLabel;
    }

    private void ClearApplicationSelection()
    {
        SelectedApplication = null;
        AssignmentStatus = "Selectia aplicatiei a fost resetata.";
    }

    private void FilterApplications()
    {
        var query = (ApplicationSearchText ?? string.Empty).Trim();
        IEnumerable<ApplicationCategoryRow> filtered = _allApplications;

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
    }

    private void UpdateSelectedCategoryDetails()
    {
        if (SelectedCategory == null)
        {
            SelectedCategoryTitle = "Nicio categorie selectata";
            SelectedCategoryDescription = "Alege o categorie din lista pentru a vedea impactul ei asupra aplicatiilor monitorizate.";
            SelectedCategoryUsage = "Aplicatiile asociate vor aparea in panoul din dreapta.";
            return;
        }

        SelectedCategoryTitle = SelectedCategory.Name;
        SelectedCategoryDescription = SelectedCategory.DescriptionPreview;
        SelectedCategoryUsage = SelectedCategory.ApplicationCount == 0
            ? "Nicio aplicatie nu este asociata in prezent acestei categorii."
            : SelectedCategory.UsageSummary;
    }

    private void UpdateSelectedApplicationDetails()
    {
        if (SelectedApplication == null)
        {
            SelectedApplicationTitle = "Nicio aplicatie selectata";
            SelectedApplicationDetail = "Selecteaza o aplicatie monitorizata pentru a-i atribui o categorie.";
            SelectedApplicationIdentity = "Detaliile tehnice vor aparea aici.";
            SelectedApplicationCategoryLabel = "Categoria curenta va fi afisata dupa selectie.";
            return;
        }

        SelectedApplicationTitle = SelectedApplication.PrimaryLabel;
        SelectedApplicationDetail = SelectedApplication.SecondaryLabel;
        SelectedApplicationIdentity = SelectedApplication.IdentitySummary;
        SelectedApplicationCategoryLabel = $"Categoria curenta: {SelectedApplication.CategoryName}";
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

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }
}
