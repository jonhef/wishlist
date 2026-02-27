import type { PublicWishlistOrder, PublicWishlistSort } from "../../api/client";

export type PublicSortState = {
  sort: PublicWishlistSort;
  order: PublicWishlistOrder;
};

export function parsePublicSortState(searchParams: URLSearchParams): PublicSortState {
  const rawSort = searchParams.get("sort");
  const sort: PublicWishlistSort = rawSort === "added" || rawSort === "price" ? rawSort : "priority";
  const rawOrder = searchParams.get("order");
  const order: PublicWishlistOrder = rawOrder === "desc" ? "desc" : "asc";

  return { sort, order };
}

export function applySortChange(searchParams: URLSearchParams, sort: PublicWishlistSort): URLSearchParams {
  const nextParams = new URLSearchParams(searchParams);

  switch (sort) {
    case "priority":
      nextParams.delete("sort");
      nextParams.delete("order");
      break;
    case "added":
      nextParams.set("sort", sort);
      nextParams.delete("order");
      break;
    case "price":
      nextParams.set("sort", sort);
      if (!nextParams.get("order")) {
        nextParams.set("order", "asc");
      }
      break;
  }

  return nextParams;
}

export function applyOrderChange(searchParams: URLSearchParams, order: PublicWishlistOrder): URLSearchParams {
  const nextParams = new URLSearchParams(searchParams);
  nextParams.set("order", order);
  return nextParams;
}

export function buildPublicWishlistQueryKey(token: string | undefined, sortState: PublicSortState): readonly unknown[] {
  return ["public-wishlist", token, sortState.sort, sortState.sort === "price" ? sortState.order : null] as const;
}
