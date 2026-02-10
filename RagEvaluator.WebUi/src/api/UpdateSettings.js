import axiosInstance from "./axiosConfig";

/**
 * Updates the application settings on the backend
 * @param {Object} settings - The settings object to be updated
 * @returns {Promise<Object>} The updated settings object from the backend
 */
export const updateSettings = async (settings) => {
  try {
    const response = await axiosInstance.patch('/settings', settings);
    return response.data;
  } catch (error) {
    console.error('Error updating settings:', error);

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
