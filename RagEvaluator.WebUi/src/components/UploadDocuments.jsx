import { useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { toast } from 'react-toastify';
import { DocumentArrowUpIcon, DocumentTextIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { uploadDocument } from '../api/UploadDocumentsService';

function UploadDocuments() {
  const [selectedFile, setSelectedFile] = useState(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState(null);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: {
      'application/pdf': ['.pdf'],
    },
    maxFiles: 1,
    onDrop: (acceptedFiles, rejectedFiles) => {
      if (rejectedFiles.length > 0) {
        toast.error('Only PDF files are allowed');
        return;
      }
      if (acceptedFiles.length > 0) {
        setSelectedFile(acceptedFiles[0]);
        setUploadResult(null);
      }
    },
  });

  const handleUpload = async () => {
    if (!selectedFile) {
      toast.error('Please select a file first');
      return;
    }

    setIsUploading(true);
    try {
      const result = await uploadDocument(selectedFile);
      setUploadResult(result);
      toast.success('Document uploaded and processed successfully!');

      // Reset form
      setSelectedFile(null);
    } catch (error) {
      console.error('Upload error:', error);
      toast.error(error.message || 'Failed to upload document');
    } finally {
      setIsUploading(false);
    }
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    setUploadResult(null);
  };

  const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className="w-full max-w-4xl mx-auto space-y-6">
      <div className="text-center mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Upload Document</h1>
        <p className="text-gray-400">Upload PDF documents for RAG processing</p>
      </div>

      {/* Dropzone */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
        <div
          {...getRootProps()}
          className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition-all min-h-[240px] flex flex-col items-center justify-center ${
            isDragActive
              ? 'border-blue-500 bg-blue-500/10'
              : 'border-gray-600 hover:border-blue-400 hover:bg-blue-500/5 bg-[#1F1F1F]'
          }`}
        >
          <input {...getInputProps()} />
          <DocumentArrowUpIcon className={`w-16 h-16 mx-auto mb-4 transition-colors ${isDragActive ? 'text-blue-400' : 'text-gray-400'}`} />
          <div className="min-h-[56px] flex flex-col justify-center">
            {isDragActive ? (
              <p className="text-lg text-blue-400">Drop the PDF file here...</p>
            ) : (
              <>
                <p className="text-lg text-gray-300 mb-2">
                  Drag and drop a PDF file here, or click to select
                </p>
                <p className="text-sm text-gray-500">Only PDF files are supported</p>
              </>
            )}
          </div>
        </div>

        {/* Selected File Info */}
        {selectedFile && (
          <div className="mt-6 bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
            <div className="flex items-start justify-between">
              <div className="flex items-start gap-3 flex-1">
                <DocumentTextIcon className="w-8 h-8 text-blue-400 flex-shrink-0 mt-1" />
                <div className="flex-1 min-w-0">
                  <p className="text-white font-medium truncate">{selectedFile.name}</p>
                  <p className="text-sm text-gray-400">
                    {formatFileSize(selectedFile.size)} • PDF Document
                  </p>
                </div>
              </div>
              <button
                onClick={handleRemoveFile}
                className="text-gray-400 hover:text-red-400 transition-colors ml-4"
                disabled={isUploading}
              >
                <XMarkIcon className="w-6 h-6" />
              </button>
            </div>
          </div>
        )}

        {/* Upload Button */}
        {selectedFile && (
          <div className="mt-6">
            <button
              onClick={handleUpload}
              disabled={isUploading}
              className="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              {isUploading ? (
                <>
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>Uploading and Processing...</span>
                </>
              ) : (
                <>
                  <DocumentArrowUpIcon className="w-5 h-5" />
                  <span>Upload Document</span>
                </>
              )}
            </button>
          </div>
        )}
      </div>

      {/* Upload Result */}
      {uploadResult && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-white mb-4">Upload Successful</h2>
          <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 space-y-3">
            <div className="flex justify-between">
              <span className="text-gray-400">Document ID:</span>
              <span className="text-gray-200 font-mono text-sm">{uploadResult.documentId}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">File Name:</span>
              <span className="text-gray-200">{uploadResult.fileName}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">Pages:</span>
              <span className="text-gray-200">{uploadResult.pageCount}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">Chunks Created:</span>
              <span className="text-gray-200">{uploadResult.chunkCount}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">Uploaded At:</span>
              <span className="text-gray-200">{formatDate(uploadResult.uploadedAt)}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default UploadDocuments;
