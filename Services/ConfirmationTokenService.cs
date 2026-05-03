using System.Security.Cryptography;
using System.Text;

namespace VexTrainerWeb.Services;

public class ConfirmationTokenService {
    //private readonly IConfiguration _configuration;
    private readonly byte[] _key;

  public ConfirmationTokenService(IConfiguration configuration) {
    var secret = configuration.GetConnectionString("DefaultConnection")
              ?? "DefaultSecretKeyForEncryption32";
    _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret))[..32];
  }

  public string GenerateEmailConfirmationToken(string email) {
    var data = $"{email}|confirm|{DateTime.UtcNow.AddHours(24):O}";
    return Encrypt(data);
  }

  public string GeneratePasswordResetToken(string email) {
    var data = $"{email}|reset|{DateTime.UtcNow.AddHours(1):O}";
    return Encrypt(data);
  }

  public (bool isValid, string email) ValidateToken(string token) {
    try {
      var data = Decrypt(token);
      var parts = data.Split('|');

      if (parts.Length != 3) return (false, string.Empty);

      var email = parts[0];
      var expiration = DateTime.Parse(parts[2]);

      if (DateTime.UtcNow > expiration) return (false, string.Empty);

      return (true, email);
    }
    catch {
      return (false, string.Empty);
    }
  }

  private string Encrypt(string plainText) {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string Decrypt(string cipherText) {
        var fullCipher = Convert.FromBase64String(
            cipherText.Replace("-", "+").Replace("_", "/")
            .PadRight(cipherText.Length + (4 - cipherText.Length % 4) % 4, '=')
        );

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
