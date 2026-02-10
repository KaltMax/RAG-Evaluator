import axiosInstance from './axiosConfig';

/**
 * Fetches a single query by ID, including its sources/results
 * @param {string} id - The query ID
 * @returns {Promise<Object>} Full query object with sources
 */
export const getQueryById = async (id) => {
  try {
    const response = await axiosInstance.get(`/query/${id}`);
    return response.data;
  } catch (error) {
    console.error('Error fetching query:', error);

    if (error.response) {
      const errorMessage =
        error.response.data?.title ||
        `Server error: ${error.response.status}`;
      throw new Error(errorMessage);
    } else if (error.request) {
      throw new Error('Network error: Unable to reach the server');
    } else {
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }
};
