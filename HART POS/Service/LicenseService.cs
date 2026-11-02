using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Win32;
using System.Xml.Linq;

namespace HARTPOS.Service
{
    public static class LicenseService
    {
        private const string ProductName = "HART POS";
        private const int SchemaVersion = 1;
        private const int MaximumLicenseFileBytes = 16 * 1024;
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>yO4/1soi0MytcS5YZ0N15kpJihoy4MHl7lgK3yLwu9ByB/b61UwgBOTnuCxpUjuuO9yOehoBFbb9xpS8Mwd8oqkhCn+ksKG7BzQ8iiDVq9OXRx3rBuWnUboVheZfEgji3VxlPJee6DJ3gO/msVIgYR6V7TJK3Xp90EXYOa8PrCjOvSUtO3PQDy+pvYS/YonHgGsBwtx6tQvHXU5Bds0b16pNW24NBeWMSFmf1s78VtUFfF6b0A7hgg85F2WwZBKyp8s98tFsAL1IUGrMtr1X1rpQ+dg437ivqj7AZJDqKEltZbPF0BLXrZSue2EMMCcPxLbQLq4+ucnX9EvkLTdmkbnmOB3x5yQxVSpoEkLR5Q3tq61/YJldiwe/+b22YiNmnC05IxpleBGrFlJ8TNd4D+w0LjiNfxm6HEsAuaO0NJlj80EeTtc2s1ZM/jZEcI8UiMib1IRAfG8f8q3vIekm++YcHcthOJ7skWAFZotAdOnIsv3Nx4gGDxvw6A+jr65B</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly object InstallLock = new object();

        public static LicenseStatus GetStatus()
        {
            string installationId;
            string error;
            if (!TryGetInstallationId(out installationId, out error))
                return LicenseStatus.Invalid(string.Empty, "Could not read the IIS server identity: " + error);

            string licensePath;
            try
            {
                licensePath = GetLicensePath();
            }
            catch (Exception ex) when (IsFileAccessException(ex) || ex is InvalidOperationException)
            {
                return LicenseStatus.Invalid(installationId, "The license folder is not configured correctly.");
            }

            try
            {
                return ValidateLicense(ReadBoundedFile(licensePath), installationId);
            }
            catch (FileNotFoundException)
            {
                return LicenseStatus.Invalid(installationId, "No license file is installed for this POS server.");
            }
            catch (DirectoryNotFoundException)
            {
                return LicenseStatus.Invalid(installationId, "No license file is installed for this POS server.");
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException)
            {
                return LicenseStatus.Invalid(installationId, "IIS cannot read the license folder. Grant the application pool read access.");
            }
            catch (Exception ex) when (IsFileAccessException(ex) || ex is InvalidDataException ||
                                      ex is DecoderFallbackException || ex is JsonException ||
                                      ex is CryptographicException)
            {
                return LicenseStatus.Invalid(installationId, "The installed license could not be read or verified.");
            }
        }

        public static LicenseStatus InstallLicense(Stream licenseStream)
        {
            if (licenseStream == null)
                return LicenseStatus.Invalid(string.Empty, "Choose a license file to install.");

            string installationId;
            string error;
            if (!TryGetInstallationId(out installationId, out error))
                return LicenseStatus.Invalid(string.Empty, "Could not read the IIS server identity: " + error);

            byte[] licenseBytes;
            try
            {
                licenseBytes = ReadBoundedStream(licenseStream);
            }
            catch (InvalidDataException ex)
            {
                return LicenseStatus.Invalid(installationId, ex.Message);
            }

            LicenseStatus validation = ValidateLicense(licenseBytes, installationId);
            if (!validation.IsValid)
                return validation;

            string licensePath;
            try
            {
                licensePath = GetLicensePath();
                InstallLicenseFile(licensePath, licenseBytes);
            }
            catch (Exception ex) when (IsFileAccessException(ex) || ex is InvalidOperationException)
            {
                return LicenseStatus.Invalid(
                    installationId,
                    "The license is valid, but IIS could not save it. Grant the application pool access to the license folder and try again.");
            }

            return validation;
        }

