import { formatHealthStatus } from "./status.mjs";

const statusEl = document.getElementById("status");

async function loadStatus() {
  try {
    const response = await fetch("/api/health");

    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`);
    }

    const payload = await response.json();
    statusEl.textContent = formatHealthStatus(payload);
  } catch {
    statusEl.textContent = "Backend status: unavailable";
  }
}

loadStatus();
setInterval(loadStatus, 5000);
