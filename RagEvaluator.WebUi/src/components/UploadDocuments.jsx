import { useState, useEffect } from "react";
import { useDropzone } from "react-dropzone";
import { toast } from "react-toastify";
import {
  DocumentArrowUpIcon,
  DocumentTextIcon,
  XMarkIcon,
} from "@heroicons/react/24/outline";
import { uploadDocument } from "../api/documentService";
import { getSettings } from "../api/settingsService";
import { formatFileSize } from "../utils/formatFileSize";
import { formatDate } from "../utils/formatDate";

function UploadDocuments() {
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadResults, setUploadResults] = useState([]);
  const [availableCourses, setAvailableCourses] = useState([]);

  useEffect(() => {
    const fetchSettings = async () => {
      try {
        const settings = await getSettings();
        setAvailableCourses(settings.availableCourses || []);
      } catch (error) {
        console.error("Failed to fetch settings:", error);
      }
    };
    fetchSettings();
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: {
      "application/pdf": [".pdf"],
    },
    maxFiles: 20,
    onDrop: (acceptedFiles, rejectedFiles) => {
      if (rejectedFiles.length > 0) {
        toast.error("Only PDF files are allowed");
        return;
      }
      if (acceptedFiles.length > 0) {
        const newFiles = acceptedFiles.map((file) => ({
          id: crypto.randomUUID(),
          file,
          language: "en",
          course: "",
        }));
        setSelectedFiles((prev) => {
          const combined = [...prev, ...newFiles];
          if (combined.length > 20) {
            toast.warning("Maximum 20 files allowed");
            return combined.slice(0, 20);
          }
          return combined;
        });
        setUploadResults([]);
      }
    },
  });

  const handleUpload = async () => {
    if (selectedFiles.length === 0) {
      toast.error("Please select at least one file");
      return;
    }

    const missingCourse = selectedFiles.some((f) => !f.course);
    if (missingCourse) {
      toast.error("Please select a course for all files");
      return;
    }

    setIsUploading(true);
    const results = [];

    for (const { id, file, language, course } of selectedFiles) {
      try {
        const result = await uploadDocument(file, language, course);
        results.push({ id, success: true, result });
      } catch (error) {
        console.error("Upload error:", error);
        results.push({
          id,
          success: false,
          fileName: file.name,
          error: error.message,
        });
      }
    }

    setUploadResults(results);

    const successCount = results.filter((r) => r.success).length;
    const failCount = results.filter((r) => !r.success).length;

    if (failCount === 0) {
      toast.success(`All ${successCount} document(s) uploaded successfully!`);
    } else if (successCount === 0) {
      toast.error(`All ${failCount} document(s) failed to upload`);
    } else {
      toast.warning(`${successCount} succeeded, ${failCount} failed`);
    }

    setSelectedFiles([]);
    setIsUploading(false);
  };

  const handleRemoveFile = (id) => {
    setSelectedFiles((prev) => prev.filter((f) => f.id !== id));
  };

  const handleLanguageChange = (id, language) => {
    setSelectedFiles((prev) =>
      prev.map((f) => (f.id === id ? { ...f, language } : f)),
    );
  };

  const handleCourseChange = (id, course) => {
    setSelectedFiles((prev) =>
      prev.map((f) => (f.id === id ? { ...f, course } : f)),
    );
  };

  return (
    <div className="w-full max-w-7xl mx-auto space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Upload Documents</h1>
        <p className="text-gray-400">
          Upload PDF documents for RAG processing (max 20 files)
        </p>
      </div>

      {/* Drag-and-drop zone with file list and upload button */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6 border border-gray-700">
        <div
          {...getRootProps()}
          className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition-all min-h-[240px] flex flex-col items-center justify-center ${
            isDragActive
              ? "border-blue-500 bg-blue-500/10"
              : "border-gray-600 hover:border-blue-400 hover:bg-blue-500/5 bg-[#1F1F1F]"
          }`}
        >
          <input {...getInputProps()} />
          <DocumentArrowUpIcon
            className={`w-16 h-16 mx-auto mb-4 transition-colors ${isDragActive ? "text-blue-400" : "text-gray-400"}`}
          />
          <div className="min-h-[56px] flex flex-col justify-center">
            {isDragActive ? (
              <p className="text-lg text-blue-400">
                Drop the PDF files here...
              </p>
            ) : (
              <>
                <p className="text-lg text-gray-300 mb-2">
                  Drag and drop PDF files here, or click to select
                </p>
                <p className="text-sm text-gray-500">
                  Only PDF files are supported (max 20 files)
                </p>
              </>
            )}
          </div>
        </div>

        {/* Selected files with language selector and remove button */}
        {selectedFiles.length > 0 && (
          <div className="mt-6 space-y-4">
            <p className="text-gray-300 font-medium">
              Selected Files ({selectedFiles.length})
            </p>
            {selectedFiles.map(({ id, file, language, course }) => (
              <div
                key={id}
                className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700"
              >
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3 flex-1">
                    <DocumentTextIcon className="w-8 h-8 text-blue-400 flex-shrink-0 mt-1" />
                    <div className="flex-1 min-w-0">
                      <p className="text-white font-medium truncate">
                        {file.name}
                      </p>
                      <p className="text-sm text-gray-400">
                        {formatFileSize(file.size)} • PDF Document
                      </p>
                    </div>
                  </div>
                  <button
                    onClick={() => handleRemoveFile(id)}
                    className="text-gray-400 hover:text-red-400 transition-colors ml-4"
                    disabled={isUploading}
                  >
                    <XMarkIcon className="w-6 h-6" />
                  </button>
                </div>

                {/* Language and Course Selection for this file */}
                <div className="mt-3 pt-3 border-t border-gray-700">
                  <div className="flex items-center gap-6 flex-wrap">
                    <div className="flex items-center gap-2">
                      <label
                        htmlFor={`language-${id}`}
                        className="text-gray-400 text-sm"
                      >
                        Language:
                      </label>
                      <select
                        id={`language-${id}`}
                        value={language}
                        onChange={(e) =>
                          handleLanguageChange(id, e.target.value)
                        }
                        disabled={isUploading}
                        className="px-3 py-1.5 bg-[#2D2D2D] border border-gray-600 rounded-lg text-gray-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      >
                        <option value="en">English</option>
                        <option value="de">German</option>
                      </select>
                    </div>
                    <div className="flex items-center gap-2">
                      <label
                        htmlFor={`course-${id}`}
                        className="text-gray-400 text-sm"
                      >
                        Course:
                      </label>
                      <select
                        id={`course-${id}`}
                        value={course}
                        onChange={(e) => handleCourseChange(id, e.target.value)}
                        disabled={isUploading}
                        className={`px-3 py-1.5 bg-[#2D2D2D] border rounded-lg text-gray-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent ${course ? "border-gray-600" : "border-red-500"}`}
                      >
                        <option value="">Select course...</option>
                        {availableCourses.map((c) => (
                          <option key={c} value={c}>
                            {c}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Upload Button */}
        {selectedFiles.length > 0 && (
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
                  <span>
                    Upload {selectedFiles.length} Document
                    {selectedFiles.length > 1 ? "s" : ""}
                  </span>
                </>
              )}
            </button>
          </div>
        )}
      </div>

      {/* Upload results: success details or error messages per file */}
      {uploadResults.length > 0 && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-white mb-4">
            Upload Results
          </h2>
          <div className="space-y-4">
            {uploadResults.map((result) => (
              <div
                key={result.success ? result.result.id : result.id}
                className={`bg-[#1F1F1F] rounded-lg p-4 border ${
                  result.success ? "border-green-700" : "border-red-700"
                }`}
              >
                {result.success ? (
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 mb-3">
                      <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                      <span className="text-green-400 font-medium">
                        Success
                      </span>
                    </div>
                    <div className="grid grid-cols-2 gap-2 text-sm">
                      <span className="text-gray-400">Document ID:</span>
                      <span className="text-gray-200 font-mono text-xs">
                        {result.result.id}
                      </span>
                      <span className="text-gray-400">File Name:</span>
                      <span className="text-gray-200 truncate">
                        {result.result.fileName}
                      </span>
                      <span className="text-gray-400">Language:</span>
                      <span className="text-gray-200">
                        {result.result.language === "en" ? "English" : "German"}
                      </span>
                      <span className="text-gray-400">Course:</span>
                      <span className="text-gray-200">
                        {result.result.course}
                      </span>
                      <span className="text-gray-400">Pages:</span>
                      <span className="text-gray-200">
                        {result.result.pageCount}
                      </span>
                      <span className="text-gray-400">Chunks:</span>
                      <span className="text-gray-200">
                        {result.result.chunkCount}
                      </span>
                      <span className="text-gray-400">Uploaded At:</span>
                      <span className="text-gray-200">
                        {formatDate(result.result.uploadedAt)}
                      </span>
                    </div>
                  </div>
                ) : (
                  <div>
                    <div className="flex items-center gap-2 mb-2">
                      <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                      <span className="text-red-400 font-medium">Failed</span>
                    </div>
                    <p className="text-gray-300 text-sm">{result.fileName}</p>
                    <p className="text-red-400 text-sm mt-1">{result.error}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default UploadDocuments;
