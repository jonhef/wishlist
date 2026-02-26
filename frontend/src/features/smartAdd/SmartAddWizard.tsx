import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { apiClient, type Item } from "../../api/client";
import type { ItemDraft } from "../items/itemDraft";
import { sortItems } from "../items/sortItems";
import { CompareCards } from "./CompareCards";
import { applyChoice, currentMid, initBinaryInsert, isBinaryInsertDone, maxQuestionsEstimate, resultPos, undoChoice } from "./binaryInsert";
import { computePriorityForPosition } from "./priorityMath";
import { computeItemsVersion, hasItemsVersionChanged } from "./staleGuard";
import { smartAddStrings } from "./strings";
import { useSmartAddHotkeys } from "./useSmartAddHotkeys";

type SmartAddWizardProps = {
  wishlistId: string;
  draft: ItemDraft;
  itemsSorted: Item[];
  onCancel: () => void;
  onSkipToSimple: () => void;
  onDone: (priority: string) => Promise<void> | void;
};

export function SmartAddWizard({
  wishlistId,
  draft,
  itemsSorted,
  onCancel,
  onSkipToSimple,
  onDone
}: SmartAddWizardProps): JSX.Element {
  const [workingItems, setWorkingItems] = useState<Item[]>(itemsSorted);
  const [machine, setMachine] = useState(() => initBinaryInsert(itemsSorted.length));
  const [itemsVersion, setItemsVersion] = useState(() => computeItemsVersion(itemsSorted));
  const [isBusy, setIsBusy] = useState(false);
  const isDone = isBinaryInsertDone(machine);
  const finalizeInFlightRef = useRef(false);

  useEffect(() => {
    setWorkingItems(itemsSorted);
    setMachine(initBinaryInsert(itemsSorted.length));
    setItemsVersion(computeItemsVersion(itemsSorted));
    setIsBusy(false);
    finalizeInFlightRef.current = false;
  }, [itemsSorted, wishlistId]);

  const mid = currentMid(machine);
  const stepsEstimate = useMemo(() => maxQuestionsEstimate(workingItems.length), [workingItems.length]);
  const stepIndex = machine.history.length + 1;

  const handleChoose = useCallback((choice: "new" | "existing") => {
    setMachine((current) => applyChoice(current, choice));
  }, []);

  const handleBack = useCallback(() => {
    setMachine((current) => undoChoice(current));
  }, []);

  const finalize = useCallback(async () => {
    if (finalizeInFlightRef.current) {
      return;
    }

    finalizeInFlightRef.current = true;
    setIsBusy(true);

    try {
      const latestItems = await apiClient.listItems(wishlistId, undefined, 100);
      const sortedLatestItems = sortItems(latestItems.items);
      const latestVersion = computeItemsVersion(sortedLatestItems);

      if (hasItemsVersionChanged(itemsVersion, latestVersion)) {
        setIsBusy(false);
        finalizeInFlightRef.current = false;

        if (window.confirm(smartAddStrings.staleDialog)) {
          setWorkingItems(sortedLatestItems);
          setItemsVersion(latestVersion);
          setMachine(initBinaryInsert(sortedLatestItems.length));
        } else {
          onSkipToSimple();
        }

        return;
      }

      const pos = resultPos(machine);
      const priority = computePriorityForPosition(
        workingItems.map((item) => item.priority),
        pos
      );

      await onDone(priority);
    } finally {
      setIsBusy(false);
      finalizeInFlightRef.current = false;
    }
  }, [itemsVersion, machine, onDone, onSkipToSimple, wishlistId, workingItems]);

  useEffect(() => {
    if (!isDone) {
      return;
    }

    void finalize();
  }, [finalize, isDone]);

  useSmartAddHotkeys({
    enabled: !isBusy && mid !== null,
    onChooseNew: () => handleChoose("new"),
    onChooseExisting: () => handleChoose("existing"),
    onBack: handleBack,
    onCancel
  });

  if (isDone || mid === null) {
    return (
      <div className="stack gap-md">
        <p>{workingItems.length === 0 ? "List is empty, adding immediately..." : "Saving order..."}</p>
      </div>
    );
  }

  return (
    <div className="stack gap-md">
      <p className="muted">{smartAddStrings.intro}</p>
      <CompareCards
        canGoBack={machine.history.length > 0}
        existingItem={workingItems[mid]}
        isBusy={isBusy}
        maxStepsEstimate={stepsEstimate}
        newItemDraft={draft}
        onBack={handleBack}
        onCancel={onCancel}
        onChoose={handleChoose}
        onSkipToSimple={onSkipToSimple}
        stepIndex={stepIndex}
      />
    </div>
  );
}
