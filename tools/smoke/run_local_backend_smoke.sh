#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_URL="${API_URL:-http://127.0.0.1:5087}"
API_PORT="${API_URL##*:}"
SIDECAR_PORT="${SIDECAR_PORT:-3333}"
DB_URL="${DB_URL:-postgresql://kirill@localhost:5432/fridgechef}"
JWT_SECRET="${JWT_SECRET:-0123456789abcdef0123456789abcdef0123456789abcdef}"
SMOKE_PASSWORD="${SMOKE_PASSWORD:-SmokePass123!}"
TMP_DIR="$(mktemp -d)"
API_PID=""
SIDECAR_PID=""
SMOKE_EMAIL=""

cleanup() {
  if [[ -n "${API_PID}" ]] && kill -0 "${API_PID}" >/dev/null 2>&1; then
    kill "${API_PID}" >/dev/null 2>&1 || true
    wait "${API_PID}" >/dev/null 2>&1 || true
  fi

  if [[ -n "${SIDECAR_PID}" ]] && kill -0 "${SIDECAR_PID}" >/dev/null 2>&1; then
    kill "${SIDECAR_PID}" >/dev/null 2>&1 || true
    wait "${SIDECAR_PID}" >/dev/null 2>&1 || true
  fi

  if [[ -n "${SMOKE_EMAIL}" ]]; then
    psql "${DB_URL}" -c "DELETE FROM auth.users WHERE email = '${SMOKE_EMAIL}';" >/dev/null 2>&1 || true
  fi

  rm -rf "${TMP_DIR}"
}
trap cleanup EXIT

require_bin() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Missing required binary: $1" >&2
    exit 1
  }
}

require_bin curl
require_bin jq
require_bin node
require_bin dotnet
require_bin psql

read_scalar() {
  psql "${DB_URL}" -Atqc "$1"
}

request() {
  local method="$1"
  local url="$2"
  local expected_status="$3"
  local token="${4:-}"
  local json_body="${5:-}"
  local body_file="$TMP_DIR/response-body.json"
  local status_file="$TMP_DIR/response-status.txt"
  local curl_args=(
    -sS
    -o "$body_file"
    -w "%{http_code}"
    -X "$method"
    "$url"
  )

  if [[ -n "$token" ]]; then
    curl_args+=(-H "Authorization: Bearer ${token}")
  fi

  if [[ -n "$json_body" ]]; then
    curl_args+=(-H "Content-Type: application/json" -d "$json_body")
  fi

  curl "${curl_args[@]}" > "$status_file"
  local actual_status
  actual_status="$(cat "$status_file")"

  if [[ "$actual_status" != "$expected_status" ]]; then
    echo "Request failed: ${method} ${url}" >&2
    echo "Expected status: ${expected_status}, got: ${actual_status}" >&2
    cat "$body_file" >&2
    exit 1
  fi

  cat "$body_file"
}

wait_for_health() {
  local attempt=0
  until curl -fsS "${API_URL}/health" >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [[ "$attempt" -gt 60 ]]; then
      echo "API did not become healthy in time" >&2
      exit 1
    fi
    sleep 1
  done
}

wait_for_sidecar() {
  local attempt=0
  until curl -fsS "http://127.0.0.1:${SIDECAR_PORT}/health" >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [[ "$attempt" -gt 30 ]]; then
      echo "Fake sidecar did not become healthy in time" >&2
      exit 1
    fi
    sleep 1
  done
}

FOOD_NODE_ID="$(read_scalar "SELECT id FROM ontology.food_nodes WHERE status = 'active' ORDER BY id LIMIT 1;")"
UNIT_ID="$(read_scalar "SELECT id FROM ontology.units ORDER BY id LIMIT 1;")"
DIET_ID="$(read_scalar "SELECT id FROM taxonomy.taxons WHERE kind = 'diet' ORDER BY id LIMIT 1;")"
RECIPE_ID="$(read_scalar "SELECT id FROM catalog.recipes WHERE status = 'published' ORDER BY created_at DESC, id LIMIT 1;")"
RECIPE_SLUG="$(read_scalar "SELECT slug FROM catalog.recipes WHERE status = 'published' ORDER BY created_at DESC, id LIMIT 1;")"

node "${ROOT_DIR}/tools/smoke/fake_pricing_sidecar.js" > "${TMP_DIR}/fake-sidecar.log" 2>&1 &
SIDECAR_PID="$!"
wait_for_sidecar

