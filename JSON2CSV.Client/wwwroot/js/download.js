/**
 * Downloads a file to the user's browser
 * @param {string} filename - Name of the file to download
 * @param {string} content - Content of the file
 */
window.downloadFile = function(filename, content) {
    try {
        // Create a Blob with the content
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });

        // Create a temporary URL for the blob
        const url = URL.createObjectURL(blob);

        // Create a temporary anchor element
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.display = 'none';

        // Append to body, click, and remove
        document.body.appendChild(link);
        link.click();

        // Clean up
        setTimeout(() => {
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }, 100);

        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};