        public static bool TryGetInstallationId(out string installationId, out string error)
        {
            installationId = string.Empty;
            error = null;

            try
            {
                RegistryView view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
                using (RegistryKey machineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (RegistryKey cryptoKey = machineKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false))
                {
                    if (cryptoKey == null)
                    {
                        error = "The Windows machine identity registry key is unavailable.";
                        return false;
                    }

                    string machineGuid = cryptoKey.GetValue("MachineGuid") as string;
                    if (string.IsNullOrWhiteSpace(machineGuid))
                    {
                        error = "The Windows machine identity is unavailable.";
                        return false;
                    }

                    byte[] input = Encoding.UTF8.GetBytes(ProductName + "|" + machineGuid.Trim().ToLowerInvariant());
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] digest = sha256.ComputeHash(input);
                        var result = new StringBuilder(digest.Length * 2);
                        foreach (byte value in digest)
                            result.Append(value.ToString("X2", CultureInfo.InvariantCulture));

                        installationId = result.ToString();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException ||
                                      ex is IOException || ex is PlatformNotSupportedException)
            {
                error = "Windows did not allow the application pool to read the machine identity.";
                return false;
            }
        }

        private static LicenseStatus ValidateLicense(byte[] licenseBytes, string installationId)
        {
            if (licenseBytes == null || licenseBytes.Length == 0 || licenseBytes.Length > MaximumLicenseFileBytes)
                return LicenseStatus.Invalid(installationId, "The license file is empty or exceeds the 16 KB limit.");

            try
            {
                string envelopeJson = StrictUtf8.GetString(licenseBytes);
                LicenseEnvelope envelope = JsonConvert.DeserializeObject<LicenseEnvelope>(envelopeJson);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.Payload) ||
                    string.IsNullOrWhiteSpace(envelope.Signature))
                {
                    return LicenseStatus.Invalid(installationId, "The license file has an invalid format.");
                }

                byte[] payloadBytes = Convert.FromBase64String(envelope.Payload);
                byte[] signature = Convert.FromBase64String(envelope.Signature);
                if (payloadBytes.Length == 0 || payloadBytes.Length > MaximumLicenseFileBytes)
                    return LicenseStatus.Invalid(installationId, "The license payload has an invalid size.");

                if (!VerifySignature(payloadBytes, signature))
                    return LicenseStatus.Invalid(installationId, "The license signature is invalid.");

                using (var payloadReader = new StringReader(StrictUtf8.GetString(payloadBytes)))
                using (var jsonReader = new JsonTextReader(payloadReader)
                {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = 8
                })
                {
                    JObject payload = JObject.Load(jsonReader);
                    return ValidatePayload(payload, installationId);
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is FormatException ||
                                      ex is DecoderFallbackException || ex is ArgumentException ||
                                      ex is InvalidOperationException || ex is CryptographicException)
            {
                return LicenseStatus.Invalid(installationId, "The license file is malformed or could not be verified.");
            }
        }

        private static LicenseStatus ValidatePayload(JObject payload, string installationId)
        {
            JToken schemaVersion = payload["schemaVersion"];
            if (schemaVersion == null || schemaVersion.Type != JTokenType.Integer ||
                schemaVersion.Value<int>() != SchemaVersion)
            {
                return LicenseStatus.Invalid(installationId, "The license format is not supported.");
            }

            string product = ReadString(payload, "product");
            string customerName = ReadString(payload, "customerName");
            string licenseId = ReadString(payload, "licenseId");
            string licensedInstallationId = ReadString(payload, "installationId");
            string issuedUtc = ReadString(payload, "issuedUtc");

            if (!string.Equals(product, ProductName, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(licenseId) ||
                string.IsNullOrWhiteSpace(issuedUtc))
            {
                return LicenseStatus.Invalid(installationId, "The license is missing required product or customer information.");
            }

            if (!string.Equals(licensedInstallationId, installationId, StringComparison.OrdinalIgnoreCase))
                return LicenseStatus.Invalid(installationId, "This license was issued for a different POS server.");

            DateTime issuedDate;
            if (!DateTime.TryParseExact(
                issuedUtc,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out issuedDate) || issuedDate.Kind != DateTimeKind.Utc)
            {
                return LicenseStatus.Invalid(installationId, "The license issue date is invalid.");
            }

            JToken expiryToken = payload["expiresOnUtc"];
            DateTime? expiresOnUtc = null;
            if (expiryToken != null && expiryToken.Type != JTokenType.Null)
            {
                if (expiryToken.Type != JTokenType.String)
                    return LicenseStatus.Invalid(installationId, "The license expiry date is invalid.");

                DateTime expiryDate;
                if (!DateTime.TryParseExact(
                    expiryToken.Value<string>(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out expiryDate))
                {
                    return LicenseStatus.Invalid(installationId, "The license expiry date is invalid.");
                }

                expiresOnUtc = expiryDate.Date;
                if (DateTime.UtcNow.Date > expiresOnUtc.Value)
                    return LicenseStatus.Invalid(installationId, "This license has expired.");
            }

            string message = expiresOnUtc.HasValue
                ? "License is valid through " + expiresOnUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " UTC."
                : "Perpetual license is valid.";

            return LicenseStatus.Valid(
                installationId,
                customerName,
                licenseId,
                expiresOnUtc,
                message);
        }

        private static bool VerifySignature(byte[] payloadBytes, byte[] signature)
        {
            RSAParameters publicKey = ReadPublicKey();
            if (signature == null || signature.Length != publicKey.Modulus.Length)
                return false;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(publicKey);
                return rsa.VerifyData(
                    payloadBytes,
                    signature,
                    new HashAlgorithmName("SHA256"),
                    RSASignaturePadding.Pkcs1);
            }
        }

        private static RSAParameters ReadPublicKey()
        {
            XElement root = XDocument.Parse(PublicKeyXml).Element("RSAKeyValue");
            if (root == null)
                throw new CryptographicException("The embedded license public key is invalid.");

            byte[] modulus = Convert.FromBase64String(root.Element("Modulus").Value);
            byte[] exponent = Convert.FromBase64String(root.Element("Exponent").Value);
            if (modulus.Length * 8 != 3072)
                throw new CryptographicException("The embedded license public key must be 3072 bits.");

            return new RSAParameters { Modulus = modulus, Exponent = exponent };
        }

        private static string ReadString(JObject payload, string propertyName)
        {
            JToken token = payload[propertyName];
            return token != null && token.Type == JTokenType.String ? token.Value<string>() : null;
        }

        private static string GetLicensePath()
        {
            string directory = ConfigurationManager.AppSettings["LicenseDirectory"];
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "HARTPOS");
            }

