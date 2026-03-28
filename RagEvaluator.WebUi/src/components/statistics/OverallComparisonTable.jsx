import { PropTypes } from "prop-types";
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

function OverallComparisonTable({ selectedExperiments, colorMap }) {
  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6 border border-gray-700">
      <h2 className="text-lg font-semibold text-white mb-4">
        Overall Comparison
      </h2>
      {/* Horizontally scrollable table with sticky metric column */}
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
                  <p className="text-[10px] text-gray-300 truncate mt-0.5">
                    {exp.embeddingModel} | {exp.chunkingStrategy} |{" "}
                    {exp.promptTemplate}
                  </p>
                </th>
              ))}
            </tr>
          </thead>
          {/* Metric rows with best value highlighted per row */}
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

OverallComparisonTable.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
};

export default OverallComparisonTable;
