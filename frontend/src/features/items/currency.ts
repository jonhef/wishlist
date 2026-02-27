export const supportedCurrencies = ["EUR", "USD", "RUB", "JPY"] as const;

export type SupportedCurrency = (typeof supportedCurrencies)[number];

export function isSupportedCurrency(value: string): value is SupportedCurrency {
  return supportedCurrencies.includes(value as SupportedCurrency);
}

export function normalizeCurrency(value: string | null | undefined): SupportedCurrency | null {
  if (!value) {
    return null;
  }

  const normalized = value.trim().toUpperCase();
  return isSupportedCurrency(normalized) ? normalized : null;
}

export function getMinorUnits(currency: SupportedCurrency): number {
  switch (currency) {
    case "EUR":
    case "USD":
    case "RUB":
      return 2;
    case "JPY":
      return 0;
    default:
      return 2;
  }
}

export function minorToMajorString(amountMinor: number, currency: SupportedCurrency): string {
  const minorUnits = getMinorUnits(currency);
  if (minorUnits === 0) {
    return String(amountMinor);
  }

  return (amountMinor / (10 ** minorUnits)).toFixed(minorUnits);
}

export function majorStringToMinor(rawAmount: string, currency: SupportedCurrency): number | null {
  const normalized = rawAmount.trim().replace(",", ".");
  if (!normalized) {
    return null;
  }

  if (!/^\d+(\.\d+)?$/.test(normalized)) {
    return null;
  }

  const minorUnits = getMinorUnits(currency);
  const decimalPart = normalized.includes(".") ? normalized.split(".")[1] ?? "" : "";
  if (decimalPart.length > minorUnits) {
    return null;
  }

  const parsed = Number(normalized);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return null;
  }

  const factor = 10 ** minorUnits;
  const scaled = Math.round(parsed * factor);
  return Number.isSafeInteger(scaled) ? scaled : null;
}

export function formatMinorPrice(amountMinor: number, currency: string | null): string {
  const normalizedCurrency = normalizeCurrency(currency);
  if (!normalizedCurrency) {
    return String(amountMinor);
  }

  const minorUnits = getMinorUnits(normalizedCurrency);
  const amountMajor = amountMinor / (10 ** minorUnits);

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: normalizedCurrency,
    minimumFractionDigits: minorUnits,
    maximumFractionDigits: minorUnits
  }).format(amountMajor);
}
