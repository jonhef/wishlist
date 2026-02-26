import type { Item } from "../../api/client";

export function computeItemsVersion(items: readonly Pick<Item, "id" | "priority" | "updatedAtUtc">[]): string {
  if (items.length === 0) {
    return "empty";
  }

  return items
    .map((item) => `${item.id}:${item.priority}:${item.updatedAtUtc}`)
    .join("|");
}

export function hasItemsVersionChanged(initialVersion: string, currentVersion: string): boolean {
  return initialVersion !== currentVersion;
}
