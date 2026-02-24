import assert from "node:assert/strict";
import test from "node:test";

import { formatHealthStatus } from "./status.mjs";

test("returns success message for healthy payload", () => {
  const message = formatHealthStatus({ service: "backend", status: "ok" });

  assert.equal(message, "Backend status: ok (backend)");
});

test("returns fallback message for invalid payload", () => {
  const message = formatHealthStatus(null);

  assert.equal(message, "Backend status: unavailable");
});
