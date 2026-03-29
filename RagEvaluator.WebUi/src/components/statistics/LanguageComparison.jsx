import { PropTypes } from "prop-types";
import RetrievalMetricsChart from "./RetrievalMetricsChart";
import {
  experimentDetailShape,
  colorEntryShape,
} from "../../utils/statisticsPropTypes";

function averageMetrics(queryGroups) {
  if (!queryGroups || queryGroups.length === 0) return null;
  const groups = queryGroups.filter((g) => g.metrics != null);
  if (groups.length === 0) return null;

  const keys = ["mrr", "precisionAtK", "recallAtK", "ndcgAtK"];
  const result = {};
  keys.forEach((key) => {
    const values = groups.map((g) => g.metrics[key]).filter((v) => v != null);
    if (values.length === 0) {
      result[key] = null;
    } else {
      result[key] = {
        mean: values.reduce((a, b) => a + b.mean, 0) / values.length,
        stdDev: values.reduce((a, b) => a + (b.stdDev ?? 0), 0) / values.length,
      };
    }
  });
  return result;
}

function LanguageComparison({ selectedExperiments, colorMap }) {
  const hasEn = selectedExperiments.some((exp) =>
    exp.queryGroups?.some((g) => g.language === "en"),
  );
  const hasDe = selectedExperiments.some((exp) =>
    exp.queryGroups?.some((g) => g.language === "de"),
  );

  if (!hasEn && !hasDe) return null;

  const enAccessor = (exp) =>
    averageMetrics(exp.queryGroups?.filter((g) => g.language === "en"));
  const deAccessor = (exp) =>
    averageMetrics(exp.queryGroups?.filter((g) => g.language === "de"));

  // Side-by-side retrieval metric charts filtered by query language
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {hasEn && (
        <RetrievalMetricsChart
          selectedExperiments={selectedExperiments}
          colorMap={colorMap}
          title="English Queries"
          metricsAccessor={enAccessor}
          showErrorBars={false}
        />
      )}
      {hasDe && (
        <RetrievalMetricsChart
          selectedExperiments={selectedExperiments}
          colorMap={colorMap}
          title="German Queries"
          metricsAccessor={deAccessor}
          showErrorBars={false}
        />
      )}
    </div>
  );
}

LanguageComparison.propTypes = {
  selectedExperiments: PropTypes.arrayOf(experimentDetailShape).isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
};

export default LanguageComparison;
