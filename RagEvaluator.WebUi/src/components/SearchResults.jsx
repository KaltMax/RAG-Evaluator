import { useState, useEffect } from "react";
import {
  DocumentTextIcon,
  ClockIcon,
  ArrowDownTrayIcon,
  CheckCircleIcon,
} from "@heroicons/react/24/outline";
import { PropTypes } from "prop-types";
import { toast } from "react-toastify";
import { downloadDocument, getAllDocuments } from "../api/documentService";
import { annotateResults } from "../api/queryService";
import { relevanceGrades, getRelevanceGrade } from "../utils/relevanceGrades";
import {
  responseQualityOptions,
  getResponseQualityOption,
  getResponseQualityColor,
} from "../utils/responseQualityOptions";
import { formatMetric } from "../utils/formatMetric";
import { formatResponseTime } from "../utils/formatResponseTime";
import { formatDate } from "../utils/formatDate";

function SearchResults({ results, onAnnotated }) {
  const [annotations, setAnnotations] = useState({});
  const [responseQuality, setResponseQuality] = useState(null);
  const [hasLanguageSwitching, setHasLanguageSwitching] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [metrics, setMetrics] = useState(null);
  const [relevantDocuments, setRelevantDocuments] = useState(
    results?.relevantDocumentIds ?? [],
  );
  const [availableDocuments, setAvailableDocuments] = useState([]);

  useEffect(() => {
    const fetchDocuments = async () => {
      try {
        const docs = await getAllDocuments();
        setAvailableDocuments(docs);
      } catch (error) {
        console.error("Failed to fetch documents:", error);
      }
    };
    fetchDocuments();
  }, []);

  if (!results) {
    return null;
  }

  const getSimilarityColor = (similarity) => {
    if (similarity >= 0.8) return "text-green-400";
    if (similarity >= 0.6) return "text-yellow-400";
    return "text-orange-400";
  };

  const handleDownload = async (documentId, fileName) => {
    try {
      await downloadDocument(documentId, fileName);
    } catch (err) {
      toast.error(`Failed to download document: ${err.message}`);
    }
  };

  const handleAnnotationChange = (sourceId, gradeValue) => {
    setAnnotations((prev) => ({
      ...prev,
      [sourceId]: gradeValue,
    }));
  };

  const getAnnotatedCount = () => {
    return Object.keys(annotations).length;
  };

  const getTotalAnnotationsCount = () => {
    return (
      getAnnotatedCount() +
      (responseQuality === null ? 0 : 1) +
      (relevantDocuments.length > 0 ? 1 : 0)
    );
  };

  const getTotalAnnotationsNeeded = () => {
    return (results.sources?.length || 0) + 2; // sources + response quality + at least 1 relevant document
  };

  const allSourcesAnnotated = () => {
    return getAnnotatedCount() === results.sources?.length;
  };

  const canSubmitAnnotations = () => {
    return (
      allSourcesAnnotated() &&
      responseQuality !== null &&
      relevantDocuments.length > 0
    );
  };

  const handleRelevantDocumentToggle = (documentId) => {
    setRelevantDocuments((prev) => {
      if (prev.includes(documentId)) {
        return prev.filter((id) => id !== documentId);
      } else {
        return [...prev, documentId];
      }
    });
  };

  const handleSubmitAnnotations = async () => {
    if (!canSubmitAnnotations()) {
      if (!allSourcesAnnotated()) {
        toast.warning("Please annotate all sources before submitting");
      } else if (responseQuality === null) {
        toast.warning("Please evaluate the response quality before submitting");
      } else {
        toast.warning(
          "Please select at least one relevant document for Recall@K calculation",
        );
      }
      return;
    }

    setIsSubmitting(true);
    try {
      const annotationList = Object.entries(annotations).map(
        ([resultId, relevanceGrade]) => ({
          resultId,
          relevanceGrade,
        }),
      );

      const payload = {
        annotations: annotationList,
        responseQuality: responseQuality,
        hasLanguageSwitching: hasLanguageSwitching,
        relevantDocumentIds: relevantDocuments,
      };

      const updatedQuery = await annotateResults(results.queryId, payload);

      setMetrics({
        mrr: updatedQuery.mrr,
        precisionAtK: updatedQuery.precisionAtK,
        recallAtK: updatedQuery.recallAtK,
        ndcgAtK: updatedQuery.ndcgAtK,
        responseTimeMs: updatedQuery.responseTimeMs,
        responseQuality: responseQuality,
        hasLanguageSwitching: hasLanguageSwitching,
      });

      toast.success("Annotations submitted successfully!");
      onAnnotated?.();
    } catch (error) {
      toast.error(`Failed to submit annotations: ${error.message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="w-full space-y-6">
      {/* Answer with timestamp and response quality evaluation */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
        <div className="flex items-center gap-2 mb-4">
          <DocumentTextIcon className="w-6 h-6 text-blue-400" />
          <h2 className="text-xl font-semibold text-white">Answer</h2>
        </div>
        <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
          <p className="text-gray-200 leading-relaxed whitespace-pre-wrap">
            {results.answer}
          </p>
        </div>
        <div className="flex items-center gap-2 mt-4 text-sm text-gray-400">
          <ClockIcon className="w-4 h-4" />
          <span>{formatDate(results.timestamp)}</span>
          <span className="ml-auto">Query ID: {results.queryId}</span>
        </div>

        {/* Response quality buttons and language switching checkbox */}
        <div className="mt-4 pt-4 border-t border-gray-700">
          <details className="text-sm" open={!metrics}>
            <summary className="cursor-pointer text-gray-400 hover:text-gray-300 mb-3 font-medium">
              Response Quality Evaluation
            </summary>

            {/* Quality annotation buttons */}
            <div className="space-y-2 mb-4">
              <p className="text-xs text-gray-500 mb-2">Overall Quality:</p>
              <div className="flex flex-wrap gap-2">
                {responseQualityOptions.map((option) => {
                  const isSelected = responseQuality === option.value;
                  return (
                    <button
                      key={option.value}
                      onClick={() => setResponseQuality(option.value)}
                      disabled={isSubmitting || metrics}
                      className={`px-3 py-1.5 rounded-md text-xs font-bold text-white transition-all ${
                        isSelected ? option.selectedColor : option.color
                      } ${isSubmitting || metrics ? "opacity-50 cursor-not-allowed" : ""}`}
                      title={option.label}
                    >
                      {option.label}
                    </button>
                  );
                })}
              </div>
              {responseQuality !== null && (
                <p className="text-gray-400 text-xs mt-2">
                  Selected: {getResponseQualityOption(responseQuality)?.label}
                </p>
              )}
            </div>

            {/* Language Switching Checkbox */}
            <label className="flex items-center gap-2 text-sm text-gray-300 cursor-pointer hover:text-white transition-colors">
              <input
                type="checkbox"
                checked={hasLanguageSwitching}
                onChange={(e) => setHasLanguageSwitching(e.target.checked)}
                disabled={isSubmitting || metrics}
                className="w-4 h-4 rounded border-gray-600 bg-gray-700 text-blue-600 focus:ring-2 focus:ring-blue-500 focus:ring-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <span>Language switching detected</span>
            </label>
          </details>
        </div>
      </div>

      {/* Retrieved sources with similarity scores and relevance annotation */}
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
                    <span className="text-sm font-medium text-gray-400">
                      Source {index + 1}
                    </span>
                    {source.fileName && (
                      <span className="text-xs text-gray-500">
                        ({source.fileName})
                      </span>
                    )}
                    {source.documentId && (
                      <button
                        onClick={() =>
                          handleDownload(source.documentId, source.fileName)
                        }
                        className="text-gray-400 hover:text-blue-400 transition-colors"
                        title="Download source document"
                      >
                        <ArrowDownTrayIcon className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-gray-400">Similarity:</span>
                    <span
                      className={`text-sm font-bold ${getSimilarityColor(source.similarity)}`}
                    >
                      {(source.similarity * 100).toFixed(1)}%
                    </span>
                  </div>
                </div>
                <p className="text-gray-300 text-sm leading-relaxed">
                  {source.text}
                </p>

                {/* Relevance grade annotation buttons */}
                <div className="mt-3 pt-3 border-t border-gray-700">
                  <details className="text-xs" open={!metrics}>
                    <summary className="cursor-pointer text-gray-500 hover:text-gray-400 mb-2">
                      Relevance
                    </summary>
                    <div className="flex flex-wrap gap-2 mt-2">
                      {relevanceGrades.map((grade) => {
                        const isSelected =
                          annotations[source.id] === grade.value;
                        return (
                          <button
                            key={grade.value}
                            onClick={() =>
                              handleAnnotationChange(source.id, grade.value)
                            }
                            disabled={isSubmitting || metrics}
                            className={`px-3 py-1.5 rounded-md text-xs font-bold text-white transition-all ${
                              isSelected ? grade.selectedColor : grade.color
                            } ${isSubmitting || metrics ? "opacity-50 cursor-not-allowed" : ""}`}
                            title={grade.label}
                          >
                            {grade.label}
                          </button>
                        );
                      })}
                    </div>
                    {annotations[source.id] !== undefined && (
                      <p className="text-gray-400 text-xs mt-2">
                        Selected:{" "}
                        {getRelevanceGrade(annotations[source.id])?.label}
                      </p>
                    )}
                  </details>
                </div>

                {/* Source metadata: chunking strategy and embedding model */}
                {(source.chunkingStrategy || source.embeddingModel) && (
                  <div className="mt-3 pt-3 border-t border-gray-700">
                    <details className="text-xs text-gray-500">
                      <summary className="cursor-pointer hover:text-gray-400">
                        Details
                      </summary>
                      <div className="mt-2 space-y-1">
                        {source.chunkingStrategy && (
                          <p>
                            <span className="text-gray-400">
                              Chunking Strategy:
                            </span>{" "}
                            {source.chunkingStrategy}
                          </p>
                        )}
                        {source.embeddingModel && (
                          <p>
                            <span className="text-gray-400">
                              Embedding Model:
                            </span>{" "}
                            {source.embeddingModel}
                          </p>
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

      {/* Ground truth document selection for Recall@K */}
      {!metrics && availableDocuments.length > 0 && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-white mb-2">
            Ground Truth Documents
          </h2>
          <p className="text-sm text-gray-400 mb-4">
            Select which documents contain relevant information for this query
            (used for Recall@K calculation).
          </p>
          <div className="space-y-2 max-h-100 overflow-y-auto">
            {availableDocuments.map((doc) => (
              <label
                key={doc.id}
                className="flex items-center gap-3 p-2 rounded-lg hover:bg-[#1F1F1F] cursor-pointer transition-colors"
              >
                <input
                  type="checkbox"
                  checked={relevantDocuments.includes(doc.id)}
                  onChange={() => handleRelevantDocumentToggle(doc.id)}
                  disabled={isSubmitting}
                  className="w-4 h-4 rounded border-gray-600 bg-gray-700 text-blue-600 focus:ring-2 focus:ring-blue-500 focus:ring-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                />
                <span className="text-gray-300 text-sm">{doc.fileName}</span>
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    handleDownload(doc.id, doc.fileName);
                  }}
                  className="text-gray-400 hover:text-blue-400 transition-colors"
                  title="Download document"
                >
                  <ArrowDownTrayIcon className="w-4 h-4" />
                </button>
              </label>
            ))}
          </div>
          {relevantDocuments.length > 0 && (
            <p className="text-gray-400 text-xs mt-3">
              {relevantDocuments.length} document
              {relevantDocuments.length === 1 ? "" : "s"} selected as relevant
            </p>
          )}
        </div>
      )}

      {/* Annotation progress and submit button */}
      {!metrics && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-400">
              {getTotalAnnotationsCount()} of {getTotalAnnotationsNeeded()}{" "}
              annotations completed
            </p>
            <button
              onClick={handleSubmitAnnotations}
              disabled={isSubmitting || !canSubmitAnnotations()}
              className="px-6 py-2.5 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors flex items-center gap-2"
            >
              {isSubmitting ? (
                <>
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>Submitting...</span>
                </>
              ) : (
                <span>Submit Annotations</span>
              )}
            </button>
          </div>
        </div>
      )}

      {/* Post-submission metrics: MRR, Precision, Recall, NDCG, response quality */}
      {metrics && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6 border border-green-700">
          <div className="flex items-center gap-2 mb-4">
            <CheckCircleIcon className="w-6 h-6 text-green-400" />
            <h2 className="text-xl font-semibold text-white">
              Evaluation Metrics
            </h2>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">MRR</p>
              <p className="text-2xl font-bold text-blue-400">
                {formatMetric(metrics.mrr)}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">Precision@K</p>
              <p className="text-2xl font-bold text-blue-400">
                {formatMetric(metrics.precisionAtK)}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">Recall@K</p>
              <p className="text-2xl font-bold text-blue-400">
                {formatMetric(metrics.recallAtK)}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">NDCG@K</p>
              <p className="text-2xl font-bold text-blue-400">
                {formatMetric(metrics.ndcgAtK)}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">Response Time</p>
              <p className="text-2xl font-bold text-purple-400">
                {formatResponseTime(metrics.responseTimeMs)}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center">
              <p className="text-gray-400 text-sm mb-1">Language Switching</p>
              <p
                className={`text-2xl font-bold ${metrics.hasLanguageSwitching ? "text-red-400" : "text-green-400"}`}
              >
                {metrics.hasLanguageSwitching ? "Yes" : "No"}
              </p>
            </div>
            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700 text-center col-span-2 md:col-span-2">
              <p className="text-gray-400 text-sm mb-1">Response Quality</p>
              <p
                className={`text-2xl font-bold ${getResponseQualityColor(metrics.responseQuality)}`}
              >
                {getResponseQualityOption(metrics.responseQuality)?.label ||
                  "N/A"}
              </p>
            </div>
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
      }),
    ),
    timestamp: PropTypes.string.isRequired,
    relevantDocumentIds: PropTypes.arrayOf(PropTypes.string),
  }),
  onAnnotated: PropTypes.func,
};

export default SearchResults;
