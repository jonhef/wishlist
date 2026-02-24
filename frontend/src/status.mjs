export function formatHealthStatus(payload) {
  if (!payload || payload.status !== "ok") {
    return "Backend status: unavailable";
  }

  return `Backend status: ${payload.status} (${payload.service})`;
}
