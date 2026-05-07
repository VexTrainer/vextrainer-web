using Microsoft.AspNetCore.Mvc;
using VexTrainer.Data.Services;
using VexTrainer.Data.Models;
using VexTrainerWeb.Utilities;

namespace VexTrainerWeb.Pages;

public class LessonModel : BasePage
{
    private readonly IWebHostEnvironment _env;

    public LessonModel(AuthService authService, LessonService lessonService, IWebHostEnvironment env)
        : base(authService, lessonService)
    {
        _env = env;
    }

    // Topic details
    public int TopicId { get; set; }
    public string TopicTitle { get; set; } = "";
    public string FileName { get; set; } = "";
    public string EncodedUrl { get; set; } = "";
    
    // Navigation
    public TopicNavigation? PreviousTopic { get; set; }
    public TopicNavigation? NextTopic { get; set; }
    
    // Breadcrumb
    public short ModuleId { get; set; }
    public string ModuleName { get; set; } = "";
    public short LessonId { get; set; }
    public string LessonTitle { get; set; } = "";
    public string? ParentTopicTitle { get; set; }
    
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string fileName)
    {
        // Check authentication
        if (!IsAuthenticated)
        {
            return RedirectToPage("/Auth/SignIn");
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return RedirectToPage("/Lessons");
        }

        try
        {
            // Try to decode as new encoded format first
            var decoded = TopicUrlEncoder.Decode(fileName);
            
            if (decoded.HasValue)
            {
                // New encoded format
                ModuleId = (short)decoded.Value.moduleId;
                LessonId = (short)decoded.Value.lessonId;
                TopicId = decoded.Value.topicId;
                
                // Generate old-style filename for content loading
                FileName = $"{ModuleId:D5}-{LessonId:D5}-{TopicId:D5}";
                EncodedUrl = fileName;
            }
            else
            {
                // Legacy format: MMMMM-LLLLL-TTTTT
                var parts = fileName.Split('-');
                
                if (parts.Length != 3)
                {
                    ErrorMessage = "Invalid topic identifier";
                    return Page();
                }

                if (!short.TryParse(parts[0], out short moduleId) ||
                    !short.TryParse(parts[1], out short lessonId) ||
                    !int.TryParse(parts[2], out int topicId))
                {
                    ErrorMessage = "Invalid topic identifier";
                    return Page();
                }

                ModuleId = moduleId;
                LessonId = lessonId;
                TopicId = topicId;
                FileName = fileName;
                
                // Generate encoded URL for navigation
                EncodedUrl = TopicUrlEncoder.Encode(moduleId, lessonId, topicId);
            }

            // Get topic details with navigation from database
            var result = await _lessonService!.GetTopicDetailsAsync(TopicId, CurrentUserId);

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message ?? "Topic not found";
                return Page();
            }

            var topic = result.Data;

            // Set topic details
            TopicTitle = topic.TopicTitle;
            ModuleName = topic.ModuleName;
            LessonTitle = topic.LessonTitle;
            ParentTopicTitle = topic.ParentTopicTitle;

            // Set navigation with encoded URLs
            if (topic.PreviousTopicId.HasValue && !string.IsNullOrEmpty(topic.PreviousFileName))
            {
                // Parse previous filename to get IDs
                var prevParts = topic.PreviousFileName.Split('-');
                if (prevParts.Length == 3 &&
                    int.TryParse(prevParts[0], out int prevModId) &&
                    int.TryParse(prevParts[1], out int prevLesId) &&
                    int.TryParse(prevParts[2], out int prevTopId))
                {
                    PreviousTopic = new TopicNavigation
                    {
                        TopicId = topic.PreviousTopicId.Value,
                        TopicTitle = topic.PreviousTopicTitle ?? "",
                        FileName = TopicUrlEncoder.Encode(prevModId, prevLesId, prevTopId)
                    };
                }
            }

            if (topic.NextTopicId.HasValue && !string.IsNullOrEmpty(topic.NextFileName))
            {
                // Parse next filename to get IDs
                var nextParts = topic.NextFileName.Split('-');
                if (nextParts.Length == 3 &&
                    int.TryParse(nextParts[0], out int nextModId) &&
                    int.TryParse(nextParts[1], out int nextLesId) &&
                    int.TryParse(nextParts[2], out int nextTopId))
                {
                    NextTopic = new TopicNavigation
                    {
                        TopicId = topic.NextTopicId.Value,
                        TopicTitle = topic.NextTopicTitle ?? "",
                        FileName = TopicUrlEncoder.Encode(nextModId, nextLesId, nextTopId)
                    };
                }
            }

            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading topic: {ex.Message}");
            ErrorMessage = "An error occurred while loading the topic.";
            return Page();
        }
    }

    // Handler to get markdown content
    public async Task<IActionResult> OnGetContentAsync()
    {
        try
        {
            // Get fileName from route data (this is the ENCODED url)
            var encodedUrl = RouteData.Values["fileName"]?.ToString();
            
            if (string.IsNullOrEmpty(encodedUrl))
            {
                return NotFound("Topic file name not found in route");
            }
            
            // Decode the URL to get the actual IDs
            var decoded = TopicUrlEncoder.Decode(encodedUrl);
            string actualFileName;
            
            if (decoded.HasValue)
            {
                // New encoded format - build the actual filename
                actualFileName = $"{decoded.Value.moduleId:D5}-{decoded.Value.lessonId:D5}-{decoded.Value.topicId:D5}";
            }
            else
            {
                // Legacy format (already in correct format)
                actualFileName = encodedUrl;
            }
            
            // Build file path: /wwwroot/content/lessons/MMMMM-LLLLL-TTTTT.md
            var filePath = Path.Combine(_env.WebRootPath, "content", "lessons", $"{actualFileName}.md");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Topic file not found: {actualFileName}.md");
            }

            var content = await System.IO.File.ReadAllTextAsync(filePath);
            return Content(content, "text/markdown");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content: {ex.Message}");
            return StatusCode(500, "Error loading content");
        }
    }

    // Handler to mark topic as read
    public async Task<IActionResult> OnPostMarkReadAsync([FromBody] MarkTopicReadRequest request)
    {
        try
        {
            if (!IsAuthenticated)
            {
                return new JsonResult(new { success = false, message = "Not authenticated" })
                {
                    StatusCode = 401
                };
            }

            if (request.TopicId <= 0)
            {
                return new JsonResult(new { success = false, message = "Invalid topic ID" })
                {
                    StatusCode = 400
                };
            }

            var result = await _lessonService!.MarkTopicReadAsync(request.TopicId, CurrentUserId);

            return new JsonResult(new
            {
                success = result.Success,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking topic read: {ex.Message}");
            return new JsonResult(new { success = false, message = "Error marking topic as read" })
            {
                StatusCode = 500
            };
        }
    }
}

/// <summary>
/// Topic navigation info (prev/next)
/// </summary>
public class TopicNavigation
{
    public int TopicId { get; set; }
    public string TopicTitle { get; set; } = "";
    public string FileName { get; set; } = "";
}

/// <summary>
/// Request to mark topic as read
/// </summary>
public class MarkTopicReadRequest
{
    public int TopicId { get; set; }
}
