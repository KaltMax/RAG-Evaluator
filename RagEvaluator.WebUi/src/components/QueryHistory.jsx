import { useState, useEffect } from 'react';
import { toast } from 'react-toastify';
import { ArrowPathIcon, ChevronDownIcon, ChevronUpIcon, ClockIcon } from '@heroicons/react/24/outline';
import { getAllQueries } from '../api/GetAllQueriesService';
import { formatDate } from '../utils/formatDate';
import { formatMetric } from '../utils/formatMetric';
import { getResponseQualityOption, getResponseQualityColor } from '../utils/responseQualityOptions';
import { formatResponseTime } from '../utils/formatResponseTime';
import { formatLanguage } from '../utils/formatLanguage';

function QueryHistory() {
  const [queries, setQueries] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [expandedIds, setExpandedIds] = useState(new Set());

  const fetchQueries = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getAllQueries();
      setQueries(data);
    } catch (err) {
      setError(err.message);
      toast.error(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchQueries();
  }, []);

  const toggleExpanded = (id) => {
    setExpandedIds(prev => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const getMetricsStatus = (query) => {
    if (query.mrr !== null && query.mrr !== undefined) {
      return { label: 'Evaluated', color: 'bg-green-500/20 text-green-400' };
    }
    return { label: 'Pending', color: 'bg-yellow-500/20 text-yellow-400' };
  };

  return (
    <div className="w-full max-w-6xl mx-auto space-y-6">
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
          <ArrowPathIcon className={`w-5 h-5 ${isLoading ? 'animate-spin' : ''}`} />
          <span className="hidden md:inline">Refresh</span>
        </button>
      </div>

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
          {queries.map((query) => {
            const isExpanded = expandedIds.has(query.id);
            const metricsStatus = getMetricsStatus(query);

            return (
              <div
                key={query.id}
                className="bg-[#2D2D2D] rounded-lg shadow-lg overflow-hidden"
              >
                {/* Collapsed Header */}
                <button
                  onClick={() => toggleExpanded(query.id)}
                  className="w-full px-6 py-4 flex items-center gap-4 hover:bg-[#353535] transition-colors text-left"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-white font-medium truncate">{query.question}</p>
                    <p className="text-xs text-gray-400 font-mono truncate mt-1" title={query.id}>
                      {query.id}
                    </p>
                    <div className="flex items-center gap-2 mt-1 text-sm text-gray-400">
                      <ClockIcon className="w-4 h-4" />
                      <span>{formatDate(query.createdAt)}</span>
                    </div>
                  </div>
                  <span className={`px-2 py-1 rounded-full text-xs font-medium ${metricsStatus.color}`}>
                    {metricsStatus.label}
                  </span>
                  {isExpanded ? (
                    <ChevronUpIcon className="w-5 h-5 text-gray-400 flex-shrink-0" />
                  ) : (
                    <ChevronDownIcon className="w-5 h-5 text-gray-400 flex-shrink-0" />
                  )}
                </button>

                {/* Expanded Content */}
                {isExpanded && (
                  <div className="px-6 pb-6 border-t border-gray-700">
                    {/* Answer Section */}
                    <div className="mt-4">
                      <h3 className="text-sm font-bold text-gray-200 mb-2">Answer</h3>
                      <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
                        <p className="text-gray-200 text-sm leading-relaxed whitespace-pre-wrap">
                          {query.answer}
                        </p>
                      </div>
                    </div>

                    {/* Query Details */}
                    <div className="mt-4">
                      <h3 className="text-sm font-bold text-gray-200 mb-2">Query Details</h3>
                      <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 mb-4">
                        <p className="text-gray-400 text-xs mb-1 text-center">System Prompt</p>
                        <p className="text-gray-200 text-sm leading-relaxed whitespace-pre-wrap">
                          {query.systemPrompt || '-'}
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
                          <p className="text-gray-400 text-xs mb-1">Chat Model</p>
                          <p className="text-gray-200 text-sm font-medium truncate" title={query.chatModel}>
                            {query.chatModel || '-'}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">Embedding Model</p>
                          <p className="text-gray-200 text-sm font-medium truncate" title={query.embeddingModel}>
                            {query.embeddingModel || '-'}
                          </p>
                        </div>
                        <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                          <p className="text-gray-400 text-xs mb-1">Chunking Strategy</p>
                          <p className="text-gray-200 text-sm font-medium truncate" title={query.chunkingStrategy}>
                            {query.chunkingStrategy || '-'}
                          </p>
                        </div>
                      </div>
                    </div>

                    {/* Metrics Section */}
                    {query.mrr !== null && query.mrr !== undefined && (
                      <div className="mt-4">
                        <h3 className="text-sm font-bold text-gray-200 mb-2">Evaluation Metrics</h3>
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">MRR</p>
                            <p className="text-blue-400 text-lg font-bold">{formatMetric(query.mrr)}</p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">Precision@K</p>
                            <p className="text-blue-400 text-lg font-bold">{formatMetric(query.precisionAtK)}</p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">Recall@K</p>
                            <p className="text-blue-400 text-lg font-bold">{formatMetric(query.recallAtK)}</p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">NDCG@K</p>
                            <p className="text-blue-400 text-lg font-bold">{formatMetric(query.ndcgAtK)}</p>
                          </div>
                        </div>

                        {/* Second Row: Response Time, Language Switching, Response Quality */}
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">Response Time</p>
                            <p className="text-purple-400 text-lg font-bold">{formatResponseTime(query.responseTimeMs)}</p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center">
                            <p className="text-gray-400 text-xs mb-1">Language Switching</p>
                            <p className={`text-lg font-bold ${query.hasLanguageSwitching ? 'text-red-400' : 'text-green-400'}`}>
                              {query.hasLanguageSwitching ? 'Yes' : 'No'}
                            </p>
                          </div>
                          <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700 text-center col-span-2 md:col-span-2">
                            <p className="text-gray-400 text-xs mb-1">Response Quality</p>
                            <p className={`text-lg font-bold ${getResponseQualityColor(query.responseQuality)}`}>
                              {getResponseQualityOption(query.responseQuality)?.label || 'N/A'}
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

      {queries.length > 0 && (
        <div className="text-sm text-gray-500 text-center">
          Showing {queries.length} quer{queries.length === 1 ? 'y' : 'ies'}
        </div>
      )}
    </div>
  );
}

export default QueryHistory;
