namespace FemVed.Application.Interfaces;

/// <summary>Abstraction for object storage (Cloudflare R2, S3-compatible). Used to upload images and assets.</summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file to R2 and returns the public URL.
    /// </summary>
    /// <param name="fileName">Destination key/path in the bucket, e.g. "experts/profile-123.jpg".</param>
    /// <param name="contentStream">File content stream.</param>
    /// <param name="contentType">MIME type, e.g. "image/jpeg".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public URL of the uploaded file.</returns>
    Task<string> UploadAsync(string fileName, Stream contentStream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the bucket.
    /// </summary>
    /// <param name="fileName">Key/path of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
