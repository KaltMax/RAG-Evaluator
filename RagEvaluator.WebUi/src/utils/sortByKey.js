/**
 * Sorts an array of objects by a specified key and direction.
 * @param {Array} array - The array to sort.
 * @param {string} key - The key to sort by.
 */

export function sortByKey(array, key, direction = 'asc') {
  if (!key) return array;
  return [...array].sort((a, b) => {
    let aVal = a[key] ?? '';
    let bVal = b[key] ?? '';
    if (typeof aVal === 'string') {
      aVal = aVal.toLowerCase();
      bVal = String(bVal).toLowerCase();
    }
    if (aVal < bVal) return direction === 'asc' ? -1 : 1;
    if (aVal > bVal) return direction === 'asc' ? 1 : -1;
    return 0;
  });
}
