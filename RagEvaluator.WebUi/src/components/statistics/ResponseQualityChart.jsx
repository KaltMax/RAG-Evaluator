import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import { PropTypes } from "prop-types";
import { experimentDetailShape } from "../../utils/statisticsPropTypes";

const QUALITY_SEGMENTS = [
  { key: "CorrectAndComplete", label: "Correct", color: "#22c55e" },
  { key: "VagueOrIncomplete", label: "Vague", color: "#eab308" },
  { key: "Incorrect", label: "Incorrect", color: "#f97316" },
  { key: "Hallucinated", label: "Hallucinated", color: "#ef4444" },
];

const legendFormatter = (value) => (
  <span className="text-gray-300 text-xs">{value}</span>
);

function buildChartData(selectedExperiments) {
  return selectedExperiments.map((exp) => {
    const dist = exp.overallMetrics?.responseQualityDistribution;
    const total = dist ? Object.values(dist).reduce((a, b) => a + b, 0) : 0;
    const row = { name: exp.name };
    QUALITY_SEGMENTS.forEach((seg) => {
      const count = dist?.[seg.key] ?? 0;
      row[seg.key] = total > 0 ? (count / total) * 100 : 0;
      row[`${seg.key}_count`] = count;
    });
    row._total = total;
    return row;
  });
}

function CustomTooltip({ active, payload, label }) {
  if (!active || !payload?.length) return null;
  const total = payload[0]?.payload?._total ?? 0;
  return (
    <div className="bg-[#1F1F1F] border border-gray-700 rounded-lg p-3 shadow-lg">
      <p className="text-white font-medium text-sm mb-2">{label}</p>
      {payload.map((entry) => {
        const countKey = `${entry.dataKey}_count`;
        const count = entry.payload[countKey] ?? 0;
        return (
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
              {entry.value.toFixed(1)}% ({count}/{total})
            </span>
          </div>
        );
      })}
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
      payload: PropTypes.object,
    }),
  ),
  label: PropTypes.string,
};

function ResponseQualityChart({ selectedExperiments }) {
  const data = buildChartData(selectedExperiments);

  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6">
      <h2 className="text-lg font-semibold text-white mb-4">
        Response Quality Distribution
      </h2>
      {/* Horizontal stacked bars: Correct / Vague / Incorrect / Hallucinated */}
      <ResponsiveContainer
        width="100%"
        height={Math.max(200, selectedExperiments.length * 50 + 80)}
      >
        <BarChart
          data={data}
          layout="vertical"
          margin={{ top: 5, right: 30, left: 0, bottom: 5 }}
        >
          <XAxis
            type="number"
            domain={[0, 100]}
            tick={{ fill: "#9ca3af", fontSize: 12 }}
            unit="%"
          />
          <YAxis
            type="category"
            dataKey="name"
            tick={{ fill: "#9ca3af", fontSize: 11 }}
            width={150}
          />
          <Tooltip content={<CustomTooltip />} />
          <Legend
            wrapperStyle={{ fontSize: 12 }}
            formatter={legendFormatter}
          />
          {QUALITY_SEGMENTS.map((seg) => (
            <Bar
              key={seg.key}
              dataKey={seg.key}
              name={seg.label}
              stackId="quality"
              fill={seg.color}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

ResponseQualityChart.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
};

export default ResponseQualityChart;
