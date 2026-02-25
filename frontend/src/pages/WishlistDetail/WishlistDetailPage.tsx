import { FormEvent, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  ApiError,
  apiClient,
  type CreateItemRequest,
  type Item,
  type ItemListResult,
  type UpdateItemRequest
} from "../../api/client";
import { useTheme } from "../../theme/ThemeProvider";
import { Button, Card, Input, Modal, useToast } from "../../ui";

type SortMode = "priority" | "updated";

type ItemDraft = {
  name: string;
  url: string;
  priceAmount: string;
  priceCurrency: string;
  priority: number;
  notes: string;
};

const emptyDraft: ItemDraft = {
  name: "",
  url: "",
  priceAmount: "",
  priceCurrency: "USD",
  priority: 1,
  notes: ""
};

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

function draftFromItem(item: Item): ItemDraft {
  return {
    name: item.name,
    url: item.url ?? "",
    priceAmount: item.priceAmount !== null ? String(item.priceAmount) : "",
    priceCurrency: item.priceCurrency ?? "USD",
    priority: item.priority,
    notes: item.notes ?? ""
  };
}

function itemPayloadFromDraft(draft: ItemDraft): CreateItemRequest {
  return {
    name: draft.name.trim(),
    url: draft.url.trim() || null,
    priceAmount: draft.priceAmount.trim() ? Number(draft.priceAmount) : null,
    priceCurrency: draft.priceCurrency.trim() || null,
    priority: Number(draft.priority),
    notes: draft.notes.trim() || null
  };
}

function itemPatchPayloadFromDraft(draft: ItemDraft): UpdateItemRequest {
  return {
    name: draft.name.trim(),
    url: draft.url.trim() || null,
    priceAmount: draft.priceAmount.trim() ? Number(draft.priceAmount) : null,
    priceCurrency: draft.priceCurrency.trim() || null,
    priority: Number(draft.priority),
    notes: draft.notes.trim() || null
  };
}

export function WishlistDetailPage(): JSX.Element {
  const { wishlistId } = useParams<{ wishlistId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { setActiveTheme } = useTheme();

  const [sortMode, setSortMode] = useState<SortMode>("priority");
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<Item | null>(null);
  const [draft, setDraft] = useState<ItemDraft>(emptyDraft);

  const wishlistQueryKey = ["wishlist", wishlistId];
  const itemsQueryKey = ["wishlist-items", wishlistId];

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

  const createItemMutation = useMutation({
    mutationFn: async () => apiClient.createItem(wishlistId as string, itemPayloadFromDraft(draft)),
    onSuccess: (newItem) => {
      queryClient.setQueryData<ItemListResult | undefined>(itemsQueryKey, (current) => {
        if (!current) {
          return { items: [newItem], nextCursor: null };
        }

        return {
          ...current,
          items: [newItem, ...current.items]
        };
      });

      setIsCreateOpen(false);
      setDraft(emptyDraft);
      showToast("Item created", "success");
    },
    onError: (error) => {
      showToast(isApiError(error) ? error.message : "Could not create item", "error");
    }
  });

  const patchItemMutation = useMutation({
    mutationFn: async () => {
      if (!editingItem) {
        throw new Error("No item selected");
      }

      return apiClient.patchItem(wishlistId as string, editingItem.id, itemPatchPayloadFromDraft(draft));
    },
    onSuccess: (updatedItem) => {
      queryClient.setQueryData<ItemListResult | undefined>(itemsQueryKey, (current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          items: current.items.map((item) => (item.id === updatedItem.id ? updatedItem : item))
        };
      });

      setEditingItem(null);
      setDraft(emptyDraft);
      showToast("Item updated", "success");
    },
    onError: (error) => {
      showToast(isApiError(error) ? error.message : "Could not update item", "error");
    }
  });

  const deleteItemMutation = useMutation({
    mutationFn: (itemId: number) => apiClient.deleteItem(wishlistId as string, itemId),
    onSuccess: (_, itemId) => {
      queryClient.setQueryData<ItemListResult | undefined>(itemsQueryKey, (current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          items: current.items.filter((item) => item.id !== itemId)
        };
      });

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

  const onCreateSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();
    if (!draft.name.trim()) {
      showToast("Name is required", "error");
      return;
    }

    createItemMutation.mutate();
  };

  const onPatchSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();

    if (!editingItem) {
      return;
    }

    patchItemMutation.mutate();
  };

  const items = itemsQuery.data?.items ?? [];
  const sortedItems = useMemo(() => {
    const nextItems = [...items];

    if (sortMode === "priority") {
      nextItems.sort((left, right) => right.priority - left.priority);
      return nextItems;
    }

    nextItems.sort((left, right) => new Date(right.updatedAtUtc).getTime() - new Date(left.updatedAtUtc).getTime());
    return nextItems;
  }, [items, sortMode]);

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
          <Button onClick={() => {
            setDraft(emptyDraft);
            setIsCreateOpen(true);
          }}>
            Add item
          </Button>
        </div>
      </header>

      <div className="actions-row wrap">
        <label className="ui-field" htmlFor="sort-mode">
          <span className="ui-field-label">Sort by</span>
          <select
            className="ui-input"
            id="sort-mode"
            onChange={(event) => setSortMode(event.target.value as SortMode)}
            value={sortMode}
          >
            <option value="priority">Priority</option>
            <option value="updated">Recently updated</option>
          </select>
        </label>
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
                  setDraft(draftFromItem(item));
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

      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Add item"
        footer={(
          <>
            <Button onClick={() => setIsCreateOpen(false)} type="button" variant="ghost">
              Cancel
            </Button>
            <Button form="create-item-form" type="submit">
              Save
            </Button>
          </>
        )}
      >
        <ItemForm draft={draft} onChange={setDraft} onSubmit={onCreateSubmit} />
      </Modal>

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
        <ItemForm draft={draft} formId="edit-item-form" onChange={setDraft} onSubmit={onPatchSubmit} />
      </Modal>
    </section>
  );
}

type ItemFormProps = {
  draft: ItemDraft;
  onChange: (draft: ItemDraft) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  formId?: string;
};

function ItemForm({ draft, onChange, onSubmit, formId = "create-item-form" }: ItemFormProps): JSX.Element {
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
        label="URL"
        onChange={(event) => onChange({ ...draft, url: event.target.value })}
        type="url"
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

      <label className="ui-field" htmlFor={`${formId}-priority`}>
        <span className="ui-field-label">Priority</span>
        <input
          className="ui-input"
          id={`${formId}-priority`}
          max={10}
          min={1}
          onChange={(event) => onChange({ ...draft, priority: Number(event.target.value) })}
          type="range"
          value={draft.priority}
        />
        <span className="muted">{draft.priority}</span>
      </label>

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
