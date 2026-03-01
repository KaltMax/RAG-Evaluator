import { PropTypes } from "prop-types";
import { formatMetric } from "../../utils/formatMetric";
import { formatResponseTime } from "../../utils/formatResponseTime";

const METRICS = [
  { key: "mrr", label: "MRR", higher: true },
  { key: "precisionAtK", label: "Precision@K", higher: true },
  { key: "recallAtK", label: "Recall@K", higher: true },
  { key: "ndcgAtK", label: "NDCG@K", higher: true },
  { key: "responseTimeMs", label: "Response Time", higher: false },
  { key: "languageSwitchingRate", label: "Lang. Switching", higher: false },
];

function findBestIndex(values, higher) {
  let bestIdx = -1;
  let bestVal = higher ? -Infinity : Infinity;
  values.forEach((v, i) => {
    if (v == null) return;
    if (higher ? v > bestVal : v < bestVal) {
      bestVal = v;
      bestIdx = i;
    }
  });
  return bestIdx;
}

function formatCell(metric, value) {
  if (!value) return "N/A";
  if (metric.key === "responseTimeMs") {
    const std =
      value.stdDev == null ? "" : ` \u00b1 ${formatResponseTime(value.stdDev)}`;
    return `${formatResponseTime(value.mean)}${std}`;
  }
  if (metric.key === "languageSwitchingRate") {
    return value == null ? "N/A" : `${(value * 100).toFixed(1)}%`;
  }
  const std =
    value.stdDev == null ? "" : ` \u00b1 ${formatMetric(value.stdDev)}`;
  return `${formatMetric(value.mean)}${std}`;
}

function getMeanValue(metric, overallMetrics) {
  if (!overallMetrics) return null;
  const val = overallMetrics[metric.key];
  if (val == null) return null;
  if (metric.key === "languageSwitchingRate") return val;
  return val.mean ?? null;
}

function OverallComparisonTable({ selectedExperiments, colorMap }) {
  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6">
      <h2 className="text-lg font-semibold text-white mb-4">
        Overall Comparison
      </h2>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-700">
              <th className="text-left text-gray-400 font-medium py-2 pr-4 sticky left-0 bg-[#2D2D2D] z-10 min-w-[140px]">
                Metric
              </th>
              {selectedExperiments.map((exp) => (
                <th
                  key={exp.id}
                  className="text-center py-2 px-3 min-w-[160px]"
                >
                  <div className="flex items-center justify-center gap-2">
                    <span
                      className="w-2.5 h-2.5 rounded-full shrink-0"
                      style={{ backgroundColor: colorMap[exp.id]?.hex }}
                    />
                    <span className="text-white font-medium truncate text-xs">
                      {exp.name}
                    </span>
                  </div>
                  <p className="text-[10px] text-gray-500 truncate mt-0.5">
                    {exp.embeddingModel} | {exp.chunkingStrategy}
                  </p>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {METRICS.map((metric) => {
              const means = selectedExperiments.map((exp) =>
                getMeanValue(metric, exp.overallMetrics),
              );
              const bestIdx = findBestIndex(means, metric.higher);

              return (
                <tr key={metric.key} className="border-b border-gray-800">
                  <td className="text-gray-300 font-medium py-2.5 pr-4 sticky left-0 bg-[#2D2D2D] z-10">
                    {metric.label}
                  </td>
                  {selectedExperiments.map((exp, i) => {
                    const val =
                      metric.key === "languageSwitchingRate"
                        ? exp.overallMetrics?.languageSwitchingRate
                        : exp.overallMetrics?.[metric.key];
                    const isBest = i === bestIdx;
                    return (
                      <td
                        key={exp.id}
                        className={`text-center py-2.5 px-3 font-mono text-xs ${
                          isBest ? "text-white font-bold" : "text-gray-400"
                        }`}
                        style={
                          isBest
                            ? {
                                borderLeft: `3px solid ${colorMap[exp.id]?.hex}`,
                              }
                            : undefined
                        }
                      >
                        {formatCell(metric, val)}
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const metricAggregateShape = PropTypes.shape({
  mean: PropTypes.number.isRequired,
  stdDev: PropTypes.number,
});

const colorEntryShape = PropTypes.shape({
  hex: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
});

const experimentDetailShape = PropTypes.shape({
  id: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  embeddingModel: PropTypes.string,
  chunkingStrategy: PropTypes.string,
  overallMetrics: PropTypes.shape({
    mrr: metricAggregateShape,
    precisionAtK: metricAggregateShape,
    recallAtK: metricAggregateShape,
    ndcgAtK: metricAggregateShape,
    responseTimeMs: metricAggregateShape,
    languageSwitchingRate: PropTypes.number,
  }),
});

OverallComparisonTable.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
};

export default OverallComparisonTable;
