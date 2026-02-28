/**
 * Response quality options for evaluation UI
 * Maps to backend ResponseQuality enum (0-3)
 * Ordered from worst to best to match chunk relevance color scheme
 */
export const responseQualityOptions = [
  {
    value: 3,
    label: "Hallucinated",
    shortLabel: "Hallucinated",
    color: "bg-red-600 hover:bg-red-500",
    selectedColor: "bg-red-500 ring-3 ring-red-400",
  },
  {
    value: 2,
    label: "Incorrect",
    shortLabel: "Incorrect",
    color: "bg-orange-600 hover:bg-orange-500",
    selectedColor: "bg-orange-500 ring-3 ring-orange-400",
  },
  {
    value: 1,
    label: "Vague/Incomplete",
    shortLabel: "Vague",
    color: "bg-yellow-600 hover:bg-yellow-500",
    selectedColor: "bg-yellow-500 ring-3 ring-yellow-400",
  },
  {
    value: 0,
    label: "Correct & Complete",
    shortLabel: "Correct",
    color: "bg-green-600 hover:bg-green-500",
    selectedColor: "bg-green-500 ring-3 ring-green-400",
  },
];

/**
 * Get response quality option by value
 * @param {number} value - The response quality value (0-3)
 * @returns {Object|undefined} The response quality option config
 */
export const getResponseQualityOption = (value) => {
  return responseQualityOptions.find((option) => option.value === value);
};

/**
 * Get text color class for response quality value
 * @param {number|null|undefined} value - The response quality value (0-3)
 * @returns {string} Tailwind text color class
 */
export const getResponseQualityColor = (value) => {
  if (value === 0) return "text-green-400";
  if (value === 1) return "text-yellow-400";
  if (value === 2) return "text-orange-400";
  if (value === 3) return "text-red-400";
  return "text-gray-400";
};
