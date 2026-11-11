#!/usr/bin/env bash
set -euo pipefail

base_url="http://127.0.0.1:5289"
output_dir="$(mktemp -d)"
app_pid=""

cleanup() {
    if [ -n "$app_pid" ]; then
        kill "$app_pid" 2>/dev/null || true
        wait "$app_pid" 2>/dev/null || true
    fi
    rm -rf "$output_dir"
}
trap cleanup EXIT

ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$base_url" \
    dotnet run --project src/IbdaaAtelier.Web/IbdaaAtelier.Web.csproj \
    --configuration Release --no-build --no-launch-profile \
    >"$output_dir/app.log" 2>&1 &
app_pid=$!

ready=0
for attempt in $(seq 1 45); do
    if curl --silent --fail "$base_url/Auth/Login" -o "$output_dir/login.html"; then
        ready=1
        break
    fi
    if ! kill -0 "$app_pid" 2>/dev/null; then
        break
    fi
    sleep 1
done

if [ "$ready" -ne 1 ]; then
    cat "$output_dir/app.log"
    echo "Login page did not start on local HTTP test port."
    exit 1
fi

echo "HTTP smoke: login markup"
grep -q 'Sign In to IBDAA Atelier' "$output_dir/login.html"
grep -q 'name="__RequestVerificationToken"' "$output_dir/login.html"
grep -q 'name="Password"' "$output_dir/login.html"

echo "HTTP smoke: static CSS headers"
curl --fail --silent --show-error -D "$output_dir/css.headers" \
    "$base_url/css/site.css" -o "$output_dir/site.css"
grep -q -- '--atelier-primary:' "$output_dir/site.css"
grep -iq 'x-content-type-options: nosniff' "$output_dir/css.headers"

echo "HTTP smoke: Arabic RTL response"
curl --fail --silent --show-error \
    -H 'Cookie: .AspNetCore.Culture=c%3Dar-KW%7Cuic%3Dar-KW' \
    "$base_url/Auth/Login" -o "$output_dir/arabic.html"
grep -q '<html lang="ar" dir="rtl">' "$output_dir/arabic.html"
python3 - "$output_dir/arabic.html" <<'PY'
import html
import sys
from pathlib import Path

content = html.unescape(Path(sys.argv[1]).read_text())
if "تسجيل الدخول" not in content:
    raise SystemExit("Arabic login heading is missing.")
PY

echo "HTTP smoke: protected routes"
for path in /Admin /Pos /Payments/Take /Refunds/Issue; do
    status="$(curl --silent -o /dev/null -w '%{http_code}' "$base_url$path")"
    if [ "$status" != 302 ]; then
        echo "Expected anonymous $path to redirect to login, got HTTP $status."
        exit 1
    fi
done

echo "HTTP smoke: antiforgery enforcement"
status="$(curl --silent -o /dev/null -w '%{http_code}' \
    -X POST -d 'UserName=test&Password=test' "$base_url/Auth/Login")"
if [ "$status" != 400 ]; then
    echo "Expected login POST without antiforgery token to fail, got HTTP $status."
    exit 1
fi

browser="$(command -v google-chrome || command -v chromium || true)"
if [ -z "$browser" ]; then
    echo "Headless Chrome is required for Login screenshots."
    exit 1
fi

mkdir -p tests/ui-output
for viewport in desktop:1440,900 mobile:390,844; do
    name="${viewport%%:*}"
    size="${viewport#*:}"
    "$browser" --headless=new --no-sandbox --disable-gpu \
        --disable-dev-shm-usage --hide-scrollbars \
        --user-data-dir="$output_dir/chrome-$name" \
        --window-size="$size" --virtual-time-budget=1500 \
        --screenshot="$PWD/tests/ui-output/login-$name.png" \
        "$base_url/Auth/Login" >"$output_dir/chrome-$name.log" 2>&1 || {
            cat "$output_dir/chrome-$name.log"
            exit 1
        }
    test -s "tests/ui-output/login-$name.png"
done

echo "HTTP smoke passed: login, RTL, static CSS, protected routes and CSRF."
