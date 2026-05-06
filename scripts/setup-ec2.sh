#!/bin/bash
# Executa UMA VEZ na EC2 para instalar Docker e dependências
set -euo pipefail

echo "==> Atualizando pacotes..."
sudo apt-get update -y
sudo apt-get upgrade -y

echo "==> Instalando Docker..."
sudo apt-get install -y ca-certificates curl gnupg lsb-release

sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update -y
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo "==> Adicionando usuário ubuntu ao grupo docker..."
sudo usermod -aG docker ubuntu

echo "==> Habilitando Docker na inicialização..."
sudo systemctl enable docker
sudo systemctl start docker

echo "==> Instalando utilitários..."
sudo apt-get install -y git curl wget unzip

echo ""
echo "===================================================="
echo " Setup concluído! Faça logout e login novamente"
echo " para aplicar as permissões do grupo docker."
echo "===================================================="
