import type { QueryClient } from "@tanstack/react-query";
import type { Item, ItemListResult } from "../../api/client";
import { sortItems } from "./sortItems";

export function wishlistItemsQueryKey(wishlistId: string | undefined): readonly [string, string | undefined] {
  return ["wishlist-items", wishlistId] as const;
}

export function insertItemIntoItemsCache(
  queryClient: QueryClient,
  wishlistId: string,
  item: Item
): void {
  const queryKey = wishlistItemsQueryKey(wishlistId);

  queryClient.setQueryData<ItemListResult | undefined>(queryKey, (current) => {
    if (!current) {
      return { items: [item], nextCursor: null };
    }

    return {
      ...current,
      items: sortItems([item, ...current.items])
    };
  });
}

export function updateItemInItemsCache(
  queryClient: QueryClient,
  wishlistId: string,
  updatedItem: Item
): void {
  const queryKey = wishlistItemsQueryKey(wishlistId);

  queryClient.setQueryData<ItemListResult | undefined>(queryKey, (current) => {
    if (!current) {
      return current;
    }

    const nextItems = current.items.map((item) => (item.id === updatedItem.id ? updatedItem : item));

    return {
      ...current,
      items: sortItems(nextItems)
    };
  });
}

export function removeItemFromItemsCache(
  queryClient: QueryClient,
  wishlistId: string,
  itemId: number
): void {
  const queryKey = wishlistItemsQueryKey(wishlistId);

  queryClient.setQueryData<ItemListResult | undefined>(queryKey, (current) => {
    if (!current) {
      return current;
    }

    return {
      ...current,
      items: current.items.filter((item) => item.id !== itemId)
    };
  });
}
