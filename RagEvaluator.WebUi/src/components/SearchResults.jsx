import { DocumentTextIcon, ClockIcon } from '@heroicons/react/24/outline';
import { PropTypes } from 'prop-types';

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
                    {source.metadata?.fileName && (
                      <span className="text-xs text-gray-500">
                        ({source.metadata.fileName})
                      </span>
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
                {source.metadata && Object.keys(source.metadata).length > 0 && (
                  <div className="mt-3 pt-3 border-t border-gray-700">
                    <details className="text-xs text-gray-500">
                      <summary className="cursor-pointer hover:text-gray-400">
                        Metadata
                      </summary>
                      <pre className="mt-2 bg-[#0D0D0D] p-2 rounded overflow-x-auto">
                        {JSON.stringify(source.metadata, null, 2)}
                      </pre>
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
                metadata: PropTypes.object,
            })
        ),
        timestamp: PropTypes.string.isRequired,
    }),
};

export default SearchResults;
