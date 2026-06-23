import axiosInstance, { apiRequest } from "./axiosConfig";

export const getAllDocuments = () =>
  apiRequest(() => axiosInstance.get("/documents"));

export const getDocumentById = (id) =>
  apiRequest(() => axiosInstance.get(`/documents/${id}`));

export const uploadDocument = async (file, language, course) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("language", language);
  formData.append("course", course);

  try {
    const response = await axiosInstance.post("/documents/upload", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(
        error.response.data?.title ||
          `Server error: ${error.response.status} ${error.response.statusText}`,
      );
    } else if (error.request) {
      throw new Error(
        "Network error: Unable to reach the server. Is the backend running?",
      );
    } else {
      throw new Error(`An unexpected error occurred: ${error.message}`);
    }
  }
};

export const downloadDocument = async (id, fileName) => {
  try {
    const response = await axiosInstance.get(`/documents/${id}/download`, {
      responseType: "blob",
    });

    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  } catch (error) {
    console.error("Error downloading document:", error);
    throw new Error("Failed to download document");
  }
};

export const deleteDocument = (id) =>
  apiRequest(() => axiosInstance.delete(`/documents/${id}`));

export const reprocessDocuments = () =>
  apiRequest(() => axiosInstance.post("/documents/reprocess"));

export const reprocessDocumentById = (id) =>
  apiRequest(() => axiosInstance.post(`/documents/${id}/reprocess`));
