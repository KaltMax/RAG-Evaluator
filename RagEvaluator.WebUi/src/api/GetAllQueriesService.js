import axiosInstance from './axiosConfig';

/**
 * Fetches all queries from the backend
 * @returns {Promise<Array>} Array of query objects
 */
export const getAllQueries = async () => {
  try {
    const response = await axiosInstance.get('/query/history');
    return response.data;
  } catch (error) {
    console.error('Error fetching queries:', error);

    if (error.response) {
      const errorMessage =
        error.response.data?.message ||
        error.response.data?.error ||
        `Server error: ${error.response.status}`;
      throw new Error(errorMessage);
    } else if (error.request) {
      throw new Error('Network error: Unable to reach the server');
    } else {
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }        
}