            string fullDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory.Trim()));
            string applicationRoot = HttpRuntime.AppDomainAppPath;
            if (string.IsNullOrWhiteSpace(applicationRoot))
                throw new InvalidOperationException("The ASP.NET application root could not be determined.");

            string fullApplicationRoot = Path.GetFullPath(applicationRoot);
            string applicationRootWithSeparator = fullApplicationRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (string.Equals(
                    fullDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    fullApplicationRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase) ||
                fullDirectory.StartsWith(applicationRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The license folder must be outside the website directory.");
            }

            return Path.Combine(fullDirectory, "license.json");
        }

        private static byte[] ReadBoundedFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return ReadBoundedStream(stream);
        }

        private static byte[] ReadBoundedStream(Stream stream)
        {
            using (var buffer = new MemoryStream())
            {
                byte[] chunk = new byte[4096];
                int read;
                while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (buffer.Length + read > MaximumLicenseFileBytes)
                        throw new InvalidDataException("The license file exceeds the 16 KB size limit.");

                    buffer.Write(chunk, 0, read);
                }

                return buffer.ToArray();
            }
        }

        private static void InstallLicenseFile(string licensePath, byte[] licenseBytes)
        {
            string directory = Path.GetDirectoryName(licensePath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("The license directory is invalid.");

            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory, "license-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(licenseBytes, 0, licenseBytes.Length);
                    stream.Flush(true);
                }

                lock (InstallLock)
                {
                    if (File.Exists(licensePath))
                        File.Replace(temporaryPath, licensePath, null);
                    else
                        File.Move(temporaryPath, licensePath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static bool IsFileAccessException(Exception ex)
        {
            return ex is IOException ||
                   ex is UnauthorizedAccessException ||
                   ex is SecurityException ||
                   ex is ArgumentException ||
                   ex is NotSupportedException;
        }

        private sealed class LicenseEnvelope
        {
            [JsonProperty("payload")]
            public string Payload { get; set; }

            [JsonProperty("signature")]
            public string Signature { get; set; }
        }
    }

    public sealed class LicenseStatus
    {
        private LicenseStatus(
            bool isValid,
            string installationId,
            string customerName,
            string licenseId,
            DateTime? expiresOnUtc,
            string message)
        {
            IsValid = isValid;
            InstallationId = installationId;
            CustomerName = customerName;
            LicenseId = licenseId;
            ExpiresOnUtc = expiresOnUtc;
            Message = message;
        }

        public bool IsValid { get; private set; }
        public string InstallationId { get; private set; }
        public string CustomerName { get; private set; }
        public string LicenseId { get; private set; }
        public DateTime? ExpiresOnUtc { get; private set; }
        public string Message { get; private set; }

        internal static LicenseStatus Invalid(string installationId, string message)
        {
            return new LicenseStatus(false, installationId, null, null, null, message);
        }

        internal static LicenseStatus Valid(
            string installationId,
            string customerName,
            string licenseId,
            DateTime? expiresOnUtc,
            string message)
        {
            return new LicenseStatus(true, installationId, customerName, licenseId, expiresOnUtc, message);
        }
    }
}
