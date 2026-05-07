using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Services;
using VexTrainer.Data.Models;

namespace VexTrainerWeb.Pages;

public class LessonsModel : BasePage
{
    public LessonsModel(AuthService authService, LessonService lessonService)
        : base(authService, lessonService)
    {
    }

    public List<ModuleWithLessons>? Modules { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check authentication using BasePage property
        if (!IsAuthenticated)
        {
            return RedirectToPage("/Auth/SignIn");
        }

        try
        {
            // Get all lessons with progress using CurrentUserId from BasePage
            var result = await _lessonService!.GetAllLessonsAsync(CurrentUserId);

            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return Page();
            }

            // Group lessons by module and load topics for each lesson
            Modules = new List<ModuleWithLessons>();

            var moduleGroups = result.Data!
                .GroupBy(l => new { l.ModuleId, l.ModuleName })
                .OrderBy(g => g.Key.ModuleId);

            foreach (var moduleGroup in moduleGroups)
            {
                var module = new ModuleWithLessons
                {
                    ModuleId = moduleGroup.Key.ModuleId,
                    ModuleName = moduleGroup.Key.ModuleName,
                    Lessons = new List<LessonWithTopics>()
                };

                foreach (var lesson in moduleGroup.OrderBy(l => l.LessonId))
                {
                    // Get topics for this lesson
                    var topicsResult = await _lessonService!.GetTopicsByLessonAsync(lesson.LessonId, CurrentUserId);
                    
                    var lessonWithTopics = new LessonWithTopics
                    {
                        LessonId = lesson.LessonId,
                        LessonTitle = lesson.LessonTitle,
                        FileName = lesson.FileName,
                        IsRead = lesson.IsRead,
                        TopicsRead = (short)lesson.TopicsRead,  // Explicit cast
                        TotalTopics = (short)lesson.TotalTopics,  // Explicit cast
                        Topics = new List<TopicHierarchy>()
                    };

                    if (topicsResult.Success && topicsResult.Data != null)
                    {
                        // Build topic hierarchy (H3 topics with H4 sub-topics)
                        var h3Topics = topicsResult.Data.Where(t => t.HeadingLevel == 3).OrderBy(t => t.DisplayOrder);

                        foreach (var h3Topic in h3Topics)
                        {
                            var topicHierarchy = new TopicHierarchy
                            {
                                TopicId = h3Topic.TopicId,
                                TopicTitle = h3Topic.TopicTitle,
                                IsRead = h3Topic.IsRead,
                                SubTopics = new List<SubTopicInfo>()
                            };

                            // Find H4 sub-topics for this H3
                            var h4SubTopics = topicsResult.Data
                                .Where(t => t.HeadingLevel == 4 && t.ParentTopicId == h3Topic.TopicId)
                                .OrderBy(t => t.DisplayOrder);

                            foreach (var h4Topic in h4SubTopics)
                            {
                                topicHierarchy.SubTopics.Add(new SubTopicInfo
                                {
                                    TopicId = h4Topic.TopicId,
                                    SubTopicTitle = h4Topic.TopicTitle,
                                    IsRead = h4Topic.IsRead
                                });
                            }

                            lessonWithTopics.Topics.Add(topicHierarchy);
                        }
                    }

                    module.Lessons.Add(lessonWithTopics);
                }

                Modules.Add(module);
            }

            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading lessons: {ex.Message}");
            ErrorMessage = "An error occurred while loading lessons. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Get lesson status based on topics read
    /// </summary>
    public string GetLessonStatus(LessonWithTopics lesson)
    {
        // If lesson is marked as read, it's completed
        if (lesson.IsRead)
            return "Completed";

        // If lesson has no topics, just check IsRead
        if (lesson.TotalTopics == 0)
            return lesson.IsRead ? "Completed" : "Unread";

        // Check topic progress - if ALL topics read, consider it completed
        if (lesson.TotalTopics > 0 && lesson.TopicsRead >= lesson.TotalTopics)
            return "Completed";
        else if (lesson.TopicsRead > 0)
            return "Partial";
        else
            return "Unread";
    }

    /// <summary>
    /// Get module status based on lessons
    /// </summary>
    public string GetModuleStatus(short moduleId)
    {
        var module = Modules?.FirstOrDefault(m => m.ModuleId == moduleId);
        if (module == null || !module.Lessons.Any())
            return "Unread";

        var completedCount = module.Lessons.Count(l => GetLessonStatus(l) == "Completed");
        var partialCount = module.Lessons.Count(l => GetLessonStatus(l) == "Partial");

        // All lessons completed
        if (completedCount == module.Lessons.Count)
            return "Completed";

        // Some lessons read or in progress
        if (completedCount > 0 || partialCount > 0)
            return "Partial";

        // No lessons started
        return "Unread";
    }
}

/// <summary>
/// Module with grouped lessons
/// </summary>
public class ModuleWithLessons
{
    public short ModuleId { get; set; }
    public string ModuleName { get; set; } = "";
    public List<LessonWithTopics> Lessons { get; set; } = new();
}

/// <summary>
/// Lesson with topics hierarchy
/// </summary>
public class LessonWithTopics
{
    public short LessonId { get; set; }
    public string LessonTitle { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool IsRead { get; set; }
    public short TopicsRead { get; set; }
    public short TotalTopics { get; set; }
    public List<TopicHierarchy> Topics { get; set; } = new();
}

/// <summary>
/// Topic with sub-topics
/// </summary>
public class TopicHierarchy
{
    public int TopicId { get; set; }
    public string TopicTitle { get; set; } = "";
    public bool IsRead { get; set; }
    public List<SubTopicInfo> SubTopics { get; set; } = new();
}

/// <summary>
/// Sub-topic (H4)
/// </summary>
public class SubTopicInfo
{
    public int TopicId { get; set; }
    public string SubTopicTitle { get; set; } = "";
    public bool IsRead { get; set; }
}
