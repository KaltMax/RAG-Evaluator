import axiosInstance from './axiosConfig';

/**
* Sends a query to the backend RAG service
* @param {string} question - The question to ask
* @param {number} topK - Number of top results to retrieve (default: 3)
* @param {string} language - Language for the query ('en' or 'de', default: 'en')
* @returns {Promise<Object>} Query response with answer and sources
*/
export const postQuery = async (question, topK = 3, language = 'en') => {
  try {
    const response = await axiosInstance.post('/query', {
      Question: question,
      TopK: topK,
      Language: language,
    });
    return response.data;
  } catch (error) {
    console.error('Full error object:', error);

    if (error.response) {
      // Server error with response
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);

      const errorMessage =
        error.response.data?.title ||
        `Server error: ${error.response.status} ${error.response.statusText}`;

      throw new Error(errorMessage);
    } else if (error.request) {
      // Network error
      console.error('No response received:', error.request);
      throw new Error('Network error: Unable to reach the server. Is the backend running?');
    } else {
      // Other errors
      console.error('Error message:', error.message);
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }
};
