using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Shared.Protocol;

namespace Orneholm.NewsSearch
{
    public class StorageTransfer
    {
        private readonly CloudBlobClient _cloudBlobClient;

        public StorageTransfer(string storageConnectionString)
        {
            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            }
        }

        public async Task<Dictionary<string, Uri>> TransferBlockBlobs(string cloudBlobContainerName, List<TransferBlob> blobs)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            var uris = new Dictionary<string, Uri>();
            foreach (var blob in blobs)
            {
                var uri = await TransferBlockBlobIfNotExists(cloudBlobContainer, blob.TargetBlobIdentifier, blob.SourceUrl, blob.TargetBlobMetadata);
                uris.Add(blob.TargetBlobIdentifier, uri);
            }
            return uris;
        }

        public async Task<Uri> TransferBlockBlobIfNotExists(string cloudBlobContainerName, string targetBlobName, string sourceUrl, Dictionary<string, string> metadata = null)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            return await TransferBlockBlobIfNotExists(cloudBlobContainer, targetBlobName, sourceUrl, metadata);
        }

        public static async Task<Uri> TransferBlockBlobIfNotExists(CloudBlobContainer cloudBlobContainer, string targetBlobName, string sourceUrl, Dictionary<string, string> metadata = null)
        {
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(targetBlobName);

            Console.WriteLine($"Transfering {sourceUrl} to {targetBlobName}..");

            if (!await cloudBlockBlob.ExistsAsync())
            {
                var blockId = GetBase64Encoded("1");
                var sourceUri = new Uri(sourceUrl);

                cloudBlockBlob.PutBlock(blockId, sourceUri, 0, null, Checksum.None);
                await cloudBlockBlob.PutBlockListAsync(new List<string> { blockId });

                Console.WriteLine($"Transfered {sourceUrl} to {targetBlobName}!");
            }
            else
            {
                Console.WriteLine($"Blob already existed: {targetBlobName}");
            }


            if (metadata != null)
            {
                await cloudBlockBlob.FetchAttributesAsync();
                foreach (var property in metadata)
                {
                    cloudBlockBlob.Metadata[property.Key] = property.Value;
                }

                try
                {
                    Console.WriteLine($"Updating metadata for {targetBlobName}...");
                    await cloudBlockBlob.SetMetadataAsync();
                    Console.WriteLine($"Updated metadata for {targetBlobName}!");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var sas = GetContainerSasUri(cloudBlobContainer);
            return new Uri(cloudBlockBlob.Uri + sas);
        }

        public async Task SetMetadata(string containerName, string blobName, Dictionary<string, string> metadata)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();
            foreach (var property in metadata)
            {
                cloudBlockBlob.Metadata[property.Key] = property.Value;
            }
            await cloudBlockBlob.SetMetadataAsync();
        }

        public async Task SetMetadataValue(string containerName, string blobName, string key, string value)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();
            cloudBlockBlob.Metadata[key] = value;
            await cloudBlockBlob.SetMetadataAsync();
        }

        public async Task<IDictionary<string, string>> GetMetadataValues(string containerName, string blobName)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();

            return cloudBlockBlob.Metadata;
        }

        private static string GetBase64Encoded(string text)
        {
            var encodedBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(encodedBytes);
        }

        private static string GetContainerSasUri(CloudBlobContainer container)
        {
            return container.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTime.UtcNow.AddHours(-3),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read
            }, null);
        }
    }
}