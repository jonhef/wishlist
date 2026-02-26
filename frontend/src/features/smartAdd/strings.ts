export const smartAddStrings = {
  intro: "Smart add: answer a few comparisons to determine priority.",
  questionTitle: "What matters more right now?",
  newItemLabel: "New item",
  existingItemLabel: "Current item",
  chooseNew: "New is more important",
  chooseExisting: "Current is more important",
  back: "Back",
  cancel: "Cancel",
  skipToSimple: "Skip and add normally",
  staleDialog: "The list has changed. Recalculate questions using the latest order?",
  progress: (step: number, total: number) => `Question ${step} of ~${total}`
} as const;
