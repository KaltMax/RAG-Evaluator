/**
 * Formats language code to full language name
 * @param {string|null|undefined} lang - Language code (e.g., 'en', 'de')
 * @returns {string} Full language name or '-' if invalid
 */
export const formatLanguage = (lang) => {
  const languages = {
    en: "English",
    de: "German",
  };
  return languages[lang] || lang || "-";
};
