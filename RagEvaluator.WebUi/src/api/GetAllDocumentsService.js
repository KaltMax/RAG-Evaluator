import axiosInstance from './axiosConfig';

/**
* Fetches all documents from the backend
* @returns {Promise<Array>} Array of document objects
*/
export const getAllDocuments = async () => {
  try {
    const response = await axiosInstance.get('/documents');
    return response.data;
  } catch (error) {
    console.error('Error fetching documents:', error);

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
