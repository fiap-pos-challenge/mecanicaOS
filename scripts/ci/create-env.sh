#!/usr/bin/env bash
# Cria um .env descartável a partir do .env.example, trocando cada segredo por um valor aleatório.
# Usado só no CI/CD; localmente basta copiar o .env.example.
set -euo pipefail

cp .env.example .env
random() { openssl rand -hex "$1"; }
replace() {
  local name="$1" value="$2"
  echo "::add-mask::${value}"
  sed -i "s|^${name}=.*|${name}=${value}|" .env
}

replace MSSQL_SA_PASSWORD "Ci@$(random 12)Aa1"
replace JWT_SECRET "$(random 32)"
replace PIX_WEBHOOK_SECRET "$(random 16)"
replace DEV_ADMIN_PASSWORD "Ci@$(random 8)Aa1"
replace SEED_USERS_PASSWORD "Ci@$(random 8)Aa1"
replace SONAR_ADMIN_PASSWORD "Ci@$(random 8)Aa1"
