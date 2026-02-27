#!/usr/bin/env sh
set -eu

DOMAIN="${NGINX_DOMAIN:-wishlist.jonhef.org}"
GLOBAL_CERT_DIR="${CLOUDFLARE_GLOBAL_CONFIG_DIR:-/etc/ssl/cloudflare}"
TARGET_DIR="/etc/nginx/certs"
TARGET_CERT="${TARGET_DIR}/tls.crt"
TARGET_KEY="${TARGET_DIR}/tls.key"

mkdir -p "${TARGET_DIR}"

find_first_file() {
  search_dir="$1"
  shift

  for candidate in "$@"; do
    path="${search_dir}/${candidate}"
    if [ -f "${path}" ]; then
      echo "${path}"
      return 0
    fi
  done

  return 1
}

cert_path=""
key_path=""

if [ -n "${CLOUDFLARE_CERT_FILE:-}" ] && [ -f "${CLOUDFLARE_CERT_FILE}" ]; then
  cert_path="${CLOUDFLARE_CERT_FILE}"
fi

if [ -n "${CLOUDFLARE_KEY_FILE:-}" ] && [ -f "${CLOUDFLARE_KEY_FILE}" ]; then
  key_path="${CLOUDFLARE_KEY_FILE}"
fi

if [ -z "${cert_path}" ]; then
  cert_path="$(find_first_file "${GLOBAL_CERT_DIR}" \
    "${DOMAIN}.crt" \
    "${DOMAIN}.pem" \
    "certs/${DOMAIN}.crt" \
    "certs/${DOMAIN}.pem" \
    "origin/${DOMAIN}.crt" \
    "origin/${DOMAIN}.pem" || true)"
fi

if [ -z "${key_path}" ]; then
  key_path="$(find_first_file "${GLOBAL_CERT_DIR}" \
    "${DOMAIN}.key" \
    "certs/${DOMAIN}.key" \
    "origin/${DOMAIN}.key" || true)"
fi

if [ -z "${cert_path}" ]; then
  cert_path="$(find "${GLOBAL_CERT_DIR}" -type f \( -name "*${DOMAIN}*.crt" -o -name "*${DOMAIN}*.pem" \) | head -n 1 || true)"
fi

if [ -z "${key_path}" ]; then
  key_path="$(find "${GLOBAL_CERT_DIR}" -type f -name "*${DOMAIN}*.key" | head -n 1 || true)"
fi

if [ -z "${cert_path}" ] || [ -z "${key_path}" ]; then
  echo "[nginx-bootstrap] Could not auto-discover Cloudflare cert/key for ${DOMAIN}." >&2
  echo "[nginx-bootstrap] Checked directory: ${GLOBAL_CERT_DIR}" >&2
  echo "[nginx-bootstrap] Set CLOUDFLARE_CERT_FILE and CLOUDFLARE_KEY_FILE to override." >&2
  exit 1
fi

cp "${cert_path}" "${TARGET_CERT}"
cp "${key_path}" "${TARGET_KEY}"
chmod 600 "${TARGET_KEY}"

echo "[nginx-bootstrap] Using certificate: ${cert_path}"
echo "[nginx-bootstrap] Using key: ${key_path}"

exec nginx -g 'daemon off;'
