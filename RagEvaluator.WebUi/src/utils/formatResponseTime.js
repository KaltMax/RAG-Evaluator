/**
 * Formats response time in milliseconds to human readable format
 * @param {number|null|undefined} ms - Response time in milliseconds
 * @returns {string} Formatted time (e.g., "150ms" or "1.50s") or 'N/A' if invalid
 */
export const formatResponseTime = (ms) => {
  if (ms === null || ms === undefined) return "N/A";
  if (ms < 1000) return `${ms}ms`;
  return `${(ms / 1000).toFixed(2)}s`;
};
