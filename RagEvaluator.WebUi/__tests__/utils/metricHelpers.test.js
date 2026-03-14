import { describe, it, expect } from "vitest";
import {
  findBestIndex,
  formatCell,
  getMeanValue,
  METRICS,
} from "../../src/utils/metricHelpers";

describe("findBestIndex", () => {
  it("finds the highest value index when higher is true", () => {
    expect(findBestIndex([0.5, 0.9, 0.7], true)).toBe(1);
  });

  it("finds the lowest value index when higher is false", () => {
    expect(findBestIndex([300, 100, 200], false)).toBe(1);
  });

  it("returns -1 for all null values", () => {
    expect(findBestIndex([null, null, null], true)).toBe(-1);
  });

  it("skips null values", () => {
    expect(findBestIndex([null, 0.5, null], true)).toBe(1);
  });

  it("handles single element", () => {
    expect(findBestIndex([0.8], true)).toBe(0);
  });

  it("handles empty array", () => {
    expect(findBestIndex([], true)).toBe(-1);
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
