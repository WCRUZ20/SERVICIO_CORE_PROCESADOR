using Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.DataProtection
{
    /// <summary>
    /// Implementación de ISecretProtector usando DataProtection de ASP.NET Core
    /// </summary>
    public class DataProtectionSecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;
        private const string Prefix = "enc:";

        public DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("IntegrationWorker.Secrets");
        }

        /// <summary>
        /// Encripta un texto plano y le agrega el prefijo "enc:"
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            try
            {
                var encrypted = _protector.Protect(plainText);
                return $"{Prefix}{encrypted}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al encriptar el texto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Desencripta un texto que tiene el prefijo "enc:"
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                // Si no tiene el prefijo, asumimos que ya está desencriptado
                if (!cipherText.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    return cipherText;

                var encryptedValue = cipherText.Substring(Prefix.Length);
                return _protector.Unprotect(encryptedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al desencriptar el texto: {ex.Message}", ex);
            }
        }
    }
}




