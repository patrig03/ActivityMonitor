using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;

namespace ActivityMonitor.ViewModels;

public partial class InterventionsViewModel
{
    private void ResetEditData()
    {
        EditData = CreateDraft();
    }

    private ThresholdEditData CreateDraft(Threshold? threshold = null)
    {
        var draft = threshold ?? new Threshold();

        if (draft.CategoryId == 0 && Categories.Count > 0)
        {
            draft.CategoryId = Categories[0].Id;
        }

        if (draft.AppId == 0 && Apps.FirstOrDefault(a => a.Id.HasValue)?.Id is int firstAppId)
        {
            draft.AppId = firstAppId;
        }

        return new ThresholdEditData
        {
            Category = Categories.FirstOrDefault(c => c.Id == draft.CategoryId) ?? new Category { Name = string.Empty },
            Threshold = draft
        };
    }

    private void LoadCategories()
    {
        Categories.Clear();

        foreach (var category in _manager.GetAllCategories().Select(Category.FromDto).OrderBy(c => c.Name))
        {
            Categories.Add(category);
        }
    }

    private void LoadApps()
    {
        Apps.Clear();

        foreach (var app in _manager.GetAllApplications()
                     .Select(ApplicationRecord.FromDto)
                     .Where(a => a.Id.HasValue && !string.IsNullOrWhiteSpace(a.ProcessName))
                     .OrderBy(a => a.ProcessName)
                     .DistinctBy(a => a.ProcessName))
        {
            Apps.Add(app);
        }
    }

    private void RefreshCollections()
    {
        QueryThresholds();
        QueryInterventions();
        UpdateSummaries();
    }

    private void QueryThresholds()
    {
        ThresholdRows.Clear();

        var categoryLookup = Categories.ToDictionary(category => category.Id);
        var appLookup = Apps
            .Where(app => app.Id.HasValue)
            .ToDictionary(app => app.Id!.Value);

        foreach (var thresholdDto in _manager.GetAllThresholds())
        {
            if (thresholdDto is null)
            {
                continue;
            }

            var threshold = Threshold.FromDto(thresholdDto);
            var category = categoryLookup.GetValueOrDefault(threshold.CategoryId)
                           ?? new Category { Id = threshold.CategoryId, Name = $"Category {threshold.CategoryId}" };

            appLookup.TryGetValue(threshold.AppId, out var app);

            ThresholdRows.Add(new ThresholdRow
            {
                Category = category,
                App = app,
                Threshold = threshold
            });
        }
    }

    private void QueryInterventions()
    {
        InterventionHistory.Clear();
        RecentAlerts.Clear();

        var thresholdLookup = ThresholdRows.ToDictionary(row => row.Threshold.Id);

        var interventions = _manager.GetInterventionsForUser(1)
            .Select(Intervention.FromDto)
            .OrderByDescending(item => item.TriggeredAt)
            .ToList();

        foreach (var intervention in interventions)
        {
            thresholdLookup.TryGetValue(intervention.ThresholdId, out var thresholdRow);

            var row = new InterventionHistoryRow
            {
                Id = intervention.Id,
                ThresholdId = intervention.ThresholdId,
                TriggeredAt = intervention.TriggeredAt,
                Snoozed = intervention.Snoozed,
                TargetName = thresholdRow?.TargetName ?? $"Threshold #{intervention.ThresholdId}",
                TargetType = thresholdRow?.Threshold.TargetType ?? "Unknown",
                CategoryName = thresholdRow?.Category.Name ?? "Unknown",
                InterventionType = thresholdRow?.Threshold.InterventionType ?? "Unknown",
                LimitSummary = thresholdRow?.LimitSummary ?? "Unknown"
            };

            InterventionHistory.Add(row);
        }

        foreach (var row in InterventionHistory.Take(5))
        {
            RecentAlerts.Add(row);
        }
    }

