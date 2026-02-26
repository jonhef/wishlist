import Decimal from "decimal.js";
import type { Item } from "../../api/client";

function parseTimestamp(value?: string): number {
  if (!value) {
    return 0;
  }

  const timestamp = new Date(value).getTime();
  return Number.isFinite(timestamp) ? timestamp : 0;
}

export function sortItems<T extends Pick<Item, "id" | "priority" | "createdAtUtc">>(items: readonly T[]): T[] {
  return [...items].sort((left, right) => {
    const priorityOrder = new Decimal(right.priority).cmp(new Decimal(left.priority));
    if (priorityOrder !== 0) {
      return priorityOrder;
    }

    const createdAtOrder = parseTimestamp(right.createdAtUtc) - parseTimestamp(left.createdAtUtc);
    if (createdAtOrder !== 0) {
      return createdAtOrder;
    }

    return right.id - left.id;
  });
}
