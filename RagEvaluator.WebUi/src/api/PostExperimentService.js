import axiosInstance from './axiosConfig';

/**
 * Creates a new experiment by sending parsed JSON data to the backend
 * @param {Object} experimentData - The experiment configuration
 * @param {string} experimentData.Name - Experiment name
 * @param {number} experimentData.RepeatCount - Number of repetitions
 * @param {Array} experimentData.Queries - Array of query objects
 * @returns {Promise<Object>} Created experiment response
 */
export const createExperiment = async (experimentData) => {
  try {
    const response = await axiosInstance.post('/experiments', experimentData);
    return response.data;
  } catch (error) {
    console.error('Full error object:', error);

    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);

      const errorMessage =
        error.response.data?.title ||
        `Server error: ${error.response.status} ${error.response.statusText}`;

      throw new Error(errorMessage);
    } else if (error.request) {
      console.error('No response received:', error.request);
      throw new Error('Network error: Unable to reach the server. Is the backend running?');
    } else {
      console.error('Error message:', error.message);
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }
};
