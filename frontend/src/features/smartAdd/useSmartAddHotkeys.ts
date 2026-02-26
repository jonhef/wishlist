import { useEffect } from "react";

type UseSmartAddHotkeysOptions = {
  enabled: boolean;
  onChooseNew: () => void;
  onChooseExisting: () => void;
  onBack: () => void;
  onCancel: () => void;
};

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }

  const tag = target.tagName.toLowerCase();
  return tag === "input" || tag === "textarea" || tag === "select" || target.isContentEditable;
}

export function useSmartAddHotkeys({
  enabled,
  onChooseNew,
  onChooseExisting,
  onBack,
  onCancel
}: UseSmartAddHotkeysOptions): void {
  useEffect(() => {
    if (!enabled) {
      return undefined;
    }

    const handler = (event: KeyboardEvent): void => {
      if (event.altKey || event.ctrlKey || event.metaKey || isTypingTarget(event.target)) {
        return;
      }

      if (event.key === "ArrowLeft") {
        event.preventDefault();
        onChooseNew();
      } else if (event.key === "ArrowRight") {
        event.preventDefault();
        onChooseExisting();
      } else if (event.key === "Backspace") {
        event.preventDefault();
        onBack();
      } else if (event.key === "Escape") {
        event.preventDefault();
        onCancel();
      }
    };

    window.addEventListener("keydown", handler);
    return () => {
      window.removeEventListener("keydown", handler);
    };
  }, [enabled, onBack, onCancel, onChooseExisting, onChooseNew]);
}
