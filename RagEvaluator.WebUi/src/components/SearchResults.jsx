import { DocumentTextIcon, ClockIcon, ArrowDownTrayIcon } from '@heroicons/react/24/outline';
import { PropTypes } from 'prop-types';
import { toast } from 'react-toastify';
import { downloadDocument } from '../api/DownloadDocumentService';

function SearchResults({ results }) {
  if (!results) {
    return null;
  }

  const formatTimestamp = (timestamp) => {
    return new Date(timestamp).toLocaleString();
  };

  const getSimilarityColor = (similarity) => {
    if (similarity >= 0.8) return 'text-green-400';
    if (similarity >= 0.6) return 'text-yellow-400';
    return 'text-orange-400';
  };

  const handleDownload = async (documentId, fileName) => {
    try {
      await downloadDocument(documentId, fileName);
    } catch (err) {
      toast.error(err.message || 'Failed to download document');
    }
  };

  return (
    <div className="w-full space-y-6">
      {/* Answer Section */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
        <div className="flex items-center gap-2 mb-4">
          <DocumentTextIcon className="w-6 h-6 text-blue-400" />
          <h2 className="text-xl font-semibold text-white">Answer</h2>
        </div>
        <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
          <p className="text-gray-200 leading-relaxed whitespace-pre-wrap">{results.answer}</p>
        </div>
        <div className="flex items-center gap-2 mt-4 text-sm text-gray-400">
          <ClockIcon className="w-4 h-4" />
          <span>{formatTimestamp(results.timestamp)}</span>
          <span className="ml-auto">Query ID: {results.queryId}</span>
        </div>
      </div>

      {/* Sources Section */}
      {results.sources && results.sources.length > 0 && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-white mb-4">
            Sources ({results.sources.length})
          </h2>
          <div className="space-y-4">
            {results.sources.map((source, index) => (
              <div
                key={source.id || index}
                className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 hover:border-gray-600 transition-colors"
              >
                <div className="flex items-start justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-400">Source {index + 1}</span>
                    {source.fileName && (
                      <span className="text-xs text-gray-500">
                        ({source.fileName})
                      </span>
                    )}
                    {source.documentId && (
                      <button
                        onClick={() => handleDownload(source.documentId, source.fileName)}
                        className="text-gray-400 hover:text-blue-400 transition-colors"
                        title="Download source document"
                      >
                        <ArrowDownTrayIcon className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-gray-400">Similarity:</span>
                    <span className={`text-sm font-bold ${getSimilarityColor(source.similarity)}`}>
                      {(source.similarity * 100).toFixed(1)}%
                    </span>
                  </div>
                </div>
                <p className="text-gray-300 text-sm leading-relaxed">{source.text}</p>
                {(source.chunkingStrategy || source.embeddingModel) && (
                  <div className="mt-3 pt-3 border-t border-gray-700">
                    <details className="text-xs text-gray-500">
                      <summary className="cursor-pointer hover:text-gray-400">
                        Details
                      </summary>
                      <div className="mt-2 space-y-1">
                        {source.chunkingStrategy && (
                          <p><span className="text-gray-400">Chunking Strategy:</span> {source.chunkingStrategy}</p>
                        )}
                        {source.embeddingModel && (
                          <p><span className="text-gray-400">Embedding Model:</span> {source.embeddingModel}</p>
                        )}
                      </div>
                    </details>
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

SearchResults.propTypes = {
    results: PropTypes.shape({
        queryId: PropTypes.string.isRequired,
        question: PropTypes.string.isRequired,
        answer: PropTypes.string.isRequired,
        sources: PropTypes.arrayOf(
            PropTypes.shape({
                id: PropTypes.string,
                text: PropTypes.string.isRequired,
                similarity: PropTypes.number.isRequired,
                documentId: PropTypes.string,
                fileName: PropTypes.string,
                chunkingStrategy: PropTypes.string,
                embeddingModel: PropTypes.string,
            })
        ),
        timestamp: PropTypes.string.isRequired,
    }),
};

export default SearchResults;
