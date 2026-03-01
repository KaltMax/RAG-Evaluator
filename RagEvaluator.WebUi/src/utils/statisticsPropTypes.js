import { PropTypes } from "prop-types";

export const metricAggregateShape = PropTypes.shape({
  mean: PropTypes.number.isRequired,
  stdDev: PropTypes.number,
});

export const colorEntryShape = PropTypes.shape({
  hex: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
});

export const queryGroupShape = PropTypes.shape({
  question: PropTypes.string.isRequired,
  language: PropTypes.string.isRequired,
  metrics: PropTypes.shape({
    mrr: metricAggregateShape,
    precisionAtK: metricAggregateShape,
    recallAtK: metricAggregateShape,
    ndcgAtK: metricAggregateShape,
    responseTimeMs: metricAggregateShape,
    languageSwitchingRate: PropTypes.number,
  }),
});

export const experimentDetailShape = PropTypes.shape({
  id: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  embeddingModel: PropTypes.string,
  chunkingStrategy: PropTypes.string,
  queryGroups: PropTypes.arrayOf(queryGroupShape),
  overallMetrics: PropTypes.shape({
    mrr: metricAggregateShape,
    precisionAtK: metricAggregateShape,
    recallAtK: metricAggregateShape,
    ndcgAtK: metricAggregateShape,
    responseTimeMs: metricAggregateShape,
    languageSwitchingRate: PropTypes.number,
    responseQualityDistribution: PropTypes.objectOf(PropTypes.number),
  }),
});

export const experimentSummaryShape = PropTypes.shape({
  id: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  status: PropTypes.string.isRequired,
  embeddingModel: PropTypes.string,
  chunkingStrategy: PropTypes.string,
  promptTemplate: PropTypes.string,
  progress: PropTypes.shape({
    total: PropTypes.number.isRequired,
    completed: PropTypes.number.isRequired,
    annotated: PropTypes.number.isRequired,
  }).isRequired,
});
