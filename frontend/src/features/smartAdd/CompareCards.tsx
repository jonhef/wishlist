import { useEffect, useRef } from "react";
import type { Item } from "../../api/client";
import type { ItemDraft } from "../items/itemDraft";
import { Button, Card } from "../../ui";
import { smartAddStrings } from "./strings";

type CompareCardsProps = {
  newItemDraft: ItemDraft;
  existingItem: Pick<Item, "name" | "url" | "notes" | "priceAmount" | "priceCurrency">;
  onChoose: (choice: "new" | "existing") => void;
  onBack: () => void;
  onCancel: () => void;
  onSkipToSimple: () => void;
  canGoBack: boolean;
  stepIndex: number;
  maxStepsEstimate: number;
  isBusy: boolean;
};

export function CompareCards({
  newItemDraft,
  existingItem,
  onChoose,
  onBack,
  onCancel,
  onSkipToSimple,
  canGoBack,
  stepIndex,
  maxStepsEstimate,
  isBusy
}: CompareCardsProps): JSX.Element {
  const primaryActionRef = useRef<HTMLButtonElement>(null);
  const parsedDraftPrice = Number(newItemDraft.priceAmount);
  const draftPriceAmount = Number.isFinite(parsedDraftPrice) ? parsedDraftPrice : null;

  useEffect(() => {
    primaryActionRef.current?.focus();
  }, [stepIndex, existingItem.name]);

  return (
    <div className="stack gap-md">
      <div aria-live="polite" className="smart-add-progress muted">
        {smartAddStrings.progress(stepIndex, maxStepsEstimate)}
      </div>
      <h3 aria-live="polite">{smartAddStrings.questionTitle}</h3>

      <div className="smart-compare-grid">
        <Card className="smart-compare-card">
          <p className="muted">{smartAddStrings.newItemLabel}</p>
          <h4>{newItemDraft.name || "Untitled"}</h4>
          <ItemSummary
            notes={newItemDraft.notes || null}
            priceAmount={newItemDraft.priceAmount ? draftPriceAmount : null}
            priceCurrency={newItemDraft.priceCurrency || null}
            url={newItemDraft.url || null}
          />
        </Card>

        <Card className="smart-compare-card">
          <p className="muted">{smartAddStrings.existingItemLabel}</p>
          <h4>{existingItem.name}</h4>
          <ItemSummary
            notes={existingItem.notes}
            priceAmount={existingItem.priceAmount}
            priceCurrency={existingItem.priceCurrency}
            url={existingItem.url}
          />
        </Card>
      </div>

      <div className="smart-compare-actions">
        <Button
          aria-label="New item is more important than current item"
          className="smart-compare-choice"
          disabled={isBusy}
          onClick={() => onChoose("new")}
          ref={primaryActionRef}
          type="button"
        >
          {smartAddStrings.chooseNew}
        </Button>
        <Button
          aria-label="Current item is more important than new item"
          className="smart-compare-choice"
          disabled={isBusy}
          onClick={() => onChoose("existing")}
          type="button"
          variant="secondary"
        >
          {smartAddStrings.chooseExisting}
        </Button>
      </div>

      <div className="actions-row wrap">
        <Button disabled={!canGoBack || isBusy} onClick={onBack} type="button" variant="ghost">
          {smartAddStrings.back}
        </Button>
        <Button disabled={isBusy} onClick={onSkipToSimple} type="button" variant="ghost">
          {smartAddStrings.skipToSimple}
        </Button>
        <Button disabled={isBusy} onClick={onCancel} type="button" variant="ghost">
          {smartAddStrings.cancel}
        </Button>
      </div>
    </div>
  );
}

type ItemSummaryProps = {
  notes: string | null;
  url: string | null;
  priceAmount: number | null;
  priceCurrency: string | null;
};

function ItemSummary({ notes, url, priceAmount, priceCurrency }: ItemSummaryProps): JSX.Element {
  return (
    <div className="stack gap-md">
      {notes ? <p>{notes}</p> : null}
      {url ? (
        <a className="inline-link" href={url} rel="noreferrer" target="_blank">
          {url}
        </a>
      ) : null}
      {priceAmount !== null ? (
        <p className="muted">
          {priceAmount} {priceCurrency ?? ""}
        </p>
      ) : null}
    </div>
  );
}
