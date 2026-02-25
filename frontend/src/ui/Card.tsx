import type { HTMLAttributes } from "react";

export function Card({ className = "", ...props }: HTMLAttributes<HTMLDivElement>): JSX.Element {
  return <div className={`ui-card glow-hover ${className}`.trim()} {...props} />;
}
