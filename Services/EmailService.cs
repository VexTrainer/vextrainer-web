using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VexTrainerWeb.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

  public async Task SendAccountDeletionRequestEmailAsync(string email, string token) {
    var deleteUrl = $"{_configuration["Site:BaseUrl"]}/Auth/DeleteAccount?token={token}";
    var subject = "VexTrainer Account Deletion Request";
    var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2 style='color: #d32f2f;'>Account Deletion Request</h2>
                <p>We received a request to permanently delete your VexTrainer account.</p>
                <p>Click the button below to confirm. <strong>This action cannot be undone.</strong></p>
                <p>
                    <a href='{deleteUrl}'
                       style='background: #d32f2f; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; display: inline-block;'>
                        Delete My Account
                    </a>
                </p>
                <p>Or copy and paste this link:</p>
                <p style='word-break: break-all;'>{deleteUrl}</p>
                <p>This link expires in <strong>24 hours</strong>.</p>
                <p>If you did not request this, safely ignore this email — your account will not be affected.</p>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>VexTrainer - Learn PROS Programming for VEX Robotics</p>
            </body>
            </html>";
    await SendEmailAsync(email, subject, body);
  }

  public async Task SendAccountDeletionCompleteEmailAsync(string email) {
    var subject = "Your VexTrainer Account Has Been Deleted";
    var body = @"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Account Deleted</h2>
                <p>Your VexTrainer account has been permanently deleted.</p>
                <p>All your personal data has been removed from our systems.</p>
                <p>We're sorry to see you go. If you ever want to return, you're welcome to create a new account.</p>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>VexTrainer - Learn PROS Programming for VEX Robotics</p>
            </body>
            </html>";
    await SendEmailAsync(email, subject, body);
  }

  public async Task SendConfirmationEmailAsync(string email, string token)
    {
        var confirmUrl = $"{_configuration["Site:BaseUrl"]}/Auth/Confirm?token={token}";
        var subject = "Confirm Your VexTrainer Account";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to VexTrainer!</h2>
                <p>Please confirm your email address by clicking the link below:</p>
                <p><a href='{confirmUrl}' style='background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>Confirm Email</a></p>
                <p>Or copy and paste this link:</p>
                <p>{confirmUrl}</p>
                <p>This link expires in 24 hours.</p>
                <p>If you didn't create this account, please ignore this email.</p>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>VexTrainer - Learn PROS Programming for VEX Robotics</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var resetUrl = $"{_configuration["Site:BaseUrl"]}/Auth/ResetPassword?token={token}";
        var subject = "Reset Your VexTrainer Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Password Reset Request</h2>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetUrl}' style='background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>Reset Password</a></p>
                <p>Or copy and paste this link:</p>
                <p>{resetUrl}</p>
                <p>This link expires in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>VexTrainer - Learn PROS Programming for VEX Robotics</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["Email:FromName"],
                _configuration["Email:FromEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = htmlBody
            };

            using var client = new SmtpClient();

            // SecureSocketOptions driven by Email:EnableSsl in appsettings.json
            var enableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) && ssl;
            var socketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(
                _configuration["Email:SmtpServer"],
                int.Parse(_configuration["Email:SmtpPort"] ?? "25"),
                socketOptions
            );

            await client.AuthenticateAsync(
                _configuration["Email:FromEmail"],
                _configuration["Email:FromPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email sent successfully to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            throw;
        }
    }
}
