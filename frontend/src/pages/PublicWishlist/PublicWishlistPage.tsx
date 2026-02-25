import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ApiError, apiClient } from "../../api/client";
import { Card } from "../../ui";

// Public API currently does not expose theme tokens/themeId, so public route uses active default theme.

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function PublicWishlistPage(): JSX.Element {
  const { token } = useParams<{ token: string }>();

  const query = useQuery({
    enabled: Boolean(token),
    queryKey: ["public-wishlist", token],
    queryFn: () => apiClient.getPublicWishlist(token as string)
  });

  if (!token) {
    return <p className="form-error">Missing public token.</p>;
  }

  if (query.isLoading) {
    return <div className="public-page">Loading public wishlist...</div>;
  }

  if (query.error) {
    const invalidLink = isApiError(query.error) && query.error.status === 404;

    return (
      <div className="public-page">
        <Card className="stack">
          <h1>{invalidLink ? "Ссылка недействительна" : "Could not load wishlist"}</h1>
          <p className="muted">
            {invalidLink
              ? "Проверьте токен или попросите владельца заново поделиться wishlist."
              : "Unexpected error while loading public view."}
          </p>
          <Link className="inline-link" to="/login">
            Open app login
          </Link>
        </Card>
      </div>
    );
  }

  const wishlist = query.data;

  return (
    <div className="public-page">
      <Card className="stack gap-md">
        <h1>{wishlist.title}</h1>
        {wishlist.description ? <p>{wishlist.description}</p> : null}
      </Card>

      <div className="stack gap-md">
        {wishlist.items.map((item, index) => (
          <Card className="item-card" key={`${item.name}-${index}`}>
            <h3>{item.name}</h3>
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
            <p className="muted">Priority: {item.priority}</p>
          </Card>
        ))}
      </div>
    </div>
  );
}
