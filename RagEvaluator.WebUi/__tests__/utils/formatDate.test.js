import { describe, it, expect } from "vitest";
import { formatDate } from "../../src/utils/formatDate";

describe("formatDate", () => {
  it("returns '-' for null", () => {
    expect(formatDate(null)).toBe("-");
  });

  it("returns '-' for undefined", () => {
    expect(formatDate(undefined)).toBe("-");
  });

  it("returns '-' for empty string", () => {
    expect(formatDate("")).toBe("-");
  });

  it("formats a valid ISO date string", () => {
    const result = formatDate("2025-06-15T10:30:00Z");
    expect(result).not.toBe("-");
    expect(typeof result).toBe("string");
    expect(result.length).toBeGreaterThan(0);
  });

  it("formats a date-only string", () => {
    const result = formatDate("2025-01-01");
    expect(result).not.toBe("-");
  });
});
