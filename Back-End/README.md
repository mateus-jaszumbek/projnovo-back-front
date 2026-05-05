# Back-End

Este diretorio e a raiz de deploy da stack Docker do projeto.

## Estrutura

- `docker-compose.yml`: sobe API e front a partir deste diretorio
- `Dockerfile`: build da API ASP.NET Core
- `../Front-End/Dockerfile`: build do front Vite/Nginx

## Subir localmente com Docker

1. Copie o arquivo de ambiente:

```bash
cp .env.example .env
```

2. Ajuste os valores necessarios no `.env`.

3. Suba a stack:

```bash
docker compose up -d --build
docker compose ps
```

O front ficara disponivel na porta definida por `HTTP_PORT` no `.env`, por padrao `8081`.

## Deploy via Git na EC2

Depois do primeiro clone no servidor:

```bash
cd ~/servicosapp-repo
git pull origin main
cd ~/servicosapp-repo/Back-End
docker compose up -d --build
docker compose ps
```

## Variaveis principais

- `JWT_KEY`: obrigatoria em producao
- `APP_URL`, `APP_URL_1`, `APP_URL_2`: origens liberadas no CORS
- `DATABASE_PROVIDER` e `DATABASE_CONNECTION_STRING`: banco usado pela API
- `MEDIA_STORAGE_*`: armazenamento local de arquivos
- `IMEI_LOOKUP_*`: integracao de consulta por IMEI
- `VITE_*`: valores embutidos no build do front

## Deploy no Railway

Use este diretorio `Back-End` como raiz do servico no Railway.

1. Configure o servico com `Dockerfile`.
2. Cadastre as variaveis do arquivo `.env.railway.example`.
3. Para PostgreSQL do Railway, use:
   - `Database__Provider=PostgreSql`
   - `ConnectionStrings__DefaultConnection=Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`
4. Configure `Security__AllowedCorsOriginsCsv` com a URL do front no Vercel.
5. Se for usar webhook da Focus, ajuste `FocusWebhook__PublicBaseUrl` com a URL publica do backend no Railway.

O backend expoe `GET /healthz`, aplica migrations no startup e detecta PostgreSQL automaticamente pela connection string.

## Deploy do front no Vercel

No diretorio `Front-End`:

1. Configure as variaveis do arquivo `.env.vercel.example`.
2. Defina `VITE_API_URL` para a URL da API publicada no Railway com `/api` no final.
3. O arquivo `vercel.json` ja adiciona o rewrite de SPA para rotas como `/kanban`, `/empresa` e `/suporte`.

## Observacao sobre arquivos no Railway

O projeto usa apenas armazenamento local de arquivos.

No Railway, isso funciona para subir a aplicacao, mas o filesystem do container nao e duravel. Em uploads de producao, arquivos podem ser perdidos em redeploys, reinicios ou troca de instancia.

Se voce precisar de persistencia real para logos, anexos ou fotos, sera necessario plugar outro armazenamento externo compativel no futuro.
