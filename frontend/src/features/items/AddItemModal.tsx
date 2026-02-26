import { FormEvent, useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, apiClient, type Item } from "../../api/client";
import { Button, Input, Modal, useToast } from "../../ui";
import { SmartAddWizard } from "../smartAdd/SmartAddWizard";
import { buildCreateItemPayload, emptyItemDraft, hasUnsavedItemDraft, type ItemDraft } from "./itemDraft";
import { insertItemIntoItemsCache } from "./itemsQueries";
import { sortItems } from "./sortItems";

type AddMode = "simple" | "smart";

type AddItemModalProps = {
  wishlistId: string;
  isOpen: boolean;
  onClose: () => void;
  items: Item[];
  isItemsLoading: boolean;
};

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function AddItemModal({
  wishlistId,
  isOpen,
  onClose,
  items,
  isItemsLoading
}: AddItemModalProps): JSX.Element {
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [draft, setDraft] = useState<ItemDraft>(emptyItemDraft);
  const [mode, setMode] = useState<AddMode>("simple");
  const [isSubmittingSmart, setIsSubmittingSmart] = useState(false);

  const sortedItems = useMemo(() => sortItems(items), [items]);

  useEffect(() => {
    if (isOpen) {
      return;
    }

    setDraft(emptyItemDraft);
    setMode("simple");
    setIsSubmittingSmart(false);
  }, [isOpen]);

  const createItemMutation = useMutation({
    mutationFn: async (priority?: string) => apiClient.createItem(wishlistId, buildCreateItemPayload(draft, priority)),
    onSuccess: (newItem) => {
      insertItemIntoItemsCache(queryClient, wishlistId, newItem);
      setDraft(emptyItemDraft);
      setMode("simple");
      setIsSubmittingSmart(false);
      onClose();
      showToast("Item created", "success");
    },
    onError: (error) => {
      setIsSubmittingSmart(false);
      showToast(isApiError(error) ? error.message : "Could not create item", "error");
    }
  });

  const requestClose = (): void => {
    if (createItemMutation.isPending || isSubmittingSmart) {
      return;
    }

    if (hasUnsavedItemDraft(draft) && !window.confirm("Discard unsaved item draft?")) {
      return;
    }

    onClose();
  };

  const onSimpleSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();

    if (!draft.name.trim()) {
      showToast("Name is required", "error");
      return;
    }

    createItemMutation.mutate(undefined);
  };

  const handleSmartDone = async (priority: string): Promise<void> => {
    if (!draft.name.trim()) {
      showToast("Name is required", "error");
      setMode("simple");
      return;
    }

    setIsSubmittingSmart(true);
    await createItemMutation.mutateAsync(priority);
  };

  const startSmartMode = (): void => {
    if (!draft.name.trim()) {
      showToast("Name is required before smart add", "error");
      return;
    }

    setMode("smart");
  };

  return (
    <Modal
      footer={mode === "simple"
        ? (
          <>
            <Button onClick={requestClose} type="button" variant="ghost">
              Cancel
            </Button>
            <Button disabled={createItemMutation.isPending} form="create-item-form" type="submit">
              Add normally
            </Button>
            <Button
              disabled={createItemMutation.isPending || isItemsLoading}
              onClick={startSmartMode}
              type="button"
              variant="secondary"
            >
              Smart add
            </Button>
          </>
        )
        : undefined}
      isOpen={isOpen}
      onClose={requestClose}
      title="Add item"
    >
      {mode === "simple" ? (
        <ItemDraftForm
          draft={draft}
          formId="create-item-form"
          onChange={setDraft}
          onSubmit={onSimpleSubmit}
        />
      ) : (
        <>
          {isItemsLoading ? (
            <div className="smart-add-skeleton">
              <p>Loading items for smart add...</p>
            </div>
          ) : (
            <SmartAddWizard
              draft={draft}
              itemsSorted={sortedItems}
              onCancel={requestClose}
              onDone={handleSmartDone}
              onSkipToSimple={() => setMode("simple")}
              wishlistId={wishlistId}
            />
          )}
        </>
      )}
    </Modal>
  );
}

type ItemDraftFormProps = {
  draft: ItemDraft;
  onChange: (draft: ItemDraft) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  formId: string;
};

function ItemDraftForm({ draft, onChange, onSubmit, formId }: ItemDraftFormProps): JSX.Element {
  return (
    <form className="stack" id={formId} onSubmit={onSubmit}>
      <Input
        id={`${formId}-name`}
        label="Name"
        onChange={(event) => onChange({ ...draft, name: event.target.value })}
        required
        value={draft.name}
      />

      <Input
        id={`${formId}-url`}
        label="URL (optional)"
        onChange={(event) => onChange({ ...draft, url: event.target.value })}
        placeholder="https://example.com/item"
        type="text"
        value={draft.url}
      />

      <div className="grid-two">
        <Input
          id={`${formId}-price`}
          label="Price"
          min="0"
          onChange={(event) => onChange({ ...draft, priceAmount: event.target.value })}
          step="0.01"
          type="number"
          value={draft.priceAmount}
        />

        <Input
          id={`${formId}-currency`}
          label="Currency"
          onChange={(event) => onChange({ ...draft, priceCurrency: event.target.value.toUpperCase() })}
          value={draft.priceCurrency}
        />
      </div>

      <label className="ui-field" htmlFor={`${formId}-notes`}>
        <span className="ui-field-label">Notes</span>
        <textarea
          className="ui-input"
          id={`${formId}-notes`}
          onChange={(event) => onChange({ ...draft, notes: event.target.value })}
          rows={4}
          value={draft.notes}
        />
      </label>
    </form>
  );
}
