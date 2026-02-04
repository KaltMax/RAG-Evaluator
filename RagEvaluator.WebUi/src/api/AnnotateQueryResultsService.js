import axiosInstance from './axiosConfig';

/**
 * Annotates query results with relevance grades and triggers metrics calculation
 * @param {string} queryId - The ID of the query to annotate
 * @param {Array<{resultId: string, relevanceGrade: number}>} annotations - Array of result annotations
 * @returns {Promise<Object>} Updated query with calculated metrics
 */
export const annotateQueryResults = async (queryId, annotations) => {
  try {
    const response = await axiosInstance.patch(`/query/${queryId}/results`, {
      annotations: annotations,
    });
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.error || 'Failed to submit annotations');
    }
    throw new Error('Network error while submitting annotations');
  }
};
