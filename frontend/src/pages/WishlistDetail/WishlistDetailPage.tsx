import { FormEvent, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError, apiClient, type Item } from "../../api/client";
import { AddItemModal } from "../../features/items/AddItemModal";
import { buildPatchItemPayload, emptyItemDraft, itemDraftFromItem, type ItemDraft } from "../../features/items/itemDraft";
import { removeItemFromItemsCache, updateItemInItemsCache, wishlistItemsQueryKey } from "../../features/items/itemsQueries";
import { sortItems } from "../../features/items/sortItems";
import { useTheme } from "../../theme/ThemeProvider";
import { Button, Card, Input, Modal, useToast } from "../../ui";

type EditItemDraft = ItemDraft & {
  priority: string;
};

const emptyEditDraft: EditItemDraft = {
  ...emptyItemDraft,
  priority: "0"
};

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

function editDraftFromItem(item: Item): EditItemDraft {
  return {
    ...itemDraftFromItem(item),
    priority: item.priority
  };
}

export function WishlistDetailPage(): JSX.Element {
  const { wishlistId } = useParams<{ wishlistId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { setActiveTheme } = useTheme();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<Item | null>(null);
  const [editDraft, setEditDraft] = useState<EditItemDraft>(emptyEditDraft);

  const wishlistQueryKey = ["wishlist", wishlistId];
  const itemsQueryKey = wishlistItemsQueryKey(wishlistId);

  const wishlistQuery = useQuery({
    enabled: Boolean(wishlistId),
    queryKey: wishlistQueryKey,
    queryFn: () => apiClient.getWishlist(wishlistId as string)
  });

  const itemsQuery = useQuery({
    enabled: Boolean(wishlistId),
    queryKey: itemsQueryKey,
    queryFn: () => apiClient.listItems(wishlistId as string, undefined, 100)
  });

  const wishlistThemeId = wishlistQuery.data?.themeId ?? null;

  useEffect(() => {
    if (!wishlistQuery.isSuccess) {
      return;
    }

    setActiveTheme(wishlistThemeId);
  }, [setActiveTheme, wishlistQuery.isSuccess, wishlistThemeId]);

  const patchItemMutation = useMutation({
    mutationFn: async () => {
      if (!editingItem) {
        throw new Error("No item selected");
      }

      return apiClient.patchItem(
        wishlistId as string,
        editingItem.id,
        buildPatchItemPayload(editDraft, editDraft.priority)
      );
    },
    onSuccess: (updatedItem) => {
      updateItemInItemsCache(queryClient, wishlistId as string, updatedItem);
      setEditingItem(null);
      setEditDraft(emptyEditDraft);
      showToast("Item updated", "success");
    },
    onError: (error) => {
      showToast(isApiError(error) ? error.message : "Could not update item", "error");
    }
  });

  const deleteItemMutation = useMutation({
    mutationFn: (itemId: number) => apiClient.deleteItem(wishlistId as string, itemId),
    onSuccess: (_, itemId) => {
      removeItemFromItemsCache(queryClient, wishlistId as string, itemId);
      showToast("Item deleted", "success");
    },
    onError: (error) => {
      showToast(isApiError(error) ? error.message : "Could not delete item", "error");
    }
  });

  const rotateShareMutation = useMutation({
    mutationFn: () => apiClient.rotateShareLink(wishlistId as string),
    onSuccess: async (payload) => {
      try {
        const apiUrl = new URL(payload.publicUrl);
        const segments = apiUrl.pathname.split("/").filter(Boolean);
        const token = segments[segments.length - 1];

        if (!token) {
          throw new Error("Token is missing");
        }

        const publicUrl = `${window.location.origin}/p/${token}`;
        await navigator.clipboard.writeText(publicUrl);
        showToast("Public link copied", "success");
      } catch {
        showToast("Could not copy public link", "error");
      }
    },
    onError: () => {
      showToast("Could not create public link", "error");
    }
  });

  const onPatchSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();

    if (!editingItem) {
      return;
    }

    patchItemMutation.mutate();
  };

  const sortedItems = useMemo(() => sortItems(itemsQuery.data?.items ?? []), [itemsQuery.data?.items]);

  if (!wishlistId) {
    return <p className="form-error">Missing wishlist id.</p>;
  }

  return (
    <section className="stack gap-lg">
      <header className="section-header">
        <div className="stack">
          <Button onClick={() => navigate("/dashboard")} variant="ghost">
            Back
          </Button>
          <h2>{wishlistQuery.data?.title ?? "Wishlist"}</h2>
          <p className="muted">{wishlistQuery.data?.description ?? "No description"}</p>
        </div>

        <div className="actions-row">
          <Button onClick={() => rotateShareMutation.mutate()} variant="secondary">
            Copy public link
          </Button>
          <Button onClick={() => setIsCreateOpen(true)}>
            Add item
          </Button>
        </div>
      </header>

      <div className="actions-row wrap">
        <Link className="inline-link" to="/themes/editor">
          Open theme editor
        </Link>
      </div>

      {wishlistQuery.isLoading || itemsQuery.isLoading ? <p>Loading wishlist...</p> : null}
      {wishlistQuery.error || itemsQuery.error ? <p className="form-error">Could not load wishlist.</p> : null}

      <div className="stack gap-md">
        {sortedItems.map((item) => (
          <Card className="item-card" key={item.id}>
            <div className="stack">
              <h3>{item.name}</h3>
              <p className="muted">Priority: {item.priority}</p>
              {item.notes ? <p>{item.notes}</p> : null}
              {item.url ? (
                <a className="inline-link" href={item.url} rel="noreferrer" target="_blank">
                  {item.url}
                </a>
              ) : null}
              {item.priceAmount !== null ? (
                <p className="muted">
                  {item.priceAmount} {item.priceCurrency ?? ""}
                </p>
              ) : null}
              <p className="muted">Updated {new Date(item.updatedAtUtc).toLocaleString()}</p>
            </div>

            <div className="actions-row">
              <Button
                aria-label={`Edit ${item.name}`}
                onClick={() => {
                  setEditingItem(item);
                  setEditDraft(editDraftFromItem(item));
                }}
                variant="secondary"
              >
                Edit
              </Button>
              <Button
                aria-label={`Delete ${item.name}`}
                onClick={() => {
                  if (window.confirm(`Delete item \"${item.name}\"?`)) {
                    deleteItemMutation.mutate(item.id);
                  }
                }}
                variant="danger"
              >
                Delete
              </Button>
            </div>
          </Card>
        ))}
      </div>

      <AddItemModal
        isItemsLoading={itemsQuery.isLoading}
        isOpen={isCreateOpen}
        items={itemsQuery.data?.items ?? []}
        onClose={() => setIsCreateOpen(false)}
        wishlistId={wishlistId}
      />

      <Modal
        isOpen={Boolean(editingItem)}
        onClose={() => setEditingItem(null)}
        title="Edit item"
        footer={(
          <>
            <Button onClick={() => setEditingItem(null)} type="button" variant="ghost">
              Cancel
            </Button>
            <Button form="edit-item-form" type="submit">
              Update
            </Button>
          </>
        )}
      >
        <EditItemForm
          draft={editDraft}
          formId="edit-item-form"
          onChange={setEditDraft}
          onSubmit={onPatchSubmit}
        />
      </Modal>
    </section>
  );
}

type EditItemFormProps = {
  draft: EditItemDraft;
  onChange: (draft: EditItemDraft) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  formId: string;
};

function EditItemForm({ draft, onChange, onSubmit, formId }: EditItemFormProps): JSX.Element {
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

      <Input
        id={`${formId}-priority`}
        label="Priority"
        onChange={(event) => onChange({ ...draft, priority: event.target.value })}
        type="text"
        value={draft.priority}
      />

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
