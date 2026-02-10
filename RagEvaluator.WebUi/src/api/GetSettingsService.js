import axiosInstance from "./axiosConfig";

/**
 *  Fetches the current application settings from the backend
 * @returns {Promise<Object>} The current application settings
 */
export const getSettings = async () => {
  try {
    const response = await axiosInstance.get('/settings');
    return response.data;
  } catch (error) {
    console.error('Error fetching settings:', error);

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
