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

## Variaveis principais

- `JWT_KEY`: obrigatoria em producao
- `APP_URL`, `APP_URL_1`, `APP_URL_2`: origens liberadas no CORS
- `DATABASE_PROVIDER`, `DATABASE_CONNECTION_STRING`, `ConnectionStrings__DefaultConnection`, `DATABASE_URL` e `PG*`: banco usado pela API
- `MEDIA_STORAGE_*`: armazenamento local de arquivos
- `IMEI_LOOKUP_*`: integracao de consulta por IMEI
- `FRONTEND_BASE_URL`: URL do front, usada para montar o link de redefinicao de senha no e-mail
- `SMTP_*`: credenciais de envio de e-mail (recuperacao de senha). Com `SMTP_ENABLED=false` (padrao), nenhum e-mail e enviado, apenas logado
- `VITE_*`: valores embutidos no build do front

## Deploy no Render

O backend roda no Render a partir do `render.yaml` na raiz do repositorio (Blueprint), usando o `Dockerfile` deste diretorio.

1. No Render, crie o servico via Blueprint apontando para este repositorio (ele le o `render.yaml` automaticamente).
2. `Jwt__Key` e gerado automaticamente pelo Render (`generateValue: true` no blueprint).
3. Variaveis marcadas com `sync: false` no `render.yaml` precisam ser preenchidas manualmente no painel do Render:
   - `ConnectionStrings__DefaultConnection`: connection string do banco Postgres do Supabase (ver secao "Banco de dados no Supabase")
   - `Security__AllowedCorsOrigins__0`: URL do front publicado no Vercel
   - `Frontend__BaseUrl`: mesma URL do front, usada para montar o link de redefinicao de senha no e-mail
   - `Smtp__Host`, `Smtp__User`, `Smtp__Password`, `Smtp__FromEmail`: credenciais do provedor de e-mail (recuperacao de senha). Depois de configurar, mude `Smtp__Enabled` para `"true"` (vem `"false"` por padrao, o que so loga o e-mail em vez de enviar)
4. Se for usar webhook da Focus, ajuste `FocusWebhook__PublicBaseUrl` com a URL publica do backend no Render.

O backend expoe `GET /healthz`, aplica migrations no startup e detecta PostgreSQL automaticamente pela connection string.

## Banco de dados no Supabase

O projeto usa o Postgres do Supabase como banco de producao (nao o Postgres do proprio Render).

1. Crie um projeto no Supabase e copie a connection string em Settings > Database.
2. Prefira a connection string via connection pooler (porta 6543), mais adequada para planos com poucas conexoes simultaneas como o Render free.
3. Configure no Render:
   - `Database__Provider=Postgres`
   - `ConnectionStrings__DefaultConnection` com a connection string do Supabase no formato `postgresql://usuario:senha@host:porta/postgres` (a API aceita a URL diretamente e converte internamente)
4. Garanta que a connection string exige SSL (`?sslmode=require`), como o Supabase requer.

## Deploy do front no Vercel

No diretorio `Front-End`:

1. Configure as variaveis do arquivo `.env.vercel.example`.
2. Defina `VITE_API_URL` para a URL da API publicada no Render com `/api` no final.
3. O arquivo `vercel.json` ja adiciona o rewrite de SPA para rotas como `/kanban`, `/empresa` e `/suporte`.

## Observacao sobre arquivos no Render

O projeto usa apenas armazenamento local de arquivos.

No Render, isso funciona para subir a aplicacao, mas o filesystem do container nao e duravel. Em uploads de producao, arquivos podem ser perdidos em redeploys, reinicios ou troca de instancia.

Se voce precisar de persistencia real para logos, anexos ou fotos, sera necessario plugar outro armazenamento externo compativel no futuro.
