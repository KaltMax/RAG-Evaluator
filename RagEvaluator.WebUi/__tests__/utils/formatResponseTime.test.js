import { describe, it, expect } from "vitest";
import { formatResponseTime } from "../../src/utils/formatResponseTime";

describe("formatResponseTime", () => {
  it("returns 'N/A' for null", () => {
    expect(formatResponseTime(null)).toBe("N/A");
  });

  it("returns 'N/A' for undefined", () => {
    expect(formatResponseTime(undefined)).toBe("N/A");
  });

  it("formats 0ms", () => {
    expect(formatResponseTime(0)).toBe("0ms");
  });

  it("formats milliseconds below 1000", () => {
    expect(formatResponseTime(150)).toBe("150ms");
  });

  it("formats 999ms as milliseconds", () => {
    expect(formatResponseTime(999)).toBe("999ms");
  });

  it("rounds milliseconds", () => {
    expect(formatResponseTime(150.7)).toBe("151ms");
  });

  it("formats exactly 1000ms as seconds", () => {
    expect(formatResponseTime(1000)).toBe("1.00s");
  });

  it("formats seconds with two decimal places", () => {
    expect(formatResponseTime(2500)).toBe("2.50s");
  });

  it("formats large values as seconds", () => {
    expect(formatResponseTime(65432)).toBe("65.43s");
  });
});
