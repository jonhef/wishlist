import { useEffect, useRef } from "react";
import type { ReactNode } from "react";
import { createPortal } from "react-dom";

type ModalProps = {
  title: string;
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
};

const FOCUSABLE_SELECTOR = [
  "a[href]",
  "button:not([disabled])",
  "textarea:not([disabled])",
  "input:not([disabled])",
  "select:not([disabled])",
  "[tabindex]:not([tabindex='-1'])"
].join(",");

export function Modal({ title, isOpen, onClose, children, footer }: ModalProps): JSX.Element | null {
  const bodyRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const root = bodyRef.current;

    if (!root) {
      return undefined;
    }

    const focusableNodes = Array.from(root.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
    const firstFocusable = focusableNodes[0];
    const lastFocusable = focusableNodes[focusableNodes.length - 1];

    firstFocusable?.focus();

    const onKeyDown = (event: KeyboardEvent): void => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }

      if (event.key !== "Tab") {
        return;
      }

      if (!firstFocusable || !lastFocusable) {
        event.preventDefault();
        return;
      }

      if (event.shiftKey && document.activeElement === firstFocusable) {
        event.preventDefault();
        lastFocusable.focus();
      }

      if (!event.shiftKey && document.activeElement === lastFocusable) {
        event.preventDefault();
        firstFocusable.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  return createPortal(
    <div className="ui-modal-backdrop" onMouseDown={onClose}>
      <section
        aria-modal="true"
        aria-label={title}
        className="ui-modal"
        onMouseDown={(event) => event.stopPropagation()}
        ref={bodyRef}
        role="dialog"
      >
        <header className="ui-modal-header">
          <h2>{title}</h2>
        </header>

        <div className="ui-modal-body">{children}</div>

        <footer className="ui-modal-footer">{footer}</footer>
      </section>
    </div>,
    document.body
  );
}