(
  cd "${ROOT_DIR}"
  export ASPNETCORE_ENVIRONMENT=Development
  export ASPNETCORE_URLS="${API_URL}"
  export Jwt__Secret="${JWT_SECRET}"
  export Pricing__AutoSync=false
  export Pricing__Sync__MaxIngredientsPerRun=3
  export Pricing__PuppeteerScraper__BaseUrl="http://127.0.0.1:${SIDECAR_PORT}"
  dotnet run --project src/FridgeChef.Api/FridgeChef.Api.csproj --no-build --no-launch-profile > "${TMP_DIR}/api.log" 2>&1
) &
API_PID="$!"

wait_for_health

request GET "${API_URL}/health" 200 >/dev/null
request GET "${API_URL}/units" 200 >/dev/null
request GET "${API_URL}/taxons?kind=diet" 200 >/dev/null
request GET "${API_URL}/taxons?kind=invalid-kind" 400 >/dev/null
request GET "${API_URL}/food-nodes?q=%D0%BC%D0%BE%D0%BB" 200 >/dev/null
request GET "${API_URL}/food-nodes/${FOOD_NODE_ID}" 200 >/dev/null
request GET "${API_URL}/recipes?page=1&pageSize=5" 200 >/dev/null
request GET "${API_URL}/recipes/${RECIPE_SLUG}" 200 >/dev/null
request GET "${API_URL}/pricing/ingredients?ids=${FOOD_NODE_ID}" 200 >/dev/null

SMOKE_EMAIL="smoke-$(date +%s)@example.test"
REGISTER_BODY="$(jq -nc \
  --arg name "Smoke Tester" \
  --arg email "${SMOKE_EMAIL}" \
  --arg password "${SMOKE_PASSWORD}" \
  '{name:$name,email:$email,password:$password,confirmPassword:$password}')"
REGISTER_RESPONSE="$(request POST "${API_URL}/auth/register" 201 "" "${REGISTER_BODY}")"
USER_TOKEN="$(echo "${REGISTER_RESPONSE}" | jq -r '.accessToken')"
REFRESH_TOKEN="$(echo "${REGISTER_RESPONSE}" | jq -r '.refreshToken')"

LOGIN_BODY="$(jq -nc --arg email "${SMOKE_EMAIL}" --arg password "${SMOKE_PASSWORD}" '{email:$email,password:$password}')"
LOGIN_RESPONSE="$(request POST "${API_URL}/auth/login" 200 "" "${LOGIN_BODY}")"
USER_TOKEN="$(echo "${LOGIN_RESPONSE}" | jq -r '.accessToken')"

REFRESH_BODY="$(jq -nc --arg refreshToken "${REFRESH_TOKEN}" '{refreshToken:$refreshToken}')"
request POST "${API_URL}/auth/refresh" 200 "" "${REFRESH_BODY}" >/dev/null

request GET "${API_URL}/users/me" 200 "${USER_TOKEN}" >/dev/null

PATCH_BODY="$(jq -nc --arg displayName "Smoke Updated" '{displayName:$displayName}')"
request PATCH "${API_URL}/users/me" 200 "${USER_TOKEN}" "${PATCH_BODY}" >/dev/null

PANTRY_CREATE_BODY="$(jq -nc --argjson foodNodeId "${FOOD_NODE_ID}" --argjson quantity 2 --argjson unitId "${UNIT_ID}" '{foodNodeId:$foodNodeId,quantity:$quantity,unitId:$unitId}')"
PANTRY_CREATE_RESPONSE="$(request POST "${API_URL}/pantry" 201 "${USER_TOKEN}" "${PANTRY_CREATE_BODY}")"
PANTRY_ITEM_ID="$(echo "${PANTRY_CREATE_RESPONSE}" | jq -r '.id')"

request GET "${API_URL}/pantry" 200 "${USER_TOKEN}" >/dev/null

PANTRY_PATCH_BODY="$(jq -nc --argjson quantity 3 '{quantity:$quantity}')"
request PATCH "${API_URL}/pantry/${PANTRY_ITEM_ID}" 200 "${USER_TOKEN}" "${PANTRY_PATCH_BODY}" >/dev/null

MATCH_BODY="$(jq -nc --argjson maxResults 5 --argjson dietId "${DIET_ID}" '{maxResults:$maxResults,dietFilterIds:[$dietId]}')"
request POST "${API_URL}/recipes/search" 200 "${USER_TOKEN}" "${MATCH_BODY}" >/dev/null

