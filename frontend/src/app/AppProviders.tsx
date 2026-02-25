import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "react-router-dom";
import { useMemo } from "react";
import { AuthProvider } from "../auth/AuthProvider";
import { appRouter } from "../router";
import { ThemeProvider } from "../theme/ThemeProvider";
import { ToastProvider } from "../ui";

export function AppProviders(): JSX.Element {
  const queryClient = useMemo(() => new QueryClient(), []);

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ThemeProvider>
          <ToastProvider>
            <RouterProvider router={appRouter} />
          </ToastProvider>
        </ThemeProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}
