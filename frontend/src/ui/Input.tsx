import type { InputHTMLAttributes } from "react";

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
  id: string;
};

export function Input({ label, id, className = "", ...props }: InputProps): JSX.Element {
  return (
    <label className="ui-field" htmlFor={id}>
      {label ? <span className="ui-field-label">{label}</span> : null}
      <input id={id} className={`ui-input glow-focus ${className}`.trim()} {...props} />
    </label>
  );
}
