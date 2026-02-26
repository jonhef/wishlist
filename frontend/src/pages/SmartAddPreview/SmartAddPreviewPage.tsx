import { useState } from "react";
import { CompareCards } from "../../features/smartAdd/CompareCards";
import type { ItemDraft } from "../../features/items/itemDraft";

const previewDraft: ItemDraft = {
  name: "New headphones",
  url: "https://example.com/headphones",
  notes: "Lightweight, with good noise cancellation",
  priceAmount: "199.99",
  priceCurrency: "USD"
};

const previewExisting = {
  name: "E-reader",
  url: "https://example.com/reader",
  notes: "Read in the evening without backlight glare",
  priceAmount: 149.99,
  priceCurrency: "USD"
};

export function SmartAddPreviewPage(): JSX.Element {
  const [stepIndex, setStepIndex] = useState(1);

  return (
    <section className="public-page stack gap-lg">
      <h2>Smart Add Preview</h2>
      <CompareCards
        canGoBack={stepIndex > 1}
        existingItem={previewExisting}
        isBusy={false}
        maxStepsEstimate={7}
        newItemDraft={previewDraft}
        onBack={() => setStepIndex((current) => Math.max(1, current - 1))}
        onCancel={() => setStepIndex(1)}
        onChoose={() => setStepIndex((current) => Math.min(7, current + 1))}
        onSkipToSimple={() => setStepIndex(1)}
        stepIndex={stepIndex}
      />
    </section>
  );
}
