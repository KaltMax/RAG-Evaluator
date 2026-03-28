import { useState, useEffect } from "react";
import { toast } from "react-toastify";
import {
  ArrowPathIcon,
  ArrowDownTrayIcon,
  TrashIcon,
  ChevronUpIcon,
  ChevronDownIcon,
  ChevronUpDownIcon,
} from "@heroicons/react/24/outline";
import {
  getAllDocuments,
  deleteDocument,
  downloadDocument,
} from "../api/documentService";
import { formatDate } from "../utils/formatDate";
import { formatFileSize } from "../utils/formatFileSize";
import { formatLanguage } from "../utils/formatLanguage";
import { sortByKey } from "../utils/sortByKey";

function DocumentList() {
  const [documents, setDocuments] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [sortKey, setSortKey] = useState(null);
  const [sortDirection, setSortDirection] = useState("asc");

  const fetchDocuments = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getAllDocuments();
      setDocuments(data);
    } catch (err) {
      const message = `Failed to load documents: ${err.message}`;
      setError(message);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, []);

  const handleDownload = async (id, fileName) => {
    try {
      await downloadDocument(id, fileName);
    } catch (err) {
      toast.error(`Failed to download document: ${err.message}`);
    }
  };

  const handleDelete = async (id) => {
    if (!confirm("Are you sure you want to delete this document?")) {
      return;
    }

    try {
      await deleteDocument(id);
      toast.success("Document deleted successfully");
      fetchDocuments();
    } catch (err) {
      toast.error(`Failed to delete document: ${err.message}`);
    }
  };

  const getStatusBadge = (status) => {
    const statusStyles = {
      Pending: "bg-yellow-500/20 text-yellow-400",
      Processing: "bg-blue-500/20 text-blue-400",
      Completed: "bg-green-500/20 text-green-400",
      Failed: "bg-red-500/20 text-red-400",
    };

    return (
      <span
        className={`px-2 py-1 rounded-full text-xs font-medium ${statusStyles[status] || "bg-gray-500/20 text-gray-400"}`}
      >
        {status}
      </span>
    );
  };

  const handleSort = (key) => {
    if (sortKey === key) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDirection("asc");
    }
  };

  const sortedDocuments = sortByKey(documents, sortKey, sortDirection);

  const renderSortIcon = (columnKey) => {
    if (sortKey !== columnKey)
      return (
        <ChevronUpDownIcon className="w-4 h-4 inline ml-1 text-gray-600" />
      );
    return sortDirection === "asc" ? (
      <ChevronUpIcon className="w-4 h-4 inline ml-1 text-blue-400" />
    ) : (
      <ChevronDownIcon className="w-4 h-4 inline ml-1 text-blue-400" />
    );
  };

  return (
    <div className="w-full max-w-7xl mx-auto space-y-6">
      {/* Page header with title and refresh button */}
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Documents</h1>
          <p className="text-gray-400">Manage your uploaded documents</p>
        </div>
        <button
          onClick={fetchDocuments}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-[#2D2D2D] hover:bg-[#3D3D3D] text-gray-300 rounded-lg transition-colors disabled:opacity-50 border border-gray-700"
        >
          <ArrowPathIcon
            className={`w-5 h-5 ${isLoading ? "animate-spin" : ""}`}
          />
          <span className="hidden md:inline">Refresh</span>
        </button>
      </div>

      {/* Document table with loading, error, and empty states */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg overflow-hidden border border-gray-700">
        {isLoading && documents.length === 0 ? (
          <div className="flex items-center justify-center py-12">
            <ArrowPathIcon className="w-8 h-8 text-gray-400 animate-spin" />
          </div>
        ) : error ? (
          <div className="text-center py-12">
            <p className="text-red-400 mb-4">{error}</p>
            <button
              onClick={fetchDocuments}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
            >
              Try Again
            </button>
          </div>
        ) : documents.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-400">No documents found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              {/* Sortable column headers */}
              <thead className="bg-[#1F1F1F]">
                <tr>
                  {[
                    { key: "fileName", label: "File Name" },
                    { key: "fileSize", label: "Size" },
                    { key: "pageCount", label: "Pages" },
                    { key: "chunkCount", label: "Chunks" },
                    { key: "language", label: "Language" },
                    { key: "course", label: "Course" },
                    { key: "status", label: "Status" },
                    { key: "uploadedAt", label: "Uploaded At" },
                  ].map(({ key, label }) => (
                    <th
                      key={key}
                      onClick={() => handleSort(key)}
                      className="px-3 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:text-gray-200 transition-colors"
                    >
                      {label}
                      {renderSortIcon(key)}
                    </th>
                  ))}
                  <th className="px-3 py-3 text-right text-xs font-medium text-gray-400 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              {/* Document rows with download and delete actions */}
              <tbody className="divide-y divide-gray-700">
                {sortedDocuments.map((doc) => (
                  <tr
                    key={doc.id}
                    className="hover:bg-[#252525] transition-colors"
                  >
                    <td className="px-3 py-4">
                      <div
                        className="text-sm text-white font-medium break-words max-w-xs"
                        title={doc.fileName}
                      >
                        {doc.fileName}
                      </div>
                      <div
                        className="text-xs text-gray-400 font-mono truncate max-w-xs"
                        title={doc.id}
                      >
                        {doc.id}
                      </div>
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap text-sm text-gray-300">
                      {formatFileSize(doc.fileSize)}
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap text-sm text-gray-300">
                      {doc.pageCount}
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap text-sm text-gray-300">
                      {doc.chunkCount}
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap text-sm text-gray-300">
                      {formatLanguage(doc.language)}
                    </td>
                    <td className="px-3 py-4 text-sm text-gray-300">
                      {doc.course}
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap">
                      {getStatusBadge(doc.status)}
                    </td>
                    <td className="px-3 py-4 text-sm text-gray-300">
                      {formatDate(doc.uploadedAt)}
                    </td>
                    <td className="px-3 py-4 whitespace-nowrap text-right">
                      <button
                        onClick={() => handleDownload(doc.id, doc.fileName)}
                        className="text-gray-400 hover:text-blue-400 transition-colors p-1 mr-2"
                        title="Download document"
                      >
                        <ArrowDownTrayIcon className="w-5 h-5" />
                      </button>
                      <button
                        onClick={() => handleDelete(doc.id)}
                        className="text-gray-400 hover:text-red-400 transition-colors p-1"
                        title="Delete document"
                      >
                        <TrashIcon className="w-5 h-5" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Document count footer */}
      {documents.length > 0 && (
        <div className="text-sm text-gray-500 text-center">
          Showing {documents.length} document{documents.length === 1 ? "" : "s"}
        </div>
      )}
    </div>
  );
}

export default DocumentList;
