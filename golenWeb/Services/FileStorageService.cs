using System.Security.Cryptography;

namespace golenWeb.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Saves an uploaded image file using a SHA-256 hash of its contents for fast, content-addressable storage & deduplication.
        /// Returns relative URL path e.g. "/uploads/images/a3f89b...jpg"
        /// </summary>
        public async Task<string?> SaveImageAsync(IFormFile? file, string subFolder = "uploads/images")
        {
            if (file == null || file.Length == 0) return null;

            // Validate extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            if (!allowedExtensions.Contains(ext))
            {
                _logger.LogWarning("Rejected file upload with invalid extension: {ext}", ext);
                return null;
            }

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Calculate SHA-256 content hash
            byte[] hashBytes = SHA256.HashData(fileBytes);
            string hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
            string hashedFileName = $"{hashHex}{ext}";

            // Ensure directory exists in wwwroot
            string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string targetDir = Path.Combine(webRootPath, subFolder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string fullPath = Path.Combine(targetDir, hashedFileName);

            // Fast deduplication: save to disk only if not already cached/existing
            if (!File.Exists(fullPath))
            {
                await File.WriteAllBytesAsync(fullPath, fileBytes);
                _logger.LogInformation("Stored new hashed image: {hashedFileName} ({size} bytes)", hashedFileName, fileBytes.Length);
            }
            else
            {
                _logger.LogInformation("Hashed image already exists (deduplicated): {hashedFileName}", hashedFileName);
            }

            // Return relative web URL
            string relativeUrl = $"/{subFolder.Trim('/')}/{hashedFileName}";
            return relativeUrl;
        }
    }
}
