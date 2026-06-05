using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Models;
using VexTrainer.Data.Services;
using VexTrainerWeb.Utilities;

namespace VexTrainerWeb.Pages;

/// <summary>
/// Streak Badge Report — shows the user's reading and quiz activity
/// for their 7 most recent active days, grouped by
/// date → module → lesson → topic (lessons) and flat by date (quizzes).
///
/// Timezone is passed as ?tz=N (minutes to add to UTC to get local time).
/// JavaScript on the page redirects with the browser's offset on first load.
/// </summary>
public class StreakReportModel : BasePage
{
    public StreakReportModel(AuthService authService, LessonService lessonService)
        : base(authService, lessonService)
    {
    }

    protected override bool RequiresAuthentication => true;

    public List<DayActivity>  Days         { get; set; } = new();
    public string?             ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync([FromQuery] int tz = 0)
    {
        // Clamp to valid timezone range (UTC-14 to UTC+14)
        if (tz < -840 || tz > 840) tz = 0;

        try
        {
            var result = await _lessonService!.GetStreakBadgeReportAsync(CurrentUserId, tz);

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message ?? "Failed to load activity report.";
                return Page();
            }

            Days = GroupByDay(result.Data);
            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading streak report: {ex.Message}");
            ErrorMessage = "An error occurred while loading the activity report.";
            return Page();
        }
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static List<DayActivity> GroupByDay(StreakBadgeReport report)
    {
        var topicsByDate  = report.Topics
            .GroupBy(t => t.ReadDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var quizzesByDate = report.Quizzes
            .GroupBy(q => q.AttemptDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allDates = topicsByDate.Keys
            .Union(quizzesByDate.Keys)
            .OrderByDescending(d => d);

        return allDates.Select(date => new DayActivity(
            Date:    date,
            Label:   FormatDateLabel(date),
            Modules: GroupByModule(topicsByDate.GetValueOrDefault(date, new())),
            Quizzes: quizzesByDate.GetValueOrDefault(date, new())
        )).ToList();
    }

    private static List<ModuleGroup> GroupByModule(List<ActivityTopicItem> topics) =>
        topics
            .GroupBy(t => new { t.ModuleId, t.ModuleName })
            .OrderBy(g  => g.Key.ModuleId)
            .Select(mg  => new ModuleGroup(
                ModuleId:   mg.Key.ModuleId,
                ModuleName: mg.Key.ModuleName,
                Lessons:    mg
                    .GroupBy(t => new { t.LessonId, t.LessonTitle })
                    .OrderBy(g  => g.Key.LessonId)
                    .Select(lg  => new LessonGroup(
                        LessonId:    lg.Key.LessonId,
                        LessonTitle: lg.Key.LessonTitle,
                        Topics:      lg.ToList()
                    )).ToList()
            )).ToList();

    private static string FormatDateLabel(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        if (date == today)            return "Today";
        if (date == today.AddDays(-1)) return "Yesterday";
        return date.ToString("MMMM d, yyyy");
    }
}

// ── Grouped view models ───────────────────────────────────────────────────────

public record DayActivity(
    DateTime             Date,
    string               Label,
    List<ModuleGroup>    Modules,
    List<ActivityQuizItem> Quizzes
);

public record ModuleGroup(
    int              ModuleId,
    string           ModuleName,
    List<LessonGroup> Lessons
);

public record LessonGroup(
    int                    LessonId,
    string                 LessonTitle,
    List<ActivityTopicItem> Topics
);
