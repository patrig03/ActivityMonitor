using Backend.Interventions.Models;

namespace ActivityMonitor.ViewModels;

public partial class InterventionsViewModel
{
    private void AddThreshold()
    {
        ResetEditData();
        IsScrollViewerVisible = true;
    }

    private void SaveThreshold()
    {
        SyncDraftSelection();
        EditData.Threshold.UserId = 1;

        var thresholdId = _manager.UpsertThreshold(EditData.Threshold.ToDto());
        if (EditData.Threshold.Id == 0)
        {
            EditData.Threshold.Id = thresholdId;
        }

        RefreshCollections();
        CancelEditing();
    }

    private void DeleteThreshold(object? parameter)
    {
        if (parameter is not ThresholdRow row)
        {
            return;
        }

        _manager.DeleteThreshold(row.Threshold.ToDto());
        RefreshCollections();

        if (EditData.Threshold.Id == row.Threshold.Id)
        {
            CancelEditing();
        }
    }

    private void EditThreshold(object? parameter)
    {
        if (parameter is not ThresholdRow row)
        {
            return;
        }

        EditData = CreateDraft(row.Threshold.Clone());
        IsScrollViewerVisible = true;
    }

    private void CancelEditing()
    {
        IsScrollViewerVisible = false;
        ResetEditData();
    }
}
