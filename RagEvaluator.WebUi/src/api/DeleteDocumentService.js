import axiosInstance from './axiosConfig';

/**
* Deletes a document by its ID
* @param {string} id - The document ID to delete
* @returns {Promise<void>}
*/
export const deleteDocument = async (id) => {
  try {
    await axiosInstance.delete(`/documents/${id}`);
  } catch (error) {
    console.error('Error deleting document:', error);

    if (error.response) {
      if (error.response.status === 404) {
        throw new Error('Document not found');
      }
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
};
