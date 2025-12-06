// Export functionality for Ceiba - US2
// Handles file downloads from Blazor components

/**
 * Downloads a file from base64 encoded data
 * @param {string} base64Data - Base64 encoded file content
 * @param {string} fileName - Name of the file to download
 * @param {string} contentType - MIME type of the file
 */
window.downloadFileFromBase64 = function (base64Data, fileName, contentType) {
    // Convert base64 to blob
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType });

    // Create download link and trigger download
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();

    // Cleanup
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

/**
 * Downloads a file directly from a URL
 * @param {string} url - URL to download from
 * @param {string} fileName - Name of the file to download
 */
window.downloadFileFromUrl = function (url, fileName) {
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
