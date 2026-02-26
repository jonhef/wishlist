import { describe, expect, it } from "vitest";
import {
  applyChoice,
  currentMid,
  initBinaryInsert,
  isBinaryInsertDone,
  maxQuestionsEstimate,
  resultPos,
  undoChoice
} from "./binaryInsert";

describe("binaryInsert state machine", () => {
  it("handles empty list with zero questions", () => {
    const state = initBinaryInsert(0);
    expect(isBinaryInsertDone(state)).toBe(true);
    expect(currentMid(state)).toBeNull();
    expect(resultPos(state)).toBe(0);
    expect(maxQuestionsEstimate(0)).toBe(0);
  });

  it("handles one item with one question", () => {
    const initial = initBinaryInsert(1);
    expect(currentMid(initial)).toBe(0);

    const done = applyChoice(initial, "existing");
    expect(isBinaryInsertDone(done)).toBe(true);
    expect(resultPos(done)).toBe(1);
    expect(done.history).toHaveLength(1);
    expect(maxQuestionsEstimate(1)).toBe(1);
  });

  it("undo restores exact previous bounds", () => {
    const initial = initBinaryInsert(10);
    const step1 = applyChoice(initial, "existing");
    const step2 = applyChoice(step1, "new");
    const rolledBack = undoChoice(step2);

    expect(rolledBack.low).toBe(step1.low);
    expect(rolledBack.high).toBe(step1.high);
    expect(rolledBack.history).toHaveLength(step1.history.length);
  });

  it("full run does not exceed ceil(log2(n+1))", () => {
    let state = initBinaryInsert(100);

    while (!isBinaryInsertDone(state)) {
      state = applyChoice(state, "existing");
    }

    expect(state.history.length).toBeLessThanOrEqual(maxQuestionsEstimate(100));
  });
});
