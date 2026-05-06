# Deploy via Git na EC2
# Uso: .\scripts\deploy-git.ps1 -Ip 52.207.193.4 -Key "C:\caminho\chave.pem"
param(
    [Parameter(Mandatory)][string]$Ip,
    [Parameter(Mandatory)][string]$Key,
    [string]$User = "ec2-user",
    [string]$RemoteDir = "/home/ec2-user/servicosapp",
    [string]$Repo = "https://github.com/mateus-jaszumbek/projnovo-back-front.git",
    [string]$Branch = "main"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

Write-Host "==> Verificando .env.aws..."
$EnvFile = Join-Path $ProjectRoot ".env.aws"
if (-not (Test-Path $EnvFile)) {
    Write-Error "Arquivo .env.aws nao encontrado em $ProjectRoot`nCopie .env.aws.example para .env.aws e preencha os valores."
}

Write-Host "==> Enviando .env.aws para EC2..."
ssh -i $Key -o StrictHostKeyChecking=no "${User}@${Ip}" "mkdir -p $RemoteDir"
scp -i $Key -o StrictHostKeyChecking=no $EnvFile "${User}@${Ip}:$RemoteDir/.env.aws"

Write-Host "==> Executando deploy na EC2..."
$RemoteScript = @"
set -euo pipefail

if [ -d "$RemoteDir/.git" ]; then
  echo '--> Atualizando repositorio...'
  cd $RemoteDir
  git fetch origin
  git reset --hard origin/$Branch
else
  echo '--> Clonando repositorio...'
  git clone --branch $Branch $Repo $RemoteDir
  cd $RemoteDir
fi

echo '--> Parando containers antigos...'
docker compose -f docker-compose.aws.yml --env-file .env.aws down --remove-orphans 2>/dev/null || true

echo '--> Build e inicializacao dos containers...'
docker compose -f docker-compose.aws.yml --env-file .env.aws up -d --build

echo '--> Aguardando servicos iniciarem...'
sleep 15
docker compose -f docker-compose.aws.yml ps

echo ''
echo '===================================================='
echo " Acesse: http://$Ip"
echo '===================================================='
"@

ssh -i $Key -o StrictHostKeyChecking=no "${User}@${Ip}" "bash -s" <<< $RemoteScript

Write-Host ""
Write-Host "Deploy concluido! Acesse: http://$Ip" -ForegroundColor Green
