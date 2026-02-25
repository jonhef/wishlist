import { FormEvent, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient, type Wishlist, type WishlistListResult } from "../../api/client";
import { ApiError } from "../../api/client";
import { useTheme } from "../../theme/ThemeProvider";
import { Button, Card, Input, Modal, useToast } from "../../ui";

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

const dashboardQueryKey = ["wishlists"];

export function DashboardPage(): JSX.Element {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { themes } = useTheme();
  const { showToast } = useToast();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createTitle, setCreateTitle] = useState("");
  const [createDescription, setCreateDescription] = useState("");
  const [createThemeId, setCreateThemeId] = useState("");

  const [editingWishlist, setEditingWishlist] = useState<Wishlist | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [actionsMenuWishlistId, setActionsMenuWishlistId] = useState<string | null>(null);

  useEffect(() => {
    const handleClick = (event: MouseEvent): void => {
      const target = event.target;

      if (!(target instanceof Element)) {
        return;
      }

      if (target.closest(".card-actions-menu")) {
        return;
      }

      setActionsMenuWishlistId(null);
    };

    document.addEventListener("click", handleClick);
    return () => {
      document.removeEventListener("click", handleClick);
    };
  }, []);

  const { data, isLoading, error } = useQuery({
    queryKey: dashboardQueryKey,
    queryFn: () => apiClient.listWishlists(undefined, 50)
  });

  const createMutation = useMutation({
    mutationFn: async () => apiClient.createWishlist({
      title: createTitle.trim(),
      description: createDescription.trim() || null,
      themeId: createThemeId || null
    }),
    onSuccess: (createdWishlist) => {
      queryClient.setQueryData<WishlistListResult | undefined>(dashboardQueryKey, (current) => {
        if (!current) {
          return { items: [createdWishlist], nextCursor: null };
        }

        return {
          ...current,
          items: [createdWishlist, ...current.items]
        };
      });

      showToast("Wishlist created", "success");
      setCreateTitle("");
      setCreateDescription("");
      setCreateThemeId("");
      setIsCreateOpen(false);
    },
    onError: (mutationError) => {
      showToast(isApiError(mutationError) ? mutationError.message : "Could not create wishlist", "error");
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (wishlistId: string) => apiClient.deleteWishlist(wishlistId),
    onSuccess: (_, wishlistId) => {
      queryClient.setQueryData<WishlistListResult | undefined>(dashboardQueryKey, (current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          items: current.items.filter((wishlist) => wishlist.id !== wishlistId)
        };
      });

      showToast("Wishlist removed", "success");
    },
    onError: (mutationError) => {
      showToast(isApiError(mutationError) ? mutationError.message : "Could not delete wishlist", "error");
    }
  });

  const patchMutation = useMutation({
    mutationFn: async () => {
      if (!editingWishlist) {
        throw new Error("No wishlist selected");
      }

      return apiClient.patchWishlist(editingWishlist.id, {
        title: editTitle.trim(),
        description: editDescription.trim() || null
      });
    },
    onSuccess: (updatedWishlist) => {
      queryClient.setQueryData<WishlistListResult | undefined>(dashboardQueryKey, (current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          items: current.items.map((wishlist) => (wishlist.id === updatedWishlist.id ? updatedWishlist : wishlist))
        };
      });

      showToast("Wishlist updated", "success");
      setEditingWishlist(null);
      setEditTitle("");
      setEditDescription("");
    },
    onError: (mutationError) => {
      showToast(isApiError(mutationError) ? mutationError.message : "Could not update wishlist", "error");
    }
  });

  const wishlists = useMemo(() => data?.items ?? [], [data]);

  const onCreateSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();
    if (!createTitle.trim()) {
      showToast("Title is required", "error");
      return;
    }
    createMutation.mutate();
  };

  const onPatchSubmit = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault();
    if (!editingWishlist) {
      return;
    }

    patchMutation.mutate();
  };

  const openRenameModal = (wishlist: Wishlist): void => {
    setEditingWishlist(wishlist);
    setEditTitle(wishlist.title);
    setEditDescription(wishlist.description ?? "");
  };

  return (
    <section className="stack gap-lg">
      <header className="section-header">
        <div>
          <h2>Wishlists</h2>
          <p className="muted">Your personal collections.</p>
        </div>
        <Button onClick={() => setIsCreateOpen(true)}>Create wishlist</Button>
      </header>

      {isLoading ? <p>Loading wishlists...</p> : null}

      {error ? <p className="form-error">Could not load wishlists.</p> : null}

      {!isLoading && !wishlists.length ? (
        <Card className="empty-state">
          <h3>No wishlists yet</h3>
          <p className="muted">Create your first wishlist to get started.</p>
          <Button onClick={() => setIsCreateOpen(true)}>Create wishlist</Button>
        </Card>
      ) : null}

      <div className="wishlist-grid">
        {wishlists.map((wishlist) => (
          <Card key={wishlist.id} className="wishlist-card">
            <div className="stack">
              <h3>{wishlist.title}</h3>
              {wishlist.description ? <p>{wishlist.description}</p> : <p className="muted">No description.</p>}
            </div>

            <div className="wishlist-meta muted">
              <span>{wishlist.itemsCount} items</span>
              <span>{new Date(wishlist.updatedAt).toLocaleString()}</span>
            </div>

            <div className="actions-row">
              <Button onClick={() => navigate(`/wishlists/${wishlist.id}`)}>Open</Button>
              <div className="card-actions-menu">
                <Button
                  aria-expanded={actionsMenuWishlistId === wishlist.id}
                  aria-haspopup="menu"
                  aria-label={`Actions for ${wishlist.title}`}
                  onClick={() => {
                    setActionsMenuWishlistId((current) => (current === wishlist.id ? null : wishlist.id));
                  }}
                  variant="ghost"
                >
                  ...
                </Button>

                {actionsMenuWishlistId === wishlist.id ? (
                  <div className="menu-popover" role="menu">
                    <Button
                      aria-label={`Rename ${wishlist.title}`}
                      onClick={() => {
                        setActionsMenuWishlistId(null);
                        openRenameModal(wishlist);
                      }}
                      variant="secondary"
                    >
                      Rename
                    </Button>
                    <Button
                      aria-label={`Delete ${wishlist.title}`}
                      onClick={() => {
                        setActionsMenuWishlistId(null);
                        if (window.confirm(`Delete wishlist \"${wishlist.title}\"?`)) {
                          deleteMutation.mutate(wishlist.id);
                        }
                      }}
                      variant="danger"
                    >
                      Delete
                    </Button>
                  </div>
                ) : null}
              </div>
            </div>
          </Card>
        ))}
      </div>

      <Modal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Create wishlist"
        footer={(
          <>
            <Button onClick={() => setIsCreateOpen(false)} type="button" variant="ghost">
              Cancel
            </Button>
            <Button form="create-wishlist-form" type="submit">
              Save
            </Button>
          </>
        )}
      >
        <form className="stack" id="create-wishlist-form" onSubmit={onCreateSubmit}>
          <Input
            id="create-title"
            label="Title"
            onChange={(event) => setCreateTitle(event.target.value)}
            required
            value={createTitle}
          />
          <label className="ui-field" htmlFor="create-description">
            <span className="ui-field-label">Description</span>
            <textarea
              className="ui-input"
              id="create-description"
              onChange={(event) => setCreateDescription(event.target.value)}
              rows={3}
              value={createDescription}
            />
          </label>
          <label className="ui-field" htmlFor="create-theme">
            <span className="ui-field-label">Theme</span>
            <select
              className="ui-input"
              id="create-theme"
              onChange={(event) => setCreateThemeId(event.target.value)}
              value={createThemeId}
            >
              <option value="">Default theme</option>
              {themes.map((theme) => (
                <option key={theme.id} value={theme.id}>
                  {theme.name}
                </option>
              ))}
            </select>
          </label>
        </form>
      </Modal>

      <Modal
        isOpen={Boolean(editingWishlist)}
        onClose={() => setEditingWishlist(null)}
        title="Rename wishlist"
        footer={(
          <>
            <Button onClick={() => setEditingWishlist(null)} type="button" variant="ghost">
              Cancel
            </Button>
            <Button form="rename-wishlist-form" type="submit">
              Update
            </Button>
          </>
        )}
      >
        <form className="stack" id="rename-wishlist-form" onSubmit={onPatchSubmit}>
          <Input
            id="rename-title"
            label="Title"
            onChange={(event) => setEditTitle(event.target.value)}
            required
            value={editTitle}
          />
          <label className="ui-field" htmlFor="rename-description">
            <span className="ui-field-label">Description</span>
            <textarea
              className="ui-input"
              id="rename-description"
              onChange={(event) => setEditDescription(event.target.value)}
              rows={3}
              value={editDescription}
            />
          </label>
        </form>
      </Modal>
    </section>
  );
}
