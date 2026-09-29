using System.Security.Cryptography;

namespace golenWeb.Services
{
    public class FileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;

        // Maximum allowed photo size in bytes (2 MB = 2,097,152 bytes)
        public const long MaxPhotoSizeBytes = 2 * 1024 * 1024; // 2 MB
        public const double MaxPhotoSizeMB = 2.0;

        public FileStorageService(ILogger<FileStorageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates photo size (Max 2MB) and format, then returns the binary data, content type, and SHA256 hash to be stored DIRECTLY inside the database.
        /// </summary>
        public async Task<(byte[]? ImageData, string? ContentType, string? ImageHash, string? ErrorMessage)> ProcessAndStoreImageInDbAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (null, null, null, null);
            }

            // 1. Validate File Size (Maximum 2 MB limit)
            if (file.Length > MaxPhotoSizeBytes)
            {
                double fileSizeMb = Math.Round((double)file.Length / (1024 * 1024), 2);
                string error = $"Photo file size ({fileSizeMb} MB) exceeds the maximum allowed limit of {MaxPhotoSizeMB} MB. Please select a smaller photo.";
                _logger.LogWarning("Photo upload rejected due to size limit: {size} bytes ({mb} MB)", file.Length, fileSizeMb);
                return (null, null, null, error);
            }

            // 2. Validate Image File Extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };
            if (!allowedExtensions.Contains(ext))
            {
                string error = "Invalid photo format. Supported formats: JPG, JPEG, PNG, GIF, WEBP, SVG.";
                _logger.LogWarning("Photo upload rejected due to invalid format: {ext}", ext);
                return (null, null, null, error);
            }

            // 3. Extract photo binary data, determine Content-Type, and calculate Hash
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            string contentType = file.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
            {
                contentType = ext switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    _ => "image/jpeg"
                };
            }

            string imageHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(fileBytes);
                imageHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            _logger.LogInformation("Successfully processed photo for direct database storage. File size: {size} bytes, Hash: {hash}", fileBytes.Length, imageHash);

            return (fileBytes, contentType, imageHash, null);
        }
    }
}
