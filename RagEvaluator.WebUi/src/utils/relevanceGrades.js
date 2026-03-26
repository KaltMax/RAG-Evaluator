/**
 * Relevance grade options for annotation UI
 * Maps to backend RelevanceGrade enum (0-1)
 */
export const relevanceGrades = [
  {
    value: 0,
    label: "Not Relevant",
    shortLabel: "Not",
    color: "bg-red-700 hover:bg-red-600",
    selectedColor: "bg-red-600 ring-3 ring-red-400",
  },
  {
    value: 1,
    label: "Relevant",
    shortLabel: "Rel",
    color: "bg-green-600 hover:bg-green-500",
    selectedColor: "bg-green-500 ring-3 ring-green-400",
  },
];

/**
 * Get relevance grade config by value
 * @param {number} value - The relevance grade value (0-1)
 * @returns {Object|undefined} The relevance grade config
 */
export const getRelevanceGrade = (value) => {
  return relevanceGrades.find((grade) => grade.value === value);
};
