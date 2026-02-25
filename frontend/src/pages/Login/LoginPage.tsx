import { FormEvent, useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthProvider";
import { ApiError } from "../../api/client";
import { Button, Card, Input, useToast } from "../../ui";

type RouterState = {
  from?: string;
};

function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function LoginPage(): JSX.Element {
  const { isAuthenticated, login, registerAndLogin } = useAuth();
  const { showToast } = useToast();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isRegisterMode, setIsRegisterMode] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const location = useLocation();
  const navigate = useNavigate();
  const from = (location.state as RouterState | null)?.from ?? "/dashboard";

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      if (isRegisterMode) {
        await registerAndLogin(email.trim(), password);
        showToast("Account created", "success");
      } else {
        await login(email.trim(), password);
        showToast("Logged in", "success");
      }

      navigate(from, { replace: true });
    } catch (error) {
      if (isApiError(error)) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Could not complete authentication request.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="auth-page">
      <Card className="auth-card">
        <h1>{isRegisterMode ? "Create account" : "Login"}</h1>

        <form className="stack" onSubmit={handleSubmit}>
          <Input
            autoComplete="email"
            id="email"
            label="Email"
            onChange={(event) => setEmail(event.target.value)}
            required
            type="email"
            value={email}
          />

          <Input
            autoComplete={isRegisterMode ? "new-password" : "current-password"}
            id="password"
            label="Password"
            minLength={6}
            onChange={(event) => setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />

          <label className="switch-row" htmlFor="mode-switch">
            <input
              checked={isRegisterMode}
              id="mode-switch"
              onChange={(event) => setIsRegisterMode(event.target.checked)}
              type="checkbox"
            />
            <span>Create new account</span>
          </label>

          {errorMessage ? <p className="form-error">{errorMessage}</p> : null}

          <Button disabled={isSubmitting} type="submit">
            {isSubmitting ? "Please wait..." : isRegisterMode ? "Create account" : "Login"}
          </Button>
        </form>
      </Card>
    </div>
  );
}
