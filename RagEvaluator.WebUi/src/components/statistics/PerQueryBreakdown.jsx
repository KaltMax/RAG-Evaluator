import { useState } from "react";
import { PropTypes } from "prop-types";
import { ChevronDownIcon, ChevronUpIcon } from "@heroicons/react/24/outline";
import { formatLanguage } from "../../utils/formatLanguage";
import {
  METRICS,
  findBestIndex,
  formatCell,
  getMeanValue,
} from "../../utils/metricHelpers";
import {
  experimentDetailShape,
  colorEntryShape,
} from "../../utils/statisticsPropTypes";

function getUniqueQuestions(selectedExperiments) {
  const questionMap = new Map();
  selectedExperiments.forEach((exp) => {
    exp.queryGroups?.forEach((qg) => {
      if (!questionMap.has(qg.question)) {
        questionMap.set(qg.question, {
          question: qg.question,
          language: qg.language,
        });
      }
    });
  });
  return [...questionMap.values()];
}

function getQueryGroupForQuestion(exp, question) {
  return exp.queryGroups?.find((qg) => qg.question === question);
}

function PerQueryBreakdown({ selectedExperiments, colorMap }) {
  const [expandedQuestions, setExpandedQuestions] = useState(new Set());
  const questions = getUniqueQuestions(selectedExperiments);

  const toggleQuestion = (question) => {
    setExpandedQuestions((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(question)) newSet.delete(question);
      else newSet.add(question);
      return newSet;
    });
  };

  if (questions.length === 0) return null;

  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6">
      <h2 className="text-lg font-semibold text-white mb-4">
        Per-Query Breakdown
      </h2>
      {/* Expandable accordion: one row per unique question across experiments */}
      <div className="space-y-2">
        {questions.map(({ question, language }) => {
          const isExpanded = expandedQuestions.has(question);

          return (
            <div
              key={question}
              className="border border-gray-700 rounded-lg overflow-hidden"
            >
              <button
                onClick={() => toggleQuestion(question)}
                className="w-full px-4 py-3 flex items-center gap-3 hover:bg-[#353535] transition-colors"
              >
                <div className="flex-1 min-w-0 text-left">
                  <p className="text-sm text-white truncate">{question}</p>
                </div>
                <span className="px-2 py-0.5 rounded text-[10px] font-medium bg-gray-700 text-gray-300 shrink-0">
                  {formatLanguage(language)}
                </span>
                {isExpanded ? (
                  <ChevronUpIcon className="w-4 h-4 text-gray-400 shrink-0" />
                ) : (
                  <ChevronDownIcon className="w-4 h-4 text-gray-400 shrink-0" />
                )}
              </button>

              {/* Expanded: per-experiment comparison table for this question */}
              {isExpanded && (
                <div className="border-t border-gray-700 px-4 py-3 overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead>
                      <tr className="border-b border-gray-700">
                        <th className="text-left text-gray-400 font-medium py-1.5 pr-3 min-w-[120px]">
                          Metric
                        </th>
                        {selectedExperiments.map((exp) => (
                          <th
                            key={exp.id}
                            className="text-center py-1.5 px-2 min-w-[130px]"
                          >
                            <div className="flex items-center justify-center gap-1.5">
                              <span
                                className="w-2 h-2 rounded-full"
                                style={{
                                  backgroundColor: colorMap[exp.id]?.hex,
                                }}
                              />
                              <span className="text-white font-medium truncate">
                                {exp.name}
                              </span>
                            </div>
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {METRICS.map((metric) => {
                        const means = selectedExperiments.map((exp) => {
                          const qg = getQueryGroupForQuestion(exp, question);
                          return getMeanValue(metric, qg?.metrics);
                        });
                        const bestIdx = findBestIndex(means, metric.higher);

                        return (
                          <tr
                            key={metric.key}
                            className="border-b border-gray-800"
                          >
                            <td className="text-gray-300 font-medium py-1.5 pr-3">
                              {metric.label}
                            </td>
                            {selectedExperiments.map((exp, i) => {
                              const qg = getQueryGroupForQuestion(
                                exp,
                                question,
                              );
                              const val =
                                metric.key === "languageSwitchingRate"
                                  ? qg?.metrics?.languageSwitchingRate
                                  : qg?.metrics?.[metric.key];
                              const isBest = i === bestIdx;
                              return (
                                <td
                                  key={exp.id}
                                  className={`text-center py-1.5 px-2 font-mono ${
                                    isBest
                                      ? "text-white font-bold"
                                      : "text-gray-400"
                                  }`}
                                  style={
                                    isBest
                                      ? {
                                          borderLeft: `3px solid ${colorMap[exp.id]?.hex}`,
                                        }
                                      : undefined
                                  }
                                >
                                  {qg?.metrics
                                    ? formatCell(metric, val)
                                    : "N/A"}
                                </td>
                              );
                            })}
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

PerQueryBreakdown.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
};

export default PerQueryBreakdown;
