import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ErrorBar,
  ResponsiveContainer,
} from "recharts";
import { PropTypes } from "prop-types";
import { formatMetric } from "../../utils/formatMetric";
import {
  experimentDetailShape,
  colorEntryShape,
} from "../../utils/statisticsPropTypes";

const RETRIEVAL_METRICS = [
  { key: "mrr", label: "MRR" },
  { key: "precisionAtK", label: "Precision@K" },
  { key: "recallAtK", label: "Recall@K" },
  { key: "ndcgAtK", label: "NDCG@K" },
];

const defaultAccessor = (exp) => exp.overallMetrics;

const legendFormatter = (value) => (
  <span className="text-gray-300 text-xs">{value}</span>
);

function CustomTooltip({ active, payload, label }) {
  if (!active || !payload?.length) return null;
  return (
    <div className="bg-[#1F1F1F] border border-gray-700 rounded-lg p-3 shadow-lg">
      <p className="text-white font-medium text-sm mb-2">{label}</p>
      {payload.map((entry) => (
        <div
          key={entry.dataKey}
          className="flex items-center gap-2 text-xs mb-1"
        >
          <span
            className="w-2.5 h-2.5 rounded-full"
            style={{ backgroundColor: entry.color }}
          />
          <span className="text-gray-300">{entry.name}:</span>
          <span className="text-white font-mono">
            {formatMetric(entry.value)}
          </span>
        </div>
      ))}
    </div>
  );
}

CustomTooltip.propTypes = {
  active: PropTypes.bool,
  payload: PropTypes.arrayOf(
    PropTypes.shape({
      dataKey: PropTypes.string,
      name: PropTypes.string,
      value: PropTypes.number,
      color: PropTypes.string,
    }),
  ),
  label: PropTypes.string,
};

function RetrievalMetricsChart({
  selectedExperiments,
  colorMap,
  title,
  metricsAccessor,
  showErrorBars = true,
}) {
  const accessor = metricsAccessor || defaultAccessor;
  const data = RETRIEVAL_METRICS.map((metric) => {
    const row = { name: metric.label };
    selectedExperiments.forEach((exp) => {
      const val = accessor(exp)?.[metric.key];
      const mean = val?.mean ?? 0;
      const stdDev = val?.stdDev ?? 0;
      row[exp.id] = mean;
      row[`${exp.id}_err`] =
        showErrorBars && stdDev > 0
          ? [Math.min(stdDev, mean), Math.min(stdDev, 1 - mean)]
          : null;
    });
    return row;
  });

  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6 border border-gray-700">
      <h2 className="text-lg font-semibold text-white mb-4">
        {title || "Retrieval Metrics"}
      </h2>
      {/* Grouped bar chart with one bar per experiment and stddev error bars */}
      <ResponsiveContainer width="100%" height={350}>
        <BarChart
          data={data}
          margin={{ top: 5, right: 30, left: 0, bottom: 5 }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
          <XAxis dataKey="name" tick={{ fill: "#9ca3af", fontSize: 12 }} />
          <YAxis
            domain={[0, 1]}
            allowDataOverflow={true}
            tick={{ fill: "#9ca3af", fontSize: 12 }}
          />
          <Tooltip content={<CustomTooltip />} />
          <Legend
            wrapperStyle={{ color: "#d1d5db", fontSize: 12 }}
            formatter={legendFormatter}
          />
          {selectedExperiments.map((exp) => (
            <Bar
              key={exp.id}
              dataKey={exp.id}
              name={exp.name}
              fill={colorMap[exp.id]?.hex}
              radius={[2, 2, 0, 0]}
            >
              <ErrorBar
                dataKey={`${exp.id}_err`}
                width={4}
                stroke="#ffffff"
                strokeWidth={2}
              />
            </Bar>
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

RetrievalMetricsChart.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
  title: PropTypes.string,
  metricsAccessor: PropTypes.func,
  showErrorBars: PropTypes.bool,
};

export default RetrievalMetricsChart;
