import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { ApiError, apiClient, type PublicWishlistSort } from "../../api/client";
import { defaultThemeTokens } from "../../theme/defaultTokens";
import { useTheme } from "../../theme/ThemeProvider";
import { Card } from "../../ui";

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function PublicWishlistPage(): JSX.Element {
  const { token } = useParams<{ token: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const { setPreviewTokens } = useTheme();
  const rawSort = searchParams.get("sort");
  const sort: PublicWishlistSort = rawSort === "added" ? "added" : "priority";

  const query = useQuery({
    enabled: Boolean(token),
    queryKey: ["public-wishlist", token, sort],
    queryFn: () => apiClient.getPublicWishlist(token as string, undefined, 100, sort)
  });

  useEffect(() => {
    setPreviewTokens(query.data?.themeTokens ?? defaultThemeTokens);
    return () => {
      setPreviewTokens(null);
    };
  }, [query.data?.themeTokens, setPreviewTokens]);

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
          <h1>{invalidLink ? "Invalid link" : "Could not load wishlist"}</h1>
          <p className="muted">
            {invalidLink
              ? "Check the token or ask the owner to share the wishlist again."
              : "Unexpected error while loading public view."}
          </p>
          <Link className="inline-link" to="/login">
            Open app login
          </Link>
        </Card>
      </div>
    );
  }

  if (!query.data) {
    return <div className="public-page">Loading public wishlist...</div>;
  }

  const wishlist = query.data;
  const hasItems = wishlist.items.length > 0;

  const handleSortChange = (value: PublicWishlistSort): void => {
    const nextParams = new URLSearchParams(searchParams);

    if (value === "priority") {
      nextParams.delete("sort");
    } else {
      nextParams.set("sort", value);
    }

    setSearchParams(nextParams, { replace: true });
  };

  return (
    <div className="public-page">
      <Card className="stack gap-md">
        <h1>{wishlist.title}</h1>
        {wishlist.description ? <p>{wishlist.description}</p> : null}
      </Card>

      <div className="actions-row wrap">
        <label className="ui-field" htmlFor="public-sort">
          <span className="ui-field-label">Sort items</span>
          <select
            className="ui-input"
            disabled={!hasItems}
            id="public-sort"
            onChange={(event) => handleSortChange(event.target.value as PublicWishlistSort)}
            value={sort}
          >
            <option value="priority">By importance</option>
            <option value="added">By added date</option>
          </select>
        </label>
      </div>

      <div className="stack gap-md">
        {wishlist.items.map((item) => (
          <Card className="item-card" key={item.id}>
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
          </Card>
        ))}
      </div>
    </div>
  );
}
