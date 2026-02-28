import axios from "axios";

// Create an Axios instance with default configuration
const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 300000, // 5 minutes - needed for large model loading and inference
});

// Request interceptor for adding auth tokens if needed in the future
axiosInstance.interceptors.request.use(
  (config) => {
    return config;
  },
  (error) => {
    return Promise.reject(error);
  },
);

// Response interceptor for handling errors globally
axiosInstance.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response) {
      // Server responded with error status
      console.error("API Error:", error.response.data);
    } else if (error.request) {
      // Request made but no response received
      console.error("Network Error:", error.message);
    } else {
      // Error in request configuration
      console.error("Error:", error.message);
    }
    return Promise.reject(error);
  },
);

// Utility function to wrap API calls and handle errors consistently
export async function apiRequest(requestFn) {
  try {
    const response = await requestFn();
    return response.data;
  } catch (error) {
    if (error.response) {
      const errorMessage =
        error.response.data?.title || `Server error: ${error.response.status}`;
      throw new Error(errorMessage);
    } else if (error.request) {
      throw new Error("Network error: Unable to reach the server");
    } else {
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }
}

export default axiosInstance;
