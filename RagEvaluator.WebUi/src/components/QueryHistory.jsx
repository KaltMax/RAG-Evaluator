import { useState, useEffect } from "react";
import { toast } from "react-toastify";
import {
  ArrowPathIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  ChevronUpDownIcon,
  ClockIcon,
  TrashIcon,
} from "@heroicons/react/24/outline";
import { getAllQueries, deleteQuery, getQueryById } from "../api/queryService";
import { formatDate } from "../utils/formatDate";
import { formatMetric } from "../utils/formatMetric";
import {
  getResponseQualityOption,
  getResponseQualityColor,
} from "../utils/responseQualityOptions";
import { formatResponseTime } from "../utils/formatResponseTime";
import { formatLanguage } from "../utils/formatLanguage";
import { sortByKey } from "../utils/sortByKey";
import SearchResults from "./SearchResults";

function QueryHistory() {
  const [queries, setQueries] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [expandedIds, setExpandedIds] = useState(new Set());
  const [queryDetails, setQueryDetails] = useState({});
  const [loadingDetails, setLoadingDetails] = useState(new Set());
  const [annotatingIds, setAnnotatingIds] = useState(new Set());
  const [sortKey, setSortKey] = useState(null);
  const [sortDirection, setSortDirection] = useState("asc");

  const fetchQueries = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getAllQueries();
      setQueries(data);
    } catch (err) {
      const message = `Failed to load queries: ${err.message}`;
      setError(message);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchQueries();
  }, []);

  const isPending = (query) => query.responseQuality == null;

  const toggleExpanded = (id) => {
    setExpandedIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const handleStartAnnotation = async (id) => {
    if (queryDetails[id]) {
      setAnnotatingIds((prev) => new Set([...prev, id]));
      return;
    }

    setLoadingDetails((prev) => new Set([...prev, id]));
    try {
      const data = await getQueryById(id);
      setQueryDetails((prev) => ({ ...prev, [id]: data }));
      setAnnotatingIds((prev) => new Set([...prev, id]));
    } catch (err) {
      console.error("Error fetching query details:", err);
      toast.error(`Failed to load query details: ${err.message}`);
    } finally {
      setLoadingDetails((prev) => {
        const newSet = new Set(prev);
        newSet.delete(id);
        return newSet;
      });
    }
  };

  const handleAnnotated = (id) => {
    setAnnotatingIds((prev) => {
      const newSet = new Set(prev);
      newSet.delete(id);
      return newSet;
    });
    setQueryDetails((prev) => {
      const newDetails = { ...prev };
      delete newDetails[id];
      return newDetails;
    });
    fetchQueries();
  };

  const getMetricsStatus = (query) => {
    if (!isPending(query)) {
      return { label: "Evaluated", color: "bg-green-500/20 text-green-400" };
    }
    return { label: "Pending", color: "bg-yellow-500/20 text-yellow-400" };
  };

  const handleDelete = async (e, id) => {
    e.stopPropagation();
    if (!confirm("Are you sure you want to delete this query?")) {
      return;
    }

    try {
      await deleteQuery(id);
      toast.success("Query deleted successfully");
      fetchQueries();
    } catch (err) {
      toast.error(`Failed to delete query: ${err.message}`);
    }
  };

  const sortOptions = [
    { key: "question", label: "Question" },
    { key: "language", label: "Language" },
    { key: "createdAt", label: "Created At" },
    { key: "experimentName", label: "Experiment" },
    { key: "responseQuality", label: "Status" },
  ];

  const handleSort = (key) => {
    if (sortKey === key) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDirection("asc");
    }
  };

  const sortedQueries = sortByKey(queries, sortKey, sortDirection);

  return (
    <div className="w-full max-w-6xl mx-auto space-y-6">
      {/* Page header with title and refresh button */}
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Query History</h1>
          <p className="text-gray-400">Review and analyze past queries</p>
        </div>
        <button
          onClick={fetchQueries}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-[#2D2D2D] hover:bg-[#3D3D3D] text-gray-300 rounded-lg transition-colors disabled:opacity-50"
        >
          <ArrowPathIcon
            className={`w-5 h-5 ${isLoading ? "animate-spin" : ""}`}
          />
          <span className="hidden md:inline">Refresh</span>
        </button>
      </div>

      {/* Query cards with loading, error, and empty states */}
      {isLoading && queries.length === 0 ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 flex items-center justify-center">
          <ArrowPathIcon className="w-8 h-8 text-gray-400 animate-spin" />
        </div>
      ) : error ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center">
          <p className="text-red-400 mb-4">{error}</p>
          <button
            onClick={fetchQueries}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            Try Again
          </button>
        </div>
      ) : queries.length === 0 ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center">
          <p className="text-gray-400">No queries found</p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Sort controls */}
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-xs text-gray-300 uppercase tracking-wider">
              Sort by
            </span>
            {sortOptions.map(({ key, label }) => (
              <button
                key={key}
                onClick={() => handleSort(key)}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors select-none ${
                  sortKey === key
                    ? "bg-blue-500/20 text-blue-400 border border-blue-500/40"
                    : "bg-[#2D2D2D] text-gray-400 border border-gray-700 hover:border-gray-500"
                }`}
              >
                {label}
                {sortKey === key ? (
                  sortDirection === "asc" ? (
                    <ChevronUpIcon className="w-3 h-3 inline ml-1" />
                  ) : (
                    <ChevronDownIcon className="w-3 h-3 inline ml-1" />
                  )
                ) : (
                  <ChevronUpDownIcon className="w-3 h-3 inline ml-1" />
                )}
              </button>
            ))}
          </div>

          {sortedQueries.map((query) => {
            const isExpanded = expandedIds.has(query.id);
            const metricsStatus = getMetricsStatus(query);

            return (
              <div
                key={query.id}
                className="bg-[#2D2D2D] rounded-lg shadow-lg overflow-hidden"
              >
                {/* Collapsed card header with question, status badge, and delete */}
                <div
                  className="w-full px-6 py-4 flex items-center gap-4 hover:bg-[#353535] transition-colors cursor-pointer select-none"
                  onClick={() => toggleExpanded(query.id)}
                >
                  <div className="flex-1 min-w-0 flex items-center gap-4">
                    <div className="flex-1 min-w-0">
                      <p className="text-white font-medium truncate">
                        {query.question}
                      </p>
                      <p
                        className="text-xs text-gray-400 font-mono truncate mt-1"
                        title={query.id}
                      >
                        {query.id}
                      </p>
                      <div className="flex items-center gap-2 mt-1 text-sm text-gray-400">
                        <ClockIcon className="w-4 h-4" />
                        <span>{formatDate(query.createdAt)}</span>
                      </div>
                    </div>
                    <div className="hidden md:block text-center w-48 shrink-0">
                      <p className="text-xs text-gray-400">Experiment</p>
                      {query.experimentName ? (
                        <p
                          className="text-sm text-purple-400 font-medium truncate"
                          title={query.experimentName}
                        >
                          {query.experimentName}
                        </p>
                      ) : (
                        <p className="text-sm text-gray-500">None</p>
                      )}
                    </div>
                    <span
                      className={`px-2 py-1 rounded-full text-xs font-medium ${metricsStatus.color}`}
                    >
                      {metricsStatus.label}
                    </span>
                    {isExpanded ? (
                      <ChevronUpIcon className="w-5 h-5 text-gray-400 flex-shrink-0" />
                    ) : (
                      <ChevronDownIcon className="w-5 h-5 text-gray-400 flex-shrink-0" />
                    )}
                  </div>
                  <button
                    onClick={(e) => handleDelete(e, query.id)}
                    className="p-1 text-gray-400 hover:text-red-400 transition-colors"
                    title="Delete query"
                  >
                    <TrashIcon className="w-5 h-5" />
                  </button>
                </div>

                {/* Expanded content: answer, query details, and evaluation metrics */}
                {isExpanded && (
                  <div className="px-6 pb-6 border-t border-gray-700">
                    {/* LLM-generated answer */}
                    <div className="mt-4">
                      <h3 className="text-sm font-bold text-gray-200 mb-2">
                        Answer
                      </h3>
                      <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
                        <p className="text-gray-200 text-sm leading-relaxed whitespace-pre-wrap">
                          {query.answer}
                        </p>
                      </div>
                    </div>

                    {/* Query parameters: system prompt, top-k, language, models, chunking */}
                    <div className="mt-4">
                      <h3 className="text-sm font-bold text-gray-200 mb-2">
                        Query Details
                      </h3>
                      <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 mb-4">
                        <p className="text-gray-400 text-xs mb-1 text-center">
                          System Prompt
                        </p>
                        <p className="text-gray-200 text-sm leading-relaxed whitespace-pre-wrap">
                          {query.systemPrompt || "-"}
                        </p>
                      </div>
                      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">Top-K</p>
                          <p className="text-gray-200 text-sm font-medium">
                            {query.topK}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">Language</p>
                          <p className="text-gray-200 text-sm font-medium">
                            {formatLanguage(query.language)}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">
                            Chat Model
                          </p>
                          <p
                            className="text-gray-200 text-sm font-medium truncate"
                            title={query.chatModel}
                          >
                            {query.chatModel || "-"}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">
                            Embedding Model
                          </p>
                          <p
                            className="text-gray-200 text-sm font-medium truncate"
                            title={query.embeddingModel}
                          >
                            {query.embeddingModel || "-"}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">
                            Chunking Strategy
                          </p>
                          <p
                            className="text-gray-200 text-sm font-medium truncate"
                            title={query.chunkingStrategy}
                          >
                            {query.chunkingStrategy || "-"}
                          </p>
                        </div>
                      </div>
                    </div>

                    {/* Pending: annotate button or SearchResults for evaluation */}
                    {isPending(query) && (
                      <div className="mt-4">
                        <h3 className="text-sm font-bold text-gray-200 mb-2">
                          Evaluation Metrics
                        </h3>
                        {annotatingIds.has(query.id) ? (
                          queryDetails[query.id] ? (
                            <SearchResults
                              results={queryDetails[query.id]}
                              onAnnotated={() => handleAnnotated(query.id)}
                            />
                          ) : (
                            <div className="flex items-center justify-center p-12">
                              <ArrowPathIcon className="w-8 h-8 text-gray-400 animate-spin" />
                            </div>
                          )
                        ) : (
                          <button
                            onClick={() => handleStartAnnotation(query.id)}
                            disabled={loadingDetails.has(query.id)}
                            className="px-6 py-2.5 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors flex items-center gap-2"
                          >
                            {loadingDetails.has(query.id) ? (
                              <>
                                <ArrowPathIcon className="w-4 h-4 animate-spin" />
                                <span>Loading...</span>
                              </>
                            ) : (
                              <span>Annotate</span>
                            )}
                          </button>
                        )}
                      </div>
                    )}

                    {/* Evaluated: retrieval metrics (MRR, Precision, Recall, NDCG) */}
                    {!isPending(query) && (
                      <div className="mt-4">
                        <h3 className="text-sm font-bold text-gray-200 mb-2">
                          Evaluation Metrics
                        </h3>
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">MRR</p>
                            <p className="text-blue-400 text-lg font-bold">
                              {formatMetric(query.mrr)}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">
                              Precision@K
                            </p>
                            <p className="text-blue-400 text-lg font-bold">
                              {formatMetric(query.precisionAtK)}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">
                              Recall@K
                            </p>
                            <p className="text-blue-400 text-lg font-bold">
                              {formatMetric(query.recallAtK)}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">NDCG@K</p>
                            <p className="text-blue-400 text-lg font-bold">
                              {formatMetric(query.ndcgAtK)}
                            </p>
                          </div>
                        </div>

                        {/* Quality metrics: response time, language switching, response quality */}
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">
                              Response Time
                            </p>
                            <p className="text-purple-400 text-lg font-bold">
                              {formatResponseTime(query.responseTimeMs)}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">
                              Language Switching
                            </p>
                            <p
                              className={`text-lg font-bold ${query.hasLanguageSwitching ? "text-red-400" : "text-green-400"}`}
                            >
                              {query.hasLanguageSwitching ? "Yes" : "No"}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center col-span-2 md:col-span-2">
                            <p className="text-gray-400 text-xs mb-1">
                              Response Quality
                            </p>
                            <p
                              className={`text-lg font-bold ${getResponseQualityColor(query.responseQuality)}`}
                            >
                              {getResponseQualityOption(query.responseQuality)
                                ?.label || "N/A"}
                            </p>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Query count footer */}
      {queries.length > 0 && (
        <div className="text-sm text-gray-500 text-center">
          Showing {queries.length} quer{queries.length === 1 ? "y" : "ies"}
        </div>
      )}
    </div>
  );
}

export default QueryHistory;
