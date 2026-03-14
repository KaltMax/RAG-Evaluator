import { describe, it, expect } from "vitest";
import { formatMetric } from "../../src/utils/formatMetric";

describe("formatMetric", () => {
  it("returns 'N/A' for null", () => {
    expect(formatMetric(null)).toBe("N/A");
  });

  it("returns 'N/A' for undefined", () => {
    expect(formatMetric(undefined)).toBe("N/A");
  });

  it("formats 0 to three decimal places", () => {
    expect(formatMetric(0)).toBe("0.000");
  });

  it("formats a decimal value to three places", () => {
    expect(formatMetric(0.85432)).toBe("0.854");
  });

  it("formats an integer to three decimal places", () => {
    expect(formatMetric(1)).toBe("1.000");
  });

  it("rounds correctly", () => {
    expect(formatMetric(0.9999)).toBe("1.000");
  });
});
