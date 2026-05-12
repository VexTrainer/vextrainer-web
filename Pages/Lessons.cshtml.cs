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
            // Single DB round trip — returns modules, lessons (with progress),
            // and navigable topics (H3/H4 + H2-when-only-content) as three flat
            // result sets. Replaces the old 1+N pattern.
            var result = await _lessonService!.GetAllModulesLessonsTopicsAsync(CurrentUserId);

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message;
                return Page();
            }

            var tree = result.Data;

            // Pre-bucket topics by lesson so the per-lesson loop is O(1) lookups
            // rather than repeated scans.
            var topicsByLesson = tree.Topics
                .GroupBy(t => t.LessonId)
                .ToDictionary(g => g.Key, g => g.ToList());

            Modules = new List<ModuleWithLessons>();

            foreach (var moduleNode in tree.Modules.OrderBy(m => m.DisplayOrder))
            {
                var module = new ModuleWithLessons
                {
                    ModuleId   = moduleNode.ModuleId,
                    ModuleName = moduleNode.ModuleName,
                    Lessons    = new List<LessonWithTopics>()
                };

                var moduleLessons = tree.Lessons
                    .Where(l => l.ModuleId == moduleNode.ModuleId)
                    .OrderBy(l => l.DisplayOrder);

                foreach (var lessonNode in moduleLessons)
                {
                    var lessonWithTopics = new LessonWithTopics
                    {
                        LessonId    = lessonNode.LessonId,
                        LessonTitle = lessonNode.LessonTitle,
                        FileName    = lessonNode.FileName,
                        IsRead      = lessonNode.IsRead,
                        TopicsRead  = (short)lessonNode.TopicsRead,
                        TotalTopics = (short)lessonNode.TotalTopics,
                        Topics      = new List<TopicHierarchy>()
                    };

                    if (topicsByLesson.TryGetValue(lessonNode.LessonId, out var lessonTopics))
                    {
                        // Build topic hierarchy (H3 topics with H4 sub-topics)
                        var h3Topics = lessonTopics
                            .Where(t => t.HeadingLevel == 3)
                            .OrderBy(t => t.DisplayOrder)
                            .ToList();

                        if (h3Topics.Any())
                        {
                            foreach (var h3Topic in h3Topics)
                            {
                                var topicHierarchy = new TopicHierarchy
                                {
                                    TopicId    = h3Topic.TopicId,
                                    TopicTitle = h3Topic.TopicTitle,
                                    IsRead     = h3Topic.IsRead,
                                    SubTopics  = new List<SubTopicInfo>()
                                };

                                var h4SubTopics = lessonTopics
                                    .Where(t => t.HeadingLevel == 4 && t.ParentTopicId == h3Topic.TopicId)
                                    .OrderBy(t => t.DisplayOrder);

                                foreach (var h4Topic in h4SubTopics)
                                {
                                    topicHierarchy.SubTopics.Add(new SubTopicInfo
                                    {
                                        TopicId       = h4Topic.TopicId,
                                        SubTopicTitle = h4Topic.TopicTitle,
                                        IsRead        = h4Topic.IsRead
                                    });
                                }

                                lessonWithTopics.Topics.Add(topicHierarchy);
                            }
                        }
                        else
                        {
                            // Single-page lesson: only an H2 topic exists (no H3 breakdown).
                            // The SP already filters H2 to only appear in this case, so any
                            // H2 we see here is THE content topic for this lesson.
                            var h2Topic = lessonTopics
                                .Where(t => t.HeadingLevel == 2)
                                .OrderBy(t => t.DisplayOrder)
                                .FirstOrDefault();

                            if (h2Topic != null)
                            {
                                lessonWithTopics.SingleTopicId = h2Topic.TopicId;
                            }
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
        var partialCount   = module.Lessons.Count(l => GetLessonStatus(l) == "Partial");

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

    /// <summary>
    /// For single-page lessons (only an H2 topic, no H3 breakdown), this holds the
    /// topic id to link the lesson title directly to its content page.
    /// Null when the lesson has H3 topics (rendered as an accordion instead).
    /// </summary>
    public int? SingleTopicId { get; set; }
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
