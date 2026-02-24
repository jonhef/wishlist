import assert from "node:assert/strict";
import test from "node:test";

import { buildHealthPayload, createHandler } from "./server.mjs";

test("buildHealthPayload returns expected shape", () => {
  const payload = buildHealthPayload();

  assert.equal(payload.service, "backend");
  assert.equal(payload.status, "ok");
  assert.equal(typeof payload.timestamp, "string");
  assert.ok(!Number.isNaN(Date.parse(payload.timestamp)));
});

test("createHandler returns a function", () => {
  const handler = createHandler();

  assert.equal(typeof handler, "function");
});
