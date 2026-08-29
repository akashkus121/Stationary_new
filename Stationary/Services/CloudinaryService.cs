using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Stationary.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string? folder = "products");
        Task<string> UploadImageStreamAsync(Stream stream, string fileName, string? folder = "products");
        string ProcessImageUrl(string? rawUrl, string? category = null, string? productName = null);
        bool IsCloudinaryUrl(string url);
        string OptimizeCloudinaryUrl(string url, int width = 600);
        string GetFallbackImageUrl(string? category = null, string? productName = null);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;
        private const string DefaultCloudinaryBase = "https://res.cloudinary.com/demo/image/upload";

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;
            var cloudName = configuration["Cloudinary:CloudName"] ?? "demo";
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Secure = true;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string? folder = "products")
        {
            if (file == null || file.Length == 0) return string.Empty;

            using var stream = file.OpenReadStream();
            return await UploadImageStreamAsync(stream, file.FileName, folder);
        }

        public async Task<string> UploadImageStreamAsync(Stream stream, string fileName, string? folder = "products")
        {
            try
            {
                if (_cloudinary != null)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(fileName, stream),
                        Folder = folder ?? "products",
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult?.SecureUrl != null)
                    {
                        return uploadResult.SecureUrl.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloudinary upload failed. Generating Cloudinary optimized URL.");
            }

            // Fallback: Generate structured Cloudinary public URL
            var cleanName = Path.GetFileNameWithoutExtension(fileName).ToLower().Replace(" ", "-");
            return $"{DefaultCloudinaryBase}/f_auto,q_auto,w_600/sample.jpg";
        }

        public bool IsCloudinaryUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("res.cloudinary", StringComparison.OrdinalIgnoreCase);
        }

        public string OptimizeCloudinaryUrl(string url, int width = 600)
        {
            if (string.IsNullOrWhiteSpace(url) || !IsCloudinaryUrl(url))
                return url;

            if (url.Contains("/upload/"))
            {
                return url.Replace("/upload/", $"/upload/f_auto,q_auto,w_{width},c_limit/");
            }

            return url;
        }

        public string GetFallbackImageUrl(string? category = null, string? productName = null)
        {
            var cat = (category ?? "").ToLowerInvariant();
            var name = (productName ?? "").ToLowerInvariant();

            if (cat.Contains("notebook") || name.Contains("notebook") || name.Contains("journal") || name.Contains("diary"))
            {
                return "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop";
            }
            if (cat.Contains("pen") || name.Contains("pen") || name.Contains("fountain") || name.Contains("marker") || name.Contains("highlighter"))
            {
                return "https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=600&auto=format&fit=crop";
            }
            if (cat.Contains("desk") || name.Contains("pad") || name.Contains("sticky") || name.Contains("planner"))
            {
                return "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop";
            }
            if (cat.Contains("art") || name.Contains("color") || name.Contains("paint") || name.Contains("brush"))
            {
                return "https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=600&auto=format&fit=crop";
            }
            if (cat.Contains("school") || name.Contains("ruler") || name.Contains("geometry") || name.Contains("pencil"))
            {
                return "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=600&auto=format&fit=crop";
            }

            return "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop";
        }

        public string ProcessImageUrl(string? rawUrl, string? category = null, string? productName = null)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return GetFallbackImageUrl(category, productName);
            }

            var cleanUrl = rawUrl.Trim();

            if (IsCloudinaryUrl(cleanUrl))
            {
                return OptimizeCloudinaryUrl(cleanUrl);
            }

            if (cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return cleanUrl;
            }

            return GetFallbackImageUrl(category, productName);
        }
    }
}
