using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace VexTrainerWeb.Pages;

public class ContactModel : BasePage
{
    private readonly IConfiguration _configuration;

    // Override to allow public access - no authentication required
    protected override bool RequiresAuthentication { get { return false; } }

    public ContactModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public string? Category { get; set; }

    [BindProperty]
    public string? Message { get; set; }

    [BindProperty]
    public string? Website { get; set; }  // Honeypot field

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    private DateTime? LastSubmissionTime
    {
        get
        {
            if (HttpContext.Session.GetString("LastContactSubmission") is string value &&
                DateTime.TryParse(value, out DateTime time))
            {
                return time;
            }
            return null;
        }
        set
        {
            if (value.HasValue)
            {
                HttpContext.Session.SetString("LastContactSubmission", value.Value.ToString("O"));
            }
        }
    }

    public void OnGet(string? module = null, string? lesson = null, string? topic = null)
    {
        // When arriving from a lesson page feedback button, pre-fill context
        if (module != null || lesson != null || topic != null)
        {
            Category = "Correction";
            var lines = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(module))  lines.Add($"Module:  {module}");
            if (!string.IsNullOrEmpty(lesson))  lines.Add($"Lesson:  {lesson}");
            if (!string.IsNullOrEmpty(topic))   lines.Add($"Topic:   {topic}");
            lines.Add("");
            lines.Add("Correction or suggestion:");
            lines.Add("");
            Message = string.Join(Environment.NewLine, lines);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Anti-spam measure 1: Honeypot field check
        if (!string.IsNullOrEmpty(Website))
        {
            // Honeypot filled - likely a bot
            Console.WriteLine("Contact form spam attempt: Honeypot field filled");
            // Show success message to bot (don't reveal anti-spam)
            SuccessMessage = "Thank you for your message. We'll review it shortly.";
            return Page();
        }

        // Anti-spam measure 2: Rate limiting (10 seconds between submissions)
        var lastSubmission = LastSubmissionTime;
        if (lastSubmission.HasValue)
        {
            var timeSinceLastSubmission = DateTime.UtcNow - lastSubmission.Value;
            if (timeSinceLastSubmission.TotalSeconds < 10)
            {
                ErrorMessage = "Please wait a moment before submitting another message.";
                return Page();
            }
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "Please select a category.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            ErrorMessage = "Please enter your message.";
            return Page();
        }

        if (Message.Length > 2000)
        {
            ErrorMessage = "Message is too long. Please limit to 2000 characters.";
            return Page();
        }

        if (Message.Trim().Length < 10)
        {
            ErrorMessage = "Message is too short. Please provide more detail.";
            return Page();
        }

        try
        {
            // Get user info from session/claims if authenticated (use inherited IsAuthenticated)
            string userEmail = "anonymous@vextrainer.com";
            string userName = "Anonymous User";

            if (IsAuthenticated)
            {
                // Get email from session or claims
                userEmail = HttpContext.Session.GetString("UserEmail") 
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? "user@vextrainer.com";
                
                // Get name from session or claims
                userName = HttpContext.Session.GetString("UserName") 
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                    ?? "User";
            }

            // Send email to admin
            await SendContactEmailAsync(Category, Message, userEmail, userName);

            // Update last submission time
            LastSubmissionTime = DateTime.UtcNow;

            // Clear form
            Category = null;
            Message = null;

            SuccessMessage = "Thank you for your message! We'll review it and respond if needed.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending contact form: {ex.Message}");
            ErrorMessage = "There was an error sending your message. Please try again later.";
        }

        return Page();
    }

    private async Task SendContactEmailAsync(string category, string message, string userEmail, string userName)
    {
        // Get email configuration from appsettings.json
        // Key names match appsettings.json Email section
        var smtpHost     = _configuration["Email:SmtpServer"];
        var smtpPort     = int.Parse(_configuration["Email:SmtpPort"] ?? "25");
        var enableSsl    = bool.Parse(_configuration["Email:EnableSsl"] ?? "false");
        var fromEmail    = _configuration["Email:FromEmail"];
        var fromPassword = _configuration["Email:FromPassword"];
        var fromName     = _configuration["Email:FromName"] ?? "VexTrainer";
        var toEmail      = _configuration["Email:FeedbackRecipient"] ?? fromEmail;

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(fromEmail))
        {
            Console.WriteLine("Email configuration not found. Logging message to console:");
            Console.WriteLine($"Category: {category}");
            Console.WriteLine($"From: {userName} ({userEmail})");
            Console.WriteLine($"Message: {message}");
            return;
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = $"VexTrainer Contact Form - {category} | from {userEmail}",
            Body = $@"
New contact form submission:

Category: {category}
From: {userName}
Email: {userEmail}
Submitted: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

Message:
{message}
",
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail!);

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(fromEmail, fromPassword)
        };

        await smtpClient.SendMailAsync(mailMessage);
    }
}
