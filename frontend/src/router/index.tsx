import { Navigate, createBrowserRouter } from "react-router-dom";
import { AuthGuard } from "./AuthGuard";
import { AppLayout } from "./AppLayout";
import { LoginPage } from "../pages/Login/LoginPage";
import { DashboardPage } from "../pages/Dashboard/DashboardPage";
import { WishlistDetailPage } from "../pages/WishlistDetail/WishlistDetailPage";
import { PublicWishlistPage } from "../pages/PublicWishlist/PublicWishlistPage";
import { ThemeEditorPage } from "../pages/ThemeEditor/ThemeEditorPage";

export const appRouter = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />
  },
  {
    path: "/p/:token",
    element: <PublicWishlistPage />
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
        element: <DashboardPage />
      },
      {
        path: "/wishlists/:wishlistId",
        element: <WishlistDetailPage />
      },
      {
        path: "/themes/editor",
        element: <ThemeEditorPage />
      }
    ]
  },
  {
    path: "*",
    element: <Navigate to="/dashboard" replace />
  }
]);
