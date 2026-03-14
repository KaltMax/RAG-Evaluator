import { describe, it, expect } from "vitest";
import { sortByKey } from "../../src/utils/sortByKey";

describe("sortByKey", () => {
  const items = [
    { name: "Charlie", age: 30 },
    { name: "Alice", age: 25 },
    { name: "Bob", age: 35 },
  ];

  it("returns the original array when key is null", () => {
    expect(sortByKey(items, null)).toBe(items);
  });

  it("returns the original array when key is undefined", () => {
    expect(sortByKey(items, undefined)).toBe(items);
  });

  it("sorts strings ascending by default", () => {
    const result = sortByKey(items, "name");
    expect(result.map((i) => i.name)).toEqual(["Alice", "Bob", "Charlie"]);
  });

  it("sorts strings descending", () => {
    const result = sortByKey(items, "name", "desc");
    expect(result.map((i) => i.name)).toEqual(["Charlie", "Bob", "Alice"]);
  });

  it("sorts numbers ascending", () => {
    const result = sortByKey(items, "age", "asc");
    expect(result.map((i) => i.age)).toEqual([25, 30, 35]);
  });

  it("sorts numbers descending", () => {
    const result = sortByKey(items, "age", "desc");
    expect(result.map((i) => i.age)).toEqual([35, 30, 25]);
  });

  it("is case-insensitive for strings", () => {
    const mixed = [{ name: "banana" }, { name: "Apple" }, { name: "Cherry" }];
    const result = sortByKey(mixed, "name", "asc");
    expect(result.map((i) => i.name)).toEqual(["Apple", "banana", "Cherry"]);
  });

  it("does not mutate the original array", () => {
    const original = [...items];
    sortByKey(items, "name");
    expect(items).toEqual(original);
  });

  it("handles null values in data", () => {
    const withNull = [{ name: "Bob" }, { name: null }, { name: "Alice" }];
    const result = sortByKey(withNull, "name", "asc");
    expect(result[0].name).toBeNull();
    expect(result[2].name).toBe("Bob");
  });

  it("handles empty array", () => {
    expect(sortByKey([], "name")).toEqual([]);
  });
});
