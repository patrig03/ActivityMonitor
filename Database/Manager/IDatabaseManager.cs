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
    int UpdateSettings(SettingsDto settings);
    SettingsDto? GetSettings(int userId);

    /* -------------------- CATEGORIES -------------------- */

    int InsertCategory(CategoryDto category);
    CategoryDto? GetCategory(int categoryId);
    IEnumerable<CategoryDto> GetAllCategories();

    /* -------------------- APPLICATIONS -------------------- */

    int InsertApplication(ApplicationDto app);
    int UpsertApplication(ApplicationDto app);
    int? UpdateApplication(ApplicationDto app);
    IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps);
    ApplicationDto? GetApplication(int appId);
    IEnumerable<ApplicationDto> GetApplicationsByCategory(int categoryId);
    IEnumerable<ApplicationDto> GetAllApplications();
    int? IsInDb(ApplicationDto applicationDto);


    /* -------------------- SESSIONS -------------------- */

    int? IsInDb(SessionDto s);
    int InsertSession(SessionDto session);
    int? UpdateSession(SessionDto s);
    int UpsertSession(SessionDto s);
    SessionDto? GetSession(int sessionId);
    IEnumerable<SessionDto> GetSessionsForUser(int userId);
    IEnumerable<SessionDto> GetSessionsByCategory(int categoryId);
    int GetSessionDurationForCategory(int categoryId);

    /* -------------------- BROWSER ACTIVITY -------------------- */

    void InsertBrowserActivity(BrowserActivityDto activity);
    IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId);
    IEnumerable<BrowserActivityDto> GetAllBrowserActivity();
    int? IsInDb(BrowserActivityDto b);

    /* -------------------- THRESHOLDS -------------------- */

    int InsertThreshold(ThresholdDto threshold);
    ThresholdDto? GetThreshold(int userId, int categoryId);
    IEnumerable<ThresholdDto?> GetAllThresholds();
    void DeleteThreshold(ThresholdDto threshold);
    int UpdateThreshold(ThresholdDto threshold);
    int UpsertThreshold(ThresholdDto threshold);


    /* -------------------- INTERVENTIONS -------------------- */

    int InsertIntervention(InterventionDto intervention);
    IEnumerable<InterventionDto> GetInterventionsForUser(int userId);

}
