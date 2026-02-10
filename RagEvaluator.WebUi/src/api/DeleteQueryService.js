import axiosInstance from './axiosConfig';

/**
* Deletes a query by its ID
* @param {string} id - The query ID to delete
* @returns {Promise<void>}
*/
export const deleteQuery = async (id) => {
  try {
    await axiosInstance.delete(`/query/${id}`);
  } catch (error) {
    console.error('Error deleting query:', error);

    if (error.response) {
      if (error.response.status === 404) {
        throw new Error('Query not found');
      }
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
