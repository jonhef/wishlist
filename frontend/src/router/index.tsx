import { Suspense, lazy } from "react";
import { Navigate, createBrowserRouter } from "react-router-dom";
import { AuthGuard } from "./AuthGuard";
import { AppLayout } from "./AppLayout";

const LoginPage = lazy(async () => {
  const module = await import("../pages/Login/LoginPage");
  return { default: module.LoginPage };
});

const DashboardPage = lazy(async () => {
  const module = await import("../pages/Dashboard/DashboardPage");
  return { default: module.DashboardPage };
});

const WishlistDetailPage = lazy(async () => {
  const module = await import("../pages/WishlistDetail/WishlistDetailPage");
  return { default: module.WishlistDetailPage };
});

const PublicWishlistPage = lazy(async () => {
  const module = await import("../pages/PublicWishlist/PublicWishlistPage");
  return { default: module.PublicWishlistPage };
});

const ThemeEditorPage = lazy(async () => {
  const module = await import("../pages/ThemeEditor/ThemeEditorPage");
  return { default: module.ThemeEditorPage };
});

const SmartAddPreviewPage = lazy(async () => {
  const module = await import("../pages/SmartAddPreview/SmartAddPreviewPage");
  return { default: module.SmartAddPreviewPage };
});

function withSuspense(element: JSX.Element): JSX.Element {
  return (
    <Suspense fallback={<RouteLoader />}>
      {element}
    </Suspense>
  );
}

function RouteLoader(): JSX.Element {
  return <div style={{ padding: "2rem", textAlign: "center" }}>Loading...</div>;
}

export const appRouter = createBrowserRouter([
  {
    path: "/login",
    element: withSuspense(<LoginPage />)
  },
  {
    path: "/p/:token",
    element: withSuspense(<PublicWishlistPage />)
  },
  {
    path: "/preview/smart-add",
    element: withSuspense(<SmartAddPreviewPage />)
  },
  {
    path: "/",
    element: (
      <AuthGuard>
        <AppLayout />
      </AuthGuard>
    ),
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />
      },
      {
        path: "/dashboard",
        element: withSuspense(<DashboardPage />)
      },
      {
        path: "/wishlists/:wishlistId",
        element: withSuspense(<WishlistDetailPage />)
      },
      {
        path: "/themes/editor",
        element: withSuspense(<ThemeEditorPage />)
      }
    ]
  },
  {
    path: "*",
    element: <Navigate to="/dashboard" replace />
  }
]);
