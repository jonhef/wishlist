export const smartAddStrings = {
  intro: "Умное добавление: ответь на несколько сравнений, чтобы определить важность.",
  questionTitle: "Что важнее прямо сейчас?",
  newItemLabel: "Новый предмет",
  existingItemLabel: "Текущий предмет",
  chooseNew: "Новый важнее",
  chooseExisting: "Текущий важнее",
  back: "Назад",
  cancel: "Отменить",
  skipToSimple: "Пропустить и добавить обычно",
  staleDialog: "Список изменился. Пересчитать вопросы на свежем порядке?",
  progress: (step: number, total: number) => `Вопрос ${step} из ~${total}`
} as const;
