/**
 * Formats a date string to locale string
 * @param {string} dateString - ISO date string
 * @returns {string} Formatted date or '-' if invalid
 */
export const formatDate = (dateString) => {
  if (!dateString) return '-';
  return new Date(dateString).toLocaleString();
};
