import http from "node:http";

export function buildHealthPayload() {
  return {
    service: "backend",
    status: "ok",
    timestamp: new Date().toISOString()
  };
}

export function createHandler() {
  return (req, res) => {
    if (req.url === "/health") {
      const payload = JSON.stringify(buildHealthPayload());

      res.writeHead(200, {
        "Content-Type": "application/json",
        "Content-Length": Buffer.byteLength(payload)
      });
      res.end(payload);
      return;
    }

    const notFound = JSON.stringify({ error: "Not Found" });
    res.writeHead(404, {
      "Content-Type": "application/json",
      "Content-Length": Buffer.byteLength(notFound)
    });
    res.end(notFound);
  };
}

export function startServer({
  host = process.env.HOST || "0.0.0.0",
  port = Number(process.env.PORT) || 8080
} = {}) {
  const server = http.createServer(createHandler());

  server.listen(port, host, () => {
    console.log(`[backend] listening on http://${host}:${port}`);
  });

  return server;
}
