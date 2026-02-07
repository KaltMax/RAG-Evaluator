import axiosInstance from './axiosConfig';

/**
* Uploads a PDF document to the backend for RAG processing
* @param {File} file - The PDF file to upload
* @param {string} language - The document language ('en' or 'de')
* @returns {Promise<Object>} Document response with metadata
*/
export const uploadDocument = async (file, language) => {
  try {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('language', language);

    const response = await axiosInstance.post('/documents/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      // Track upload progress if needed
      onUploadProgress: (progressEvent) => {
        const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
        console.log(`Upload Progress: ${percentCompleted}%`);
      },
    });

    return response.data;
  } catch (error) {
    console.error('Full error object:', error);

    if (error.response) {
      // Server error with response
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);

      const errorMessage =
        error.response.data?.message ||
        error.response.data?.error ||
        error.response.data ||
        `Server error: ${error.response.status} ${error.response.statusText}`;

      throw new Error(typeof errorMessage === 'string' ? errorMessage : JSON.stringify(errorMessage));
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
