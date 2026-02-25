using Database.DTO;

namespace Database.Manager;

using System;
using System.Collections.Generic;

public interface IDatabaseManager : IDisposable
{
    void EnsureDatabase();
    
    /* -------------------- USERS -------------------- */

    int InsertUser(UserDto user);
    UserDto? GetUser(int userId);

    /* -------------------- SETTINGS -------------------- */

    int InsertSettings(SettingsDto settings);
    SettingsDto? GetSettings(int userId);

    /* -------------------- CATEGORIES -------------------- */

    int InsertCategory(CategoryDto category);
    CategoryDto? GetCategory(int categoryId);
    IEnumerable<CategoryDto> GetAllCategories();

    /* -------------------- APPLICATIONS -------------------- */

    int InsertApplication(ApplicationDto app);
    IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps);
    ApplicationDto? GetApplication(int appId);
    IEnumerable<ApplicationDto> GetApplicationsByCategory(int categoryId);
    IEnumerable<ApplicationDto> GetAllApplications();


    /* -------------------- SESSIONS -------------------- */

    int InsertSession(SessionDto session);
    SessionDto? GetSession(int sessionId);
    IEnumerable<SessionDto> GetSessionsForUser(int userId);

    /* -------------------- BROWSER ACTIVITY -------------------- */

    void InsertBrowserActivity(BrowserActivityDto activity);
    IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId);

    /* -------------------- THRESHOLDS -------------------- */

    int InsertThreshold(ThresholdDto threshold);
    ThresholdDto? GetThreshold(int userId, int categoryId);

    /* -------------------- INTERVENTIONS -------------------- */

    int InsertIntervention(InterventionDto intervention);
    IEnumerable<InterventionDto> GetInterventionsForUser(int userId);

    /* -------------------- REPORTS -------------------- */

    void InsertAggregatedReport(ReportAggregatedDto report);
    IEnumerable<ReportAggregatedDto> GetReports(
        int userId,
        string periodType,
        DateTime periodStart,
        DateTime periodEnd
    );
}
