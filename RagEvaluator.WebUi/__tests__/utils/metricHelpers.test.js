import { describe, it, expect } from "vitest";
import {
  findBestIndices,
  formatCell,
  getMeanValue,
  METRICS,
} from "../../src/utils/metricHelpers";

describe("findBestIndices", () => {
  it("finds the highest value index when higher is true", () => {
    expect(findBestIndices([0.5, 0.9, 0.7], true)).toEqual(new Set([1]));
  });

  it("finds the lowest value index when higher is false", () => {
    expect(findBestIndices([300, 100, 200], false)).toEqual(new Set([1]));
  });

  it("returns empty set for all null values", () => {
    expect(findBestIndices([null, null, null], true)).toEqual(new Set());
  });

  it("skips null values", () => {
    expect(findBestIndices([null, 0.5, null], true)).toEqual(new Set([1]));
  });

  it("handles single element", () => {
    expect(findBestIndices([0.8], true)).toEqual(new Set([0]));
  });

  it("handles empty array", () => {
    expect(findBestIndices([], true)).toEqual(new Set());
  });

  it("returns all indices for tied values", () => {
    expect(findBestIndices([0.9, 0.9, 0.5], true)).toEqual(new Set([0, 1]));
  });
});

describe("formatCell", () => {
  const mrrMetric = METRICS.find((m) => m.key === "mrr");
  const responseTimeMetric = METRICS.find((m) => m.key === "responseTimeMs");
  const langSwitchMetric = METRICS.find(
    (m) => m.key === "languageSwitchingRate",
  );

  it("returns 'N/A' for null value", () => {
    expect(formatCell(mrrMetric, null)).toBe("N/A");
  });

  it("returns 'N/A' for undefined value", () => {
    expect(formatCell(mrrMetric, undefined)).toBe("N/A");
  });

  it("formats a plain metric value", () => {
    expect(formatCell(mrrMetric, 0.85)).toBe("0.850");
  });

  it("formats metric with mean and stdDev", () => {
    const result = formatCell(mrrMetric, { mean: 0.85, stdDev: 0.05 });
    expect(result).toBe("0.850 ± 0.050");
  });

  it("formats metric with mean and no stdDev", () => {
    const result = formatCell(mrrMetric, { mean: 0.85, stdDev: null });
    expect(result).toBe("0.850");
  });

  it("formats response time in milliseconds", () => {
    expect(formatCell(responseTimeMetric, 150)).toBe("150ms");
  });

  it("formats response time in seconds", () => {
    expect(formatCell(responseTimeMetric, 2500)).toBe("2.50s");
  });

  it("formats response time with mean and stdDev", () => {
    const result = formatCell(responseTimeMetric, {
      mean: 2500,
      stdDev: 300,
    });
    expect(result).toBe("2.50s ± 300ms");
  });

  it("formats language switching rate as percentage", () => {
    expect(formatCell(langSwitchMetric, 0.25)).toBe("25.0%");
  });
});

describe("getMeanValue", () => {
  const mrrMetric = METRICS.find((m) => m.key === "mrr");
  const langSwitchMetric = METRICS.find(
    (m) => m.key === "languageSwitchingRate",
  );

  it("returns null when metrics is null", () => {
    expect(getMeanValue(mrrMetric, null)).toBeNull();
  });

  it("returns null when metric key is missing", () => {
    expect(getMeanValue(mrrMetric, {})).toBeNull();
  });

  it("returns the mean from an aggregate object", () => {
    expect(getMeanValue(mrrMetric, { mrr: { mean: 0.85, stdDev: 0.1 } })).toBe(
      0.85,
    );
  });

  it("returns null when mean is missing", () => {
    expect(getMeanValue(mrrMetric, { mrr: { stdDev: 0.1 } })).toBeNull();
  });

  it("returns the raw value for languageSwitchingRate", () => {
    expect(
      getMeanValue(langSwitchMetric, { languageSwitchingRate: 0.15 }),
    ).toBe(0.15);
  });
});
