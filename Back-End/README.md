# Back-End

Este diretório é a raiz de deploy da stack Docker do projeto.

## Estrutura

- `docker-compose.yml`: sobe API e front a partir deste diretório
- `Dockerfile`: build da API ASP.NET Core
- `../Front-End/Dockerfile`: build do front Vite/Nginx

## Subir localmente com Docker

1. Copie o arquivo de ambiente:

```bash
cp .env.example .env
```

2. Ajuste os valores necessários no `.env`.

3. Suba a stack:

```bash
docker compose up -d --build
docker compose ps
```

O front ficará disponível na porta definida por `HTTP_PORT` no `.env`, por padrão `8081`.

## Deploy via Git na EC2

Depois do primeiro clone no servidor:

```bash
cd ~/servicosapp-repo
git pull origin main
cd ~/servicosapp-repo/Back-End
docker compose up -d --build
docker compose ps
```

## Variáveis principais

- `JWT_KEY`: obrigatória em produção
- `APP_URL`, `APP_URL_1`, `APP_URL_2`: origens liberadas no CORS
- `DATABASE_PROVIDER` e `DATABASE_CONNECTION_STRING`: banco usado pela API
- `MEDIA_STORAGE_*`: armazenamento local ou S3
- `IMEI_LOOKUP_*`: integração de consulta por IMEI
- `VITE_*`: valores embutidos no build do front
