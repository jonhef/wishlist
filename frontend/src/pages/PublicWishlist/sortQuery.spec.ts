import { describe, expect, it } from "vitest";
import { applyOrderChange, applySortChange, buildPublicWishlistQueryKey, parsePublicSortState } from "./sortQuery";

describe("public sort query", () => {
  it("parses defaults", () => {
    const state = parsePublicSortState(new URLSearchParams());

    expect(state.sort).toBe("priority");
    expect(state.order).toBe("asc");
  });

  it("sets price sort with default asc order", () => {
    const params = applySortChange(new URLSearchParams("sort=added"), "price");

    expect(params.get("sort")).toBe("price");
    expect(params.get("order")).toBe("asc");
  });

  it("removes order when switching away from price", () => {
    const params = applySortChange(new URLSearchParams("sort=price&order=desc"), "added");

    expect(params.get("sort")).toBe("added");
    expect(params.has("order")).toBe(false);
  });

  it("includes order in query key for price sort", () => {
    const priceKey = buildPublicWishlistQueryKey("token", { sort: "price", order: "desc" });
    const addedKey = buildPublicWishlistQueryKey("token", { sort: "added", order: "asc" });

    expect(priceKey).toEqual(["public-wishlist", "token", "price", "desc"]);
    expect(addedKey).toEqual(["public-wishlist", "token", "added", null]);
  });

  it("updates order param", () => {
    const params = applyOrderChange(new URLSearchParams("sort=price&order=asc"), "desc");

    expect(params.get("order")).toBe("desc");
  });
});
