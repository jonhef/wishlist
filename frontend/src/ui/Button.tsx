import type { ButtonHTMLAttributes } from "react";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
};

export function Button({ variant = "primary", className = "", ...props }: ButtonProps): JSX.Element {
  return <button className={`ui-button glow-focus ui-button-${variant} ${className}`.trim()} {...props} />;
}
