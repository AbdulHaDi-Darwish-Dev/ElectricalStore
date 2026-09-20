#!/bin/sh
# Render nginx.conf from template using STORE_HOST (required).
set -eu
: "${STORE_HOST:?STORE_HOST is required (e.g. store.example.com)}"

# Prefer envsubst when available; otherwise sed (STORE_HOST must not contain '|').
if command -v envsubst >/dev/null 2>&1; then
  envsubst '${STORE_HOST}' < /etc/nginx/templates/nginx.conf.template > /etc/nginx/nginx.conf
else
  sed "s/\${STORE_HOST}/${STORE_HOST}/g" \
    /etc/nginx/templates/nginx.conf.template > /etc/nginx/nginx.conf
fi

nginx -t
exec nginx -g 'daemon off;'
