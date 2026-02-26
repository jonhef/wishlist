export type BinaryChoice = "new" | "existing";

type BinarySnapshot = {
  low: number;
  high: number;
};

export type BinaryInsertState = {
  low: number;
  high: number;
  total: number;
  history: BinarySnapshot[];
};

export function initBinaryInsert(total: number): BinaryInsertState {
  if (!Number.isInteger(total) || total < 0) {
    throw new Error("total must be a non-negative integer");
  }

  return {
    low: 0,
    high: total,
    total,
    history: []
  };
}

export function isBinaryInsertDone(state: BinaryInsertState): boolean {
  return state.low >= state.high;
}

export function currentMid(state: BinaryInsertState): number | null {
  if (isBinaryInsertDone(state)) {
    return null;
  }

  return Math.floor((state.low + state.high) / 2);
}

export function applyChoice(state: BinaryInsertState, choice: BinaryChoice): BinaryInsertState {
  const mid = currentMid(state);

  if (mid === null) {
    return state;
  }

  const history = [...state.history, { low: state.low, high: state.high }];

  if (choice === "new") {
    return {
      ...state,
      high: mid,
      history
    };
  }

  return {
    ...state,
    low: mid + 1,
    history
  };
}

export function undoChoice(state: BinaryInsertState): BinaryInsertState {
  const previous = state.history[state.history.length - 1];
  if (!previous) {
    return state;
  }

  return {
    ...state,
    low: previous.low,
    high: previous.high,
    history: state.history.slice(0, -1)
  };
}

export function resultPos(state: BinaryInsertState): number {
  return state.low;
}

export function maxQuestionsEstimate(total: number): number {
  if (total <= 0) {
    return 0;
  }

  return Math.ceil(Math.log2(total + 1));
}
