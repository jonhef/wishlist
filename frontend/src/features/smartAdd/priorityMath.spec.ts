import Decimal from "decimal.js";
import { describe, expect, it } from "vitest";
import { computePriorityForPosition, formatPriority, midpoint, minusStep, plusStep } from "./priorityMath";

describe("priorityMath", () => {
  it("computes midpoint strictly between values", () => {
    const value = midpoint("2048.0000000000", "1024.0000000000");
    const decimalValue = new Decimal(value);
    expect(decimalValue.lessThan("2048.0000000000")).toBe(true);
    expect(decimalValue.greaterThan("1024.0000000000")).toBe(true);
  });

  it("keeps fixed scale formatting without exponent", () => {
    const value = formatPriority(new Decimal("123456789.123456789"));
    expect(value).toBe("123456789.1234567890");
    expect(value.includes("e")).toBe(false);
  });

  it("handles top and bottom inserts with step", () => {
    expect(plusStep("1000.0000000000")).toBe("2024.0000000000");
    expect(minusStep("1000.0000000000")).toBe("-24.0000000000");
  });

  it("returns zero priority for empty list", () => {
    expect(computePriorityForPosition([], 0)).toBe("0.0000000000");
  });
});
