using System;
using System.Collections.Generic;
using System.IO;

namespace Buy2.Application.Common.Utilities;

public static class NewsMediaValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB boundary

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        ".jpg", ".jpeg", ".png", ".webp", ".gif",
        // Videos
        ".mp4", ".mov", ".webm",
        // Documents
        ".pdf"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "video/mp4", "video/quicktime", "video/webm",
        "application/pdf"
    };

    public static (bool IsValid, string? ErrorMessage) ValidateMedia(long fileSizeBytes, string? fileName, string? contentType = null)
    {
        if (fileSizeBytes <= 0)
        {
            return (false, "Media file cannot be empty.");
        }

        if (fileSizeBytes > MaxFileSizeBytes)
        {
            return (false, $"Media file size exceeds the 10 MB limit (Current: {Math.Round(fileSizeBytes / (1024.0 * 1024.0), 2)} MB).");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return (false, "File name must be specified.");
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            return (false, $"Unsupported media file format '{ext}'. Supported formats: {string.Join(", ", AllowedExtensions)}.");
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedMimeTypes.Contains(contentType))
        {
            return (false, $"Unsupported media content type '{contentType}'.");
        }

        return (true, null);
    }
}
