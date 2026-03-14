import { describe, it, expect } from "vitest";
import { formatFileSize } from "../../src/utils/formatFileSize";

describe("formatFileSize", () => {
  it("returns '-' for 0", () => {
    expect(formatFileSize(0)).toBe("-");
  });

  it("returns '-' for null", () => {
    expect(formatFileSize(null)).toBe("-");
  });

  it("returns '-' for undefined", () => {
    expect(formatFileSize(undefined)).toBe("-");
  });

  it("formats bytes", () => {
    expect(formatFileSize(500)).toBe("500 B");
  });

  it("formats 1023 bytes as bytes", () => {
    expect(formatFileSize(1023)).toBe("1023 B");
  });

  it("formats exactly 1 KB", () => {
    expect(formatFileSize(1024)).toBe("1.00 KB");
  });

  it("formats kilobytes", () => {
    expect(formatFileSize(5120)).toBe("5.00 KB");
  });

  it("formats values just below 1 MB as KB", () => {
    expect(formatFileSize(1024 * 1024 - 1)).toBe("1024.00 KB");
  });

  it("formats exactly 1 MB", () => {
    expect(formatFileSize(1024 * 1024)).toBe("1.00 MB");
  });

  it("formats megabytes", () => {
    expect(formatFileSize(2.5 * 1024 * 1024)).toBe("2.50 MB");
  });
});
