import { formatMetric } from "./formatMetric";
import { formatResponseTime } from "./formatResponseTime";

export const METRICS = [
  { key: "mrr", label: "MRR", higher: true },
  { key: "precisionAtK", label: "Precision@K", higher: true },
  { key: "recallAtK", label: "Recall@K", higher: true },
  { key: "ndcgAtK", label: "NDCG@K", higher: true },
  { key: "responseTimeMs", label: "Response Time", higher: false },
  { key: "languageSwitchingRate", label: "Lang. Switching", higher: false },
];

export function findBestIndex(values, higher) {
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

export function formatCell(metric, value) {
  if (value == null) return "N/A";
  if (metric.key === "responseTimeMs") {
    if (typeof value === "object") {
      const std =
        value.stdDev == null
          ? ""
          : ` ± ${formatResponseTime(value.stdDev)}`;
      return `${formatResponseTime(value.mean)}${std}`;
    }
    return formatResponseTime(value);
  }
  if (metric.key === "languageSwitchingRate") {
    return `${(value * 100).toFixed(1)}%`;
  }
  if (typeof value === "object" && value.mean != null) {
    const std =
      value.stdDev == null ? "" : ` ± ${formatMetric(value.stdDev)}`;
    return `${formatMetric(value.mean)}${std}`;
  }
  return formatMetric(value);
}

export function getMeanValue(metric, metrics) {
  if (!metrics) return null;
  const val = metrics[metric.key];
  if (val == null) return null;
  if (metric.key === "languageSwitchingRate") return val;
  return val.mean ?? null;
}