    private void UpdateSummaries()
    {
        var activeThresholds = ThresholdRows.Count(row => row.Threshold.Active);
        var inactiveThresholds = ThresholdRows.Count - activeThresholds;
        var coveredCategories = ThresholdRows
            .Where(row => row.Threshold.TargetType == Threshold.CategoryTargetType)
            .Select(row => row.Category.Id)
            .Distinct()
            .Count();
        var recentAlerts = InterventionHistory.Count(row => row.TriggeredAt >= DateTime.Now.AddDays(-7));
        var snoozedAlerts = InterventionHistory.Count(row => row.Snoozed);
        var mostTriggered = InterventionHistory
            .GroupBy(row => row.TargetName)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        ActiveThresholdCount = activeThresholds.ToString();
        InactiveThresholdCount = inactiveThresholds.ToString();
        CategoryCoverage = coveredCategories.ToString();
        RecentAlertCount = recentAlerts.ToString();
        SnoozedAlertCount = snoozedAlerts.ToString();
        MostTriggeredTarget = mostTriggered == null
            ? "No interventions yet"
            : $"{mostTriggered.Key} ({mostTriggered.Count()} triggers)";
        ThresholdStatus = ThresholdRows.Count == 0
            ? "No thresholds configured yet."
            : $"Tracking {ThresholdRows.Count} thresholds across {coveredCategories} categories and {ThresholdRows.Count(row => row.Threshold.TargetType == Threshold.AppTargetType)} app rules.";
    }

    private void SyncDraftSelection()
    {
        if (EditData.Threshold.TargetType == Threshold.AppTargetType)
        {
            var selectedApp = Apps.FirstOrDefault(app => app.Id == EditData.Threshold.AppId);
            if (selectedApp?.CategoryId is int appCategoryId)
            {
                EditData.Threshold.CategoryId = appCategoryId;
            }

            return;
        }

        if (EditData.Threshold.CategoryId == 0 && Categories.Count > 0)
        {
            EditData.Threshold.CategoryId = Categories[0].Id;
        }
    }

    private void EnsureDraftSelection()
    {
        if (EditData.Threshold.TargetType == Threshold.AppTargetType)
        {
            if (EditData.Threshold.AppId == 0 && Apps.FirstOrDefault(a => a.Id.HasValue)?.Id is int firstAppId)
            {
                EditData.Threshold.AppId = firstAppId;
            }

            SyncDraftSelection();
            return;
        }

        if (EditData.Threshold.CategoryId == 0 && Categories.Count > 0)
        {
            EditData.Threshold.CategoryId = Categories[0].Id;
        }
    }

    private void AttachEditDataHandlers(ThresholdEditData? editData)
    {
        if (editData is null)
        {
            return;
        }

        editData.PropertyChanged += OnEditDataChanged;
        _observedEditThreshold = editData.Threshold;
        _observedEditThreshold.PropertyChanged += OnEditThresholdChanged;
        EnsureDraftSelection();
    }

    private void DetachEditDataHandlers(ThresholdEditData? editData)
    {
        if (editData is null)
        {
            return;
        }

        editData.PropertyChanged -= OnEditDataChanged;

        if (_observedEditThreshold is not null)
        {
            _observedEditThreshold.PropertyChanged -= OnEditThresholdChanged;
            _observedEditThreshold = null;
        }
    }

    private void OnEditDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ThresholdEditData.Threshold))
        {
            return;
        }

        if (_observedEditThreshold is not null)
        {
            _observedEditThreshold.PropertyChanged -= OnEditThresholdChanged;
        }

        _observedEditThreshold = EditData.Threshold;
        _observedEditThreshold.PropertyChanged += OnEditThresholdChanged;
        EnsureDraftSelection();
        OnPropertyChanged(nameof(ThresholdFormTitle));
    }

    private void OnEditThresholdChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Threshold.TargetType))
        {
            EnsureDraftSelection();
        }

        if (e.PropertyName == nameof(Threshold.Id))
        {
            OnPropertyChanged(nameof(ThresholdFormTitle));
        }
    }
}
