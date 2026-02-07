import axiosInstance from "./axiosConfig";

/**
 * Reprocesses all documents with the current chunking and embedding configuration
 * @returns {Promise<Object>} Reprocess result with documentsProcessed, totalChunksCreated, chunkingStrategy, embeddingModel
 */
export const reprocessDocuments = async () => {
  try {
    const response = await axiosInstance.post('/documents/reprocess');
    return response.data;
  } catch (error) {
    console.error('Error reprocessing documents:', error);

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
};
