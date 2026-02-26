import { describe, expect, it } from "vitest";
import { sortItems } from "./sortItems";

describe("sortItems", () => {
  it("sorts by priority descending", () => {
    const items = sortItems([
      { id: 1, priority: "10", createdAtUtc: "2026-01-01T00:00:00.000Z" },
      { id: 2, priority: "20", createdAtUtc: "2026-01-01T00:00:00.000Z" }
    ]);

    expect(items.map((item) => item.id)).toEqual([2, 1]);
  });

  it("uses createdAtUtc desc when priorities are equal", () => {
    const items = sortItems([
      { id: 1, priority: "20", createdAtUtc: "2026-01-01T00:00:00.000Z" },
      { id: 2, priority: "20", createdAtUtc: "2026-01-02T00:00:00.000Z" }
    ]);

    expect(items.map((item) => item.id)).toEqual([2, 1]);
  });

  it("uses id desc as last tie-breaker", () => {
    const items = sortItems([
      { id: 1, priority: "20", createdAtUtc: "2026-01-01T00:00:00.000Z" },
      { id: 2, priority: "20", createdAtUtc: "2026-01-01T00:00:00.000Z" }
    ]);

    expect(items.map((item) => item.id)).toEqual([2, 1]);
  });
});
