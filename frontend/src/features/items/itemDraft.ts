import type { CreateItemRequest, Item, UpdateItemRequest } from "../../api/client";
import { majorStringToMinor, minorToMajorString, normalizeCurrency } from "./currency";

export type ItemDraft = {
  name: string;
  url: string;
  priceAmount: string;
  priceCurrency: string;
  notes: string;
};

export const emptyItemDraft: ItemDraft = {
  name: "",
  url: "",
  priceAmount: "",
  priceCurrency: "",
  notes: ""
};

export function itemDraftFromItem(item: Item): ItemDraft {
  const normalizedCurrency = normalizeCurrency(item.priceCurrency);
  return {
    name: item.name,
    url: item.url ?? "",
    priceAmount: item.priceAmount !== null && normalizedCurrency
      ? minorToMajorString(item.priceAmount, normalizedCurrency)
      : "",
    priceCurrency: item.priceCurrency ?? "",
    notes: item.notes ?? ""
  };
}

export function hasUnsavedItemDraft(draft: ItemDraft): boolean {
  return draft.name.trim().length > 0
    || draft.url.trim().length > 0
    || draft.priceAmount.trim().length > 0
    || draft.priceCurrency.trim().length > 0
    || draft.notes.trim().length > 0;
}

export function buildCreateItemPayload(draft: ItemDraft, priority?: string | null): CreateItemRequest {
  const price = resolvePriceFields(draft);

  return {
    name: draft.name.trim(),
    url: draft.url.trim() || null,
    priceAmount: price.priceAmount,
    priceCurrency: price.priceCurrency,
    priority: normalizePriority(priority),
    notes: draft.notes.trim() || null
  };
}

export function buildPatchItemPayload(draft: ItemDraft, priority?: string | null): UpdateItemRequest {
  const price = resolvePriceFields(draft);

  return {
    name: draft.name.trim(),
    url: draft.url.trim() || null,
    priceAmount: price.priceAmount,
    priceCurrency: price.priceCurrency,
    priority: normalizePriority(priority),
    notes: draft.notes.trim() || null
  };
}

function resolvePriceFields(draft: ItemDraft): { priceAmount: number | null; priceCurrency: string | null } {
  const hasAmount = draft.priceAmount.trim().length > 0;

  if (!hasAmount) {
    return {
      priceAmount: null,
      priceCurrency: null
    };
  }

  const normalizedCurrency = normalizeCurrency(draft.priceCurrency) ?? "USD";
  const normalizedAmount = majorStringToMinor(draft.priceAmount, normalizedCurrency);

  return {
    priceAmount: normalizedAmount,
    priceCurrency: normalizedAmount !== null ? normalizedCurrency : null
  };
}

function normalizePriority(priority?: string | null): string | undefined {
  if (!priority) {
    return undefined;
  }

  const normalized = priority.trim();
  return normalized.length > 0 ? normalized : undefined;
}
