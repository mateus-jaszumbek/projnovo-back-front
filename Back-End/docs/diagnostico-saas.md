# Diagnostico do SaaS financeiro para assistencias e lojas de celular

Data da auditoria: 2026-04-13

## Estado atual

O projeto e uma API .NET 10 separada em API, Application, Domain e Infrastructure. Ja existem cadastros de empresa, usuarios, clientes, aparelhos, tecnicos, catalogo de servicos, pecas, ordens de servico, vendas, estoque, caixa, contas a pagar/receber e um modulo fiscal inicial.

O repositorio analisado contem somente back-end. Ainda falta o front-end/site, infraestrutura de producao, pipeline de testes/deploy e homologacao fiscal real.

## Correcoes aplicadas nesta rodada

- Bloqueio de uso da chave JWT padrao em producao e validacao de tamanho minimo da chave.
- Rate limit nos endpoints de autenticacao para reduzir tentativa de brute force.
- `api/empresas` deixou de permitir criacao anonima direta; cadastro publico deve passar pelo fluxo `api/auth/registrar-empresa`.
- Listagem/criacao de empresas ficou restrita a `superadmin`.
- Consulta de empresa agora permite que tenant veja apenas a propria empresa, salvo `superadmin`.
- Usuarios e vinculos usuario-empresa passaram a ser filtrados por tenant para owners comuns.
- Owner comum nao consegue criar usuario `IsSuperAdmin`.
- Excecoes de aplicacao agora retornam status HTTP corretos: 400, 401, 404 e 409.
- `.gitignore` criado para evitar versionar `.vs`, `bin`, `obj`, banco local, arquivos `.user`, `.env` e artefatos compilados.
- Base fake de NF-e e NFC-e por venda, com endpoints especificos, provider mock, consulta/cancelamento roteados por tipo de documento e migration do banco.
- Cadastro de regras fiscais de produto por empresa, tipo de documento, UF, regime tributario e NCM.
- Pecas e itens fiscais passaram a armazenar campos fiscais essenciais para produto: NCM, CEST, CFOP, CST/CSOSN, origem, bases e aliquotas de ICMS/PIS/COFINS.

## Lacunas criticas antes de vender

### Fiscal

- O fluxo atual de NFS-e, NF-e e NFC-e usa provider fake/mock; ainda nao emite nota real.
- Para loja de celular, normalmente ha dois mundos fiscais: servico de manutencao tende a usar NFS-e, enquanto venda de pecas/acessorios tende a exigir NF-e ou NFC-e conforme operacao, UF, municipio e regime tributario.
- Falta provedor fiscal real com assinatura/certificado A1, homologacao, producao, validacao de schema XML, envio, consulta, cancelamento, armazenamento de XML/PDF e trilha de eventos.
- Ja existe base de emissao fiscal fake por venda (`OrigemDocumentoFiscal.Venda`) para produtos; falta trocar o mock por um provedor fiscal real.
- Falta inutilizacao de numeracao, carta de correcao quando aplicavel, contingencia, consulta de status do servico, reprocessamento/idempotencia e fila para comunicacao com SEFAZ/municipio/provedor.
- Ha uma primeira parametrizacao tributaria de produto por regra fiscal, mas ainda falta curadoria fiscal real por contador/provedor e validacoes completas por UF, CFOP, CST/CSOSN, CEST, NCM e regime.

### Financeiro e estoque

- `GerarContaReceberSeNecessarioAsync` ainda esta sem implementacao.
- Finalizacao de venda ainda nao integra automaticamente caixa, contas a receber, parcelas e comprovantes.
- Estoque baixa ao adicionar item na venda; isso funciona no MVP, mas precisa de transacao/idempotencia forte para evitar divergencia em concorrencia.
- Numeracao de OS, venda e fiscal usa maximo/proximo numero simples; em producao precisa controle transacional robusto.
- Falta conciliacao de caixa por forma de pagamento, taxas de cartao, estorno, sangria, suprimento e relatorios.
- Falta relatorio de lucratividade por peca/servico/tecnico, contas vencidas, fluxo de caixa e estoque minimo.

### Ordem de servico

- Status de OS ainda e string livre; precisa virar fluxo controlado com transicoes permitidas.
- Falta orcamento/aprovacao do cliente, termo de entrada, termo de retirada, garantia por item, fotos/anexos e assinatura do cliente.
- Senha do aparelho aparece no modelo como texto de dominio; deve ser evitada, criptografada ou substituida por campo temporario com politica de descarte.

### Seguranca e LGPD

- Falta refresh token com revogacao, politica de senha forte, verificacao de e-mail, recuperacao de senha e opcionalmente 2FA.
- Falta RBAC fino por perfil: owner, gerente, atendente, tecnico, financeiro e estoque devem ter permissoes diferentes por endpoint.
- Falta criptografia real para credenciais fiscais, certificados e segredos; os campos existem com sufixo `Encrypted`, mas nao ha servico de criptografia.
- Falta auditoria de alteracoes sensiveis, logs sem vazamento de CPF/CNPJ/senha/token/XML, backup testado e politica de retencao/exclusao.
- Falta CORS restrito ao front-end oficial, security headers, limites de tamanho de request e monitoramento de erros.

### Produto e operacao SaaS

- Falta front-end/site.
- Falta multi-plano de assinatura, cobranca, bloqueio por inadimplencia, trial, limites de uso e painel do superadmin.
- Falta onboarding fiscal guiado por empresa: dados cadastrais, regime, certificado, ambiente, municipio, serie e numeracao.
- Falta termos de uso, aviso de privacidade, contrato de tratamento de dados e canal para titulares de dados.

## Roadmap recomendado

1. MVP seguro: finalizar RBAC, validacoes, refresh token, CORS, PostgreSQL/SQL Server, seed de superadmin, testes de auth/tenant e front-end basico.
2. Operacao da loja: OS completa, venda com parcelas, caixa integrado, contas a receber/pagar, relatorios e estoque com historico confiavel.
3. Fiscal real: escolher estrategia entre integrar direto com NFS-e/NF-e/NFC-e ou usar provedor fiscal; homologar NFS-e para servicos e NF-e/NFC-e para produtos.
4. SaaS vendavel: planos, cobranca, painel admin, backups, observabilidade, politicas LGPD e deploy com secrets fora do codigo.

## Referencias oficiais acompanhadas

- Portal NFS-e: APIs de producao restrita e producao.
- Portal NF-e SVRS: manuais, schemas e notas tecnicas de NF-e/NFC-e.
- ANPD: guia orientativo de seguranca da informacao para agentes de tratamento de pequeno porte.
