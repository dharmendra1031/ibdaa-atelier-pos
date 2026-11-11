#!/usr/bin/env bash
set -euo pipefail

container_name="ibdaa-sql-ci"
ci_password="CiOnly_$(openssl rand -hex 12)_A1!"
trap 'docker rm -f "$container_name" >/dev/null 2>&1 || true' EXIT

docker run --name "$container_name" --detach \
    --env ACCEPT_EULA=Y --env MSSQL_SA_PASSWORD="$ci_password" \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null

sqlcmd="/opt/mssql-tools18/bin/sqlcmd"
if ! docker exec "$container_name" test -x "$sqlcmd"; then
    sqlcmd="/opt/mssql-tools/bin/sqlcmd"
fi

ready=0
for attempt in $(seq 1 90); do
    if docker exec "$container_name" "$sqlcmd" -S localhost -U sa -P "$ci_password" -C -b \
        -Q "SELECT 1" >/dev/null 2>&1; then
        ready=1
        break
    fi
    sleep 2
done
if [ "$ready" -ne 1 ]; then
    echo "SQL Server test container did not become ready."
    docker logs --tail 40 "$container_name"
    exit 1
fi

docker exec "$container_name" "$sqlcmd" -S localhost -U sa -P "$ci_password" -C -b \
    -Q "CREATE DATABASE IBDAA_CI" >/dev/null

run_script() {
    local file="$1"
    echo "Running $file on isolated IBDAA_CI"
    sed '0,/__SET_TEST_DATABASE_NAME__/s/__SET_TEST_DATABASE_NAME__/IBDAA_CI/' "$file" | \
        docker exec -i "$container_name" "$sqlcmd" -S localhost -U sa \
        -P "$ci_password" -C -b -d IBDAA_CI
}

for file in \
    database/001_initial_catalog.sql \
    database/003_user_authentication.sql \
    database/005_measurement_uniqueness.sql \
    database/007_customer_masters.sql \
    database/009_alteration_services.sql \
    database/011_unpaid_orders.sql; do
    run_script "$file"
done

for file in \
    database/002_verify_catalog.sql \
    database/004_verify_user_authentication.sql \
    database/006_verify_measurement_uniqueness.sql \
    database/008_verify_customer_masters.sql \
    database/010_verify_alteration_services.sql \
    database/012_verify_unpaid_orders.sql; do
    run_script "$file"
done

run_script tests/sql/order-schema-smoke.sql
echo "SQL migrations, identity keys and constraint smoke test passed."
