import axiosInstance from './axiosConfig';

/**
 * Annotates query results with relevance grades and response quality evaluation, then triggers metrics calculation
 * @param {string} queryId - The ID of the query to annotate
 * @param {Object} payload - Annotation payload containing:
 *   - annotations: Array<{resultId: string, relevanceGrade: number}>
 *   - responseQuality: number (0-3)
 *   - hasLanguageSwitching: boolean
 * @returns {Promise<Object>} Updated query with calculated metrics
 */
export const annotateResults = async (queryId, payload) => {
  try {
    const response = await axiosInstance.patch(`/query/${queryId}/results`, payload);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.error || 'Failed to submit annotations');
    }
    throw new Error('Network error while submitting annotations');
  }
};
