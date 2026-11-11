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

grep -q 'Sign In to IBDAA Atelier' "$output_dir/login.html"
grep -q 'name="__RequestVerificationToken"' "$output_dir/login.html"
grep -q 'name="Password"' "$output_dir/login.html"

curl --fail --silent --show-error -D "$output_dir/css.headers" \
    "$base_url/css/site.css" -o "$output_dir/site.css"
grep -q -- '--atelier-primary:' "$output_dir/site.css"
grep -iq 'x-content-type-options: nosniff' "$output_dir/css.headers"

curl --fail --silent --show-error \
    -H 'Cookie: .AspNetCore.Culture=c%3Dar-KW%7Cuic%3Dar-KW' \
    "$base_url/Auth/Login" -o "$output_dir/arabic.html"
grep -q '<html lang="ar" dir="rtl">' "$output_dir/arabic.html"
grep -q 'تسجيل الدخول' "$output_dir/arabic.html"

for path in /Admin /Pos /Payments/Take /Refunds/Issue; do
    status="$(curl --silent -o /dev/null -w '%{http_code}' "$base_url$path")"
    if [ "$status" != 302 ]; then
        echo "Expected anonymous $path to redirect to login, got HTTP $status."
        exit 1
    fi
done

status="$(curl --silent -o /dev/null -w '%{http_code}' \
    -X POST -d 'UserName=test&Password=test' "$base_url/Auth/Login")"
if [ "$status" != 400 ]; then
    echo "Expected login POST without antiforgery token to fail, got HTTP $status."
    exit 1
fi

echo "HTTP smoke passed: login, RTL, static CSS, protected routes and CSRF."
