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

            if (!await cloudBlockBlob.ExistsAsync())
            {
                var blockId = GetBase64Encoded("1");
                var sourceUri = new Uri(sourceUrl);

                cloudBlockBlob.PutBlock(blockId, sourceUri, 0, null, Checksum.None);
                await cloudBlockBlob.PutBlockListAsync(new List<string> {blockId});
            }

            if (metadata != null)
            {
                cloudBlockBlob.Metadata.Clear();
                foreach (var property in metadata)
                {
                    cloudBlockBlob.Metadata.Add(property);
                }
            }

            try
            {
                await cloudBlockBlob.SetMetadataAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var sas = GetContainerSasUri(cloudBlobContainer);
            return new Uri(cloudBlockBlob.Uri + sas);
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