request PUT "${API_URL}/favorites/${RECIPE_ID}" 204 "${USER_TOKEN}" >/dev/null
request GET "${API_URL}/favorites" 200 "${USER_TOKEN}" >/dev/null
request DELETE "${API_URL}/favorites/${RECIPE_ID}" 204 "${USER_TOKEN}" >/dev/null

ALLERGEN_BODY="$(jq -nc --argjson foodNodeId "${FOOD_NODE_ID}" --arg severity "strict" '{foodNodeId:$foodNodeId,severity:$severity}')"
request POST "${API_URL}/settings/allergens" 204 "${USER_TOKEN}" "${ALLERGEN_BODY}" >/dev/null
request GET "${API_URL}/settings/allergens" 200 "${USER_TOKEN}" >/dev/null
request DELETE "${API_URL}/settings/allergens/${FOOD_NODE_ID}" 204 "${USER_TOKEN}" >/dev/null

FAVORITE_FOOD_BODY="$(jq -nc --argjson foodNodeId "${FOOD_NODE_ID}" '{foodNodeId:$foodNodeId}')"
request POST "${API_URL}/settings/favorite-foods" 204 "${USER_TOKEN}" "${FAVORITE_FOOD_BODY}" >/dev/null
request GET "${API_URL}/settings/favorite-foods" 200 "${USER_TOKEN}" >/dev/null
request DELETE "${API_URL}/settings/favorite-foods/${FOOD_NODE_ID}" 204 "${USER_TOKEN}" >/dev/null

EXCLUDED_FOOD_BODY="$(jq -nc --argjson foodNodeId "${FOOD_NODE_ID}" '{foodNodeId:$foodNodeId}')"
request POST "${API_URL}/settings/excluded-foods" 204 "${USER_TOKEN}" "${EXCLUDED_FOOD_BODY}" >/dev/null
request GET "${API_URL}/settings/excluded-foods" 200 "${USER_TOKEN}" >/dev/null
request DELETE "${API_URL}/settings/excluded-foods/${FOOD_NODE_ID}" 204 "${USER_TOKEN}" >/dev/null

DIETS_BODY="$(jq -nc --argjson dietId "${DIET_ID}" '{taxonIds:[$dietId]}')"
request PUT "${API_URL}/settings/diets" 204 "${USER_TOKEN}" "${DIETS_BODY}" >/dev/null
request GET "${API_URL}/settings/diets" 200 "${USER_TOKEN}" >/dev/null

CHANGE_PASSWORD_BODY="$(jq -nc --arg oldPassword "${SMOKE_PASSWORD}" --arg newPassword "${SMOKE_PASSWORD}2" '{oldPassword:$oldPassword,newPassword:$newPassword}')"
request POST "${API_URL}/users/me/change-password" 204 "${USER_TOKEN}" "${CHANGE_PASSWORD_BODY}" >/dev/null

LOGIN_NEW_BODY="$(jq -nc --arg email "${SMOKE_EMAIL}" --arg password "${SMOKE_PASSWORD}2" '{email:$email,password:$password}')"
ADMIN_LOGIN_RESPONSE="$(request POST "${API_URL}/auth/login" 200 "" "${LOGIN_NEW_BODY}")"
USER_TOKEN="$(echo "${ADMIN_LOGIN_RESPONSE}" | jq -r '.accessToken')"

psql "${DB_URL}" -c "UPDATE auth.users SET role = 'admin' WHERE email = '${SMOKE_EMAIL}';" >/dev/null
ADMIN_LOGIN_RESPONSE="$(request POST "${API_URL}/auth/login" 200 "" "${LOGIN_NEW_BODY}")"
ADMIN_TOKEN="$(echo "${ADMIN_LOGIN_RESPONSE}" | jq -r '.accessToken')"

request GET "${API_URL}/admin/pricing/status" 200 "${ADMIN_TOKEN}" >/dev/null

SEARCH_TEST_BODY="$(jq -nc --arg query "молоко" '{query:$query}')"
request POST "${API_URL}/admin/pricing/search-test" 200 "${ADMIN_TOKEN}" "${SEARCH_TEST_BODY}" >/dev/null
request POST "${API_URL}/admin/pricing/reconnect" 200 "${ADMIN_TOKEN}" "{}" >/dev/null
request POST "${API_URL}/admin/pricing/sync" 200 "${ADMIN_TOKEN}" "{}" >/dev/null

request DELETE "${API_URL}/pantry/${PANTRY_ITEM_ID}" 204 "${USER_TOKEN}" >/dev/null
request POST "${API_URL}/auth/logout" 204 "${ADMIN_TOKEN}" "{}" >/dev/null

echo "Local backend smoke passed"
