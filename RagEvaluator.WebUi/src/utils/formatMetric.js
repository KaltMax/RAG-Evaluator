/**
 * Formats a metric value to 3 decimal places
 * @param {number|null|undefined} value - Metric value
 * @returns {string} Formatted value or 'N/A' if invalid
 */
export const formatMetric = (value) => {
  if (value === null || value === undefined) return 'N/A';
  return value.toFixed(3);
};
