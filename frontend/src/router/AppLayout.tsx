import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import { Button } from "../ui/Button";

export function AppLayout(): JSX.Element {
  const { email, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async (): Promise<void> => {
    await logout();
    navigate("/login", { replace: true });
  };

  return (
    <div className="app-layout">
      <aside className="sidebar">
        <h1 className="brand">Wishboard</h1>
        <nav className="sidebar-nav" aria-label="Main navigation">
          <NavLink to="/dashboard" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            Dashboard
          </NavLink>
          <NavLink to="/themes/editor" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            Theme Editor
          </NavLink>
        </nav>
      </aside>

      <div className="layout-main">
        <header className="topbar">
          <span className="topbar-user">{email ?? "Unknown user"}</span>
          <Button variant="secondary" onClick={handleLogout}>
            Logout
          </Button>
        </header>
        <main className="page-shell">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
