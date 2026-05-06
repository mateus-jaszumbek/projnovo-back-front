#!/bin/bash
# Faz deploy ou atualização da aplicação na EC2
# Uso: ./scripts/deploy.sh <ip-da-ec2> <caminho-da-chave-pem>
# Exemplo: ./scripts/deploy.sh 54.123.45.67 ~/.ssh/minha-chave.pem
set -euo pipefail

EC2_IP="${1:?Informe o IP da EC2: ./deploy.sh <ip> <chave.pem>}"
PEM_KEY="${2:?Informe o caminho da chave PEM: ./deploy.sh <ip> <chave.pem>}"
REMOTE_USER="ubuntu"
REMOTE_DIR="/home/ubuntu/servicosapp"

echo "==> Enviando arquivos para EC2 ($EC2_IP)..."
rsync -avz --progress \
  -e "ssh -i $PEM_KEY -o StrictHostKeyChecking=no" \
  --exclude='.git' \
  --exclude='node_modules' \
  --exclude='**/bin' \
  --exclude='**/obj' \
  --exclude='.env.aws' \
  ./ "$REMOTE_USER@$EC2_IP:$REMOTE_DIR/"

echo "==> Enviando arquivo .env.aws (se existir localmente)..."
if [ -f ".env.aws" ]; then
  scp -i "$PEM_KEY" -o StrictHostKeyChecking=no \
    .env.aws "$REMOTE_USER@$EC2_IP:$REMOTE_DIR/.env.aws"
else
  echo "    AVISO: .env.aws não encontrado localmente. Certifique-se de que ele existe na EC2."
fi

echo "==> Iniciando containers na EC2..."
ssh -i "$PEM_KEY" -o StrictHostKeyChecking=no "$REMOTE_USER@$EC2_IP" bash <<'ENDSSH'
  set -euo pipefail
  cd /home/ubuntu/servicosapp

  if [ ! -f ".env.aws" ]; then
    echo "ERRO: arquivo .env.aws não existe em $REMOTE_DIR"
    echo "Copie .env.aws.example para .env.aws e preencha os valores."
    exit 1
  fi

  echo "==> Parando containers antigos (se houver)..."
  docker compose -f docker-compose.aws.yml --env-file .env.aws down --remove-orphans || true

  echo "==> Fazendo build e subindo containers..."
  docker compose -f docker-compose.aws.yml --env-file .env.aws up -d --build

  echo "==> Aguardando serviços ficarem saudáveis..."
  sleep 10
  docker compose -f docker-compose.aws.yml ps

  echo ""
  echo "===================================================="
  echo " Deploy concluído!"
  echo " Acesse: http://$(curl -s ifconfig.me 2>/dev/null || echo '<SEU-IP>')"
  echo "===================================================="
ENDSSH
