import fs from "node:fs/promises";
import http from "node:http";
import https from "node:https";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const HOST = process.env.HOST || "0.0.0.0";
const PORT = Number(process.env.PORT) || 5173;
const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:8080";
const STATIC_ROOT = path.join(__dirname, "src");

const contentTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".mjs": "text/javascript; charset=utf-8"
};

function sendJson(res, statusCode, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body)
  });
  res.end(body);
}

function resolveStaticPath(requestPath) {
  const pathname = requestPath === "/" ? "/index.html" : requestPath;
  const normalizedPath = path.normalize(pathname).replace(/^([.][.][/\\])+/, "");
  return path.join(STATIC_ROOT, normalizedPath);
}

async function serveStatic(req, res) {
  const filePath = resolveStaticPath(req.url || "/");

  if (!filePath.startsWith(STATIC_ROOT)) {
    sendJson(res, 400, { error: "Invalid path" });
    return;
  }

  try {
    const data = await fs.readFile(filePath);
    const ext = path.extname(filePath);

    res.writeHead(200, {
      "Content-Type": contentTypes[ext] || "application/octet-stream",
      "Content-Length": data.length
    });
    res.end(data);
  } catch {
    sendJson(res, 404, { error: "Not Found" });
  }
}

function proxyApi(req, res) {
  const incomingUrl = req.url || "/";
  const upstreamPath = incomingUrl.replace(/^\/api/, "") || "/";
  const targetUrl = new URL(upstreamPath, BACKEND_URL);
  const transport = targetUrl.protocol === "https:" ? https : http;

  const proxyReq = transport.request(
    {
      hostname: targetUrl.hostname,
      method: req.method,
      path: `${targetUrl.pathname}${targetUrl.search}`,
      port: targetUrl.port || (targetUrl.protocol === "https:" ? 443 : 80),
      headers: req.headers
    },
    (proxyRes) => {
      res.writeHead(proxyRes.statusCode || 502, proxyRes.headers);
      proxyRes.pipe(res);
    }
  );

  proxyReq.on("error", () => {
    sendJson(res, 502, { error: "Backend unavailable" });
  });

  req.pipe(proxyReq);
}

const server = http.createServer((req, res) => {
  if ((req.url || "").startsWith("/api")) {
    proxyApi(req, res);
    return;
  }

  serveStatic(req, res);
});

server.listen(PORT, HOST, () => {
  console.log(`[frontend] listening on http://${HOST}:${PORT}`);
});
