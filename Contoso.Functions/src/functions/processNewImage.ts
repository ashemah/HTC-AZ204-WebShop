import { app, InvocationContext } from "@azure/functions";
import sharp from "sharp";
import { BlobServiceClient } from "@azure/storage-blob";

// Function: processNewImage
// Resizes the incoming blob (assumed image) to 100x100 and uploads a new blob with prefix 'thumb_'.
export async function processNewImage(blob: Buffer, context: InvocationContext): Promise<void> {
    const blobName: string = (context.triggerMetadata?.name as string) || "unknown";
    context.log(`processNewImage triggered for blob '${blobName}', size=${blob.length} bytes`);

    if (!blob || blob.length === 0) {
        context.log("Received empty blob, skipping.");
        return;
    }

    // Avoid recursive processing of already-generated thumbnails
    if (blobName.toLowerCase().startsWith('thumb_')) {
        context.log(`Blob '${blobName}' already a thumbnail (prefix thumb_), skipping.`);
        return;
    }

    try {
        const lower = blobName.toLowerCase();
        const supported = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".tiff", ".bmp"]; 
        if (!supported.some(ext => lower.endsWith(ext))) {
            context.log(`Blob '${blobName}' not a supported image type. Skipping.`);
            return;
        }

        // Resize image to 100x100 (cover ensures fill, may crop)
        const thumbBuffer = await sharp(blob)
            .resize(100, 100, { fit: 'cover', position: 'center' })
            .toBuffer();
        context.log(`Thumbnail created for '${blobName}' (${thumbBuffer.length} bytes)`);

        const connectionString = process.env["t03storage_STORAGE"]; // Provided by binding configuration
        if (!connectionString) {
            context.log("Environment variable 't03storage_STORAGE' not found. Cannot upload thumbnail.");
            return;
        }

        const containerName = "t03thumbs"; // same container as trigger
        const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
        const containerClient = blobServiceClient.getContainerClient(containerName);
        const thumbName = `thumb_${blobName}`;
        const blockBlobClient = containerClient.getBlockBlobClient(thumbName);

        await blockBlobClient.uploadData(thumbBuffer, {
            blobHTTPHeaders: { blobContentType: inferContentType(blobName) }
        });
        context.log(`Thumbnail uploaded as '${thumbName}'.`);
    } catch (err: any) {
        context.log(`Error generating thumbnail for '${blobName}': ${err?.message || err}`);
        throw err;
    }
}

function inferContentType(name: string): string {
    const lower = name.toLowerCase();
    if (lower.endsWith('.png')) return 'image/png';
    if (lower.endsWith('.webp')) return 'image/webp';
    if (lower.endsWith('.gif')) return 'image/gif';
    if (lower.endsWith('.bmp')) return 'image/bmp';
    if (lower.endsWith('.tiff') || lower.endsWith('.tif')) return 'image/tiff';
    return 'image/jpeg';
}

app.storageBlob('processNewImage', {
    path: 't03container/{name}', // include blob name binding
    connection: 't03storage_STORAGE',
    handler: processNewImage
});
