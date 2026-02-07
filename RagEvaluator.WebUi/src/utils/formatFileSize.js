/**
 * Format file size in bytes to a human-readable string.
 * @param {number} bytes - File size in bytes
 * @returns {string} Formatted file size (e.g., '1.23 MB')
 */
export const formatFileSize = (bytes) => {
  if (!bytes) return '-';
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};