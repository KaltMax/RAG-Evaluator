import axiosInstance from './axiosConfig';

/**
* Downloads a document by its ID
* @param {string} id - The document ID to download
* @param {string} fileName - The file name for the download
*/
export const downloadDocument = async (id, fileName) => {
    try {
        const response = await axiosInstance.get(`/documents/${id}/download`, {
            responseType: 'blob',
        });

        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Error downloading document:', error);
        throw new Error('Failed to download document');
    }
};