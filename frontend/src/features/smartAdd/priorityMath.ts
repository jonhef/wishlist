import Decimal from "decimal.js";

export const DEFAULT_PRIORITY_STEP = new Decimal(1024);
export const DEFAULT_PRIORITY_SCALE = 10;

export function parsePriority(value: string): Decimal {
  return new Decimal(value);
}

export function formatPriority(value: Decimal, scale = DEFAULT_PRIORITY_SCALE): string {
  return value.toFixed(scale);
}

export function midpoint(left: string, right: string): string {
  const mid = parsePriority(left).plus(parsePriority(right)).dividedBy(2);
  return formatPriority(mid);
}

export function plusStep(base: string, step = DEFAULT_PRIORITY_STEP): string {
  return formatPriority(parsePriority(base).plus(step));
}

export function minusStep(base: string, step = DEFAULT_PRIORITY_STEP): string {
  return formatPriority(parsePriority(base).minus(step));
}

export function computePriorityForPosition(
  priorities: readonly string[],
  position: number,
  step = DEFAULT_PRIORITY_STEP
): string {
  if (priorities.length === 0) {
    return formatPriority(new Decimal(0));
  }

  if (position <= 0) {
    return plusStep(priorities[0], step);
  }

  if (position >= priorities.length) {
    return minusStep(priorities[priorities.length - 1], step);
  }

  return midpoint(priorities[position - 1], priorities[position]);
}
