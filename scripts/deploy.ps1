# Deploy para EC2 via SCP + SSH
# Uso: .\scripts\deploy.ps1 -Ip 52.207.193.4 -Key "C:\Users\mateu\Downloads\servicosapp-key.pem"
param(
    [Parameter(Mandatory)][string]$Ip,
    [Parameter(Mandatory)][string]$Key,
    [string]$User = "ubuntu",
    [string]$RemoteDir = "/home/ubuntu/servicosapp"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

Write-Host "==> Verificando .env.aws..."
$EnvFile = Join-Path $ProjectRoot ".env.aws"
if (-not (Test-Path $EnvFile)) {
    Write-Error "Arquivo .env.aws não encontrado em $ProjectRoot`nCopie .env.aws.example para .env.aws e preencha os valores."
}

Write-Host "==> Criando arquivo zip do projeto..."
$ZipPath = Join-Path $env:TEMP "servicosapp-deploy.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

$Exclude = @('.git', 'node_modules', 'bin', 'obj', '.env.aws', '.env', '*.user', '.vs')
$TempDir = Join-Path $env:TEMP "servicosapp-deploy-src"
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir | Out-Null

# Copia arquivos excluindo pastas pesadas
Get-ChildItem $ProjectRoot -Force | Where-Object { $_.Name -notin $Exclude } | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $TempDir $_.Name) -Recurse -Force
}

Compress-Archive -Path "$TempDir\*" -DestinationPath $ZipPath -Force
Remove-Item $TempDir -Recurse -Force
$ZipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host "    Zip criado: $ZipPath ($ZipSize MB)"

Write-Host "==> Criando pasta remota..."
ssh -i $Key -o StrictHostKeyChecking=no "${User}@${Ip}" "mkdir -p $RemoteDir"

Write-Host "==> Enviando zip para EC2..."
scp -i $Key -o StrictHostKeyChecking=no $ZipPath "${User}@${Ip}:$RemoteDir/deploy.zip"

Write-Host "==> Enviando .env.aws..."
scp -i $Key -o StrictHostKeyChecking=no $EnvFile "${User}@${Ip}:$RemoteDir/.env.aws"

Write-Host "==> Extraindo e subindo containers na EC2..."
$RemoteScript = @"
set -euo pipefail
cd $RemoteDir

echo '--> Extraindo arquivos...'
unzip -o deploy.zip -d . > /dev/null
rm deploy.zip

echo '--> Parando containers antigos...'
docker compose -f docker-compose.aws.yml --env-file .env.aws down --remove-orphans 2>/dev/null || true

echo '--> Build e inicialização dos containers...'
docker compose -f docker-compose.aws.yml --env-file .env.aws up -d --build

echo '--> Aguardando 15s para serviços iniciarem...'
sleep 15
docker compose -f docker-compose.aws.yml ps
echo ''
echo '===================================================='
echo " Acesse: http://$Ip"
echo '===================================================='
"@

ssh -i $Key -o StrictHostKeyChecking=no "${User}@${Ip}" "bash -s" <<< $RemoteScript

Write-Host ""
Write-Host "Deploy concluído! Acesse: http://$Ip" -ForegroundColor Green
