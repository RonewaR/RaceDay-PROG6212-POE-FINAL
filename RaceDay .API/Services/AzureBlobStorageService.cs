using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay_.API.Services
{
    public class AzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            _blobServiceClient =
                new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadImageAsync(
            IFormFile file,
            string containerName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Please select an image.");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException(
                    "Image size cannot exceed 5 MB.");

            string[] allowedExtensions =
                { ".jpg", ".jpeg", ".png", ".webp" };

            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");

            var containerClient =
                _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None);

            string blobName =
                $"{Guid.NewGuid()}{extension}";

            var blobClient =
                containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}