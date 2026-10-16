using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Globalization;

namespace MPDCtrl.Helpers;

internal static class EncryptDecrypt
{
    public static string Encrypt(string s)
    {
        if (string.IsNullOrEmpty(s)) { return ""; }

        byte[] entropy = [0x72, 0xa2, 0x12, 0x04];

        // Uses System.Security.Cryptography.ProtectedData
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                byte[] userData = System.Text.Encoding.UTF8.GetBytes(s);
                byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(userData, entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);

                return System.Convert.ToBase64String(encryptedData);
            }
            catch
            {
                Debug.WriteLine($"Encrypt fail.");
                return s;
            }
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        else
        {
            string encryptionKey = "withas";
            byte[] sBytes = Encoding.Unicode.GetBytes(s);
            using (System.Security.Cryptography.Aes encryptor = System.Security.Cryptography.Aes.Create())
            {
                // Obsolete and deprecated in .NET 10.
                //Rfc2898DeriveBytes pdb = new(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1);
                //encryptor.Key = pdb.GetBytes(32);
                //encryptor.IV = pdb.GetBytes(16);
                // New since .NET 10.
                encryptor.Key = Rfc2898DeriveBytes.Pbkdf2(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1, 32);
                encryptor.IV = Rfc2898DeriveBytes.Pbkdf2(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1, 16);

                using MemoryStream ms = new();
                using (CryptoStream cs = new(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(sBytes, 0, sBytes.Length);
                    cs.Close();
                }
                s = Convert.ToBase64String(ms.ToArray());
            }
            return s;
        }
    }

    public static string Decrypt(string s)
    {
        if (string.IsNullOrEmpty(s)) { return ""; }

        byte[] entropy = [0x72, 0xa2, 0x12, 0x04];

        // Uses System.Security.Cryptography.ProtectedData
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                byte[] encryptedData = System.Convert.FromBase64String(s);
                byte[] userData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(userData);
            }
            catch
            {
                Debug.WriteLine($"Decrypt fail.");
                return "";
            }
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        else
        {
            string encryptionKey = "withas";
            s = s.Replace(" ", "+");
            byte[] sBytes = Convert.FromBase64String(s);
            using (System.Security.Cryptography.Aes encryptor = System.Security.Cryptography.Aes.Create())
            {
                // Obsolete and deprecated in .NET 10.
                //Rfc2898DeriveBytes pdb = new(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1);
                //encryptor.Key = pdb.GetBytes(32);
                //encryptor.IV = pdb.GetBytes(16);
                // New since .NET 10.
                encryptor.Key = Rfc2898DeriveBytes.Pbkdf2(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1, 32);
                encryptor.IV = Rfc2898DeriveBytes.Pbkdf2(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1, 16);

                using MemoryStream ms = new();
                using (CryptoStream cs = new(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(sBytes, 0, sBytes.Length);
                    cs.Close();
                }
                s = Encoding.Unicode.GetString(ms.ToArray());
            }

            return s;
        }
    }

}

