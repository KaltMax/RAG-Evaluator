import { describe, it, expect } from "vitest";
import { formatLanguage } from "../../src/utils/formatLanguage";

describe("formatLanguage", () => {
  it("returns 'English' for 'en'", () => {
    expect(formatLanguage("en")).toBe("English");
  });

  it("returns 'German' for 'de'", () => {
    expect(formatLanguage("de")).toBe("German");
  });

  it("returns the code itself for unknown language codes", () => {
    expect(formatLanguage("fr")).toBe("fr");
  });

  it("returns '-' for null", () => {
    expect(formatLanguage(null)).toBe("-");
  });

  it("returns '-' for undefined", () => {
    expect(formatLanguage(undefined)).toBe("-");
  });

  it("returns '-' for empty string", () => {
    expect(formatLanguage("")).toBe("-");
  });
});
