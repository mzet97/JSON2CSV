// JSON2CSV Application JavaScript

// Download a file from byte array
window.downloadFile = function(filename, contentType, data) {
    const blob = new Blob([data], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Initialize tooltips or other client-side features
document.addEventListener('DOMContentLoaded', function() {
    console.log('JSON2CSV Application initialized');
});
