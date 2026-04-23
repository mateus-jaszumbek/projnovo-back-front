# Base fiscal NF-e/NFC-e

Data: 2026-04-13

## O que ja esta pronto

- `POST /api/documentos-fiscais/nfe/emitir-por-venda/{vendaId}`
- `POST /api/documentos-fiscais/nfce/emitir-por-venda/{vendaId}`
- `GET /api/documentos-fiscais?tipoDocumento=Nfe|Nfce|Nfse&status=Autorizado|Rejeitado|Cancelado`
- `POST /api/documentos-fiscais/{id}/consultar`
- `POST /api/documentos-fiscais/{id}/cancelar`
- `POST /api/regras-fiscais-produtos`
- `GET /api/regras-fiscais-produtos?tipoDocumentoFiscal=Nfe|Nfce&ativo=true`
- `PUT /api/regras-fiscais-produtos/{id}`

NF-e e NFC-e ainda usam provider fake. O objetivo e permitir teste de fluxo, numeracao, itens, XML mock, consulta, cancelamento, eventos e jobs de integracao antes da homologacao real.

## Regras antes de emitir

- A venda precisa estar com status `FECHADA`.
- NF-e exige cliente/destinatario com CPF/CNPJ e UF.
- NFC-e permite consumidor nao identificado.
- Cada item de produto deve ter dados fiscais suficientes quando `ValidarTributacaoCompleta = true`: NCM, CFOP, CST/CSOSN e origem da mercadoria.
- Os dados podem vir da peca ou da regra fiscal de produto.

## Como parametrizar por UF/regime

Use `regras_fiscais_produtos` para cadastrar regras por:

- Tipo de documento: `Nfe` ou `Nfce`
- UF origem
- UF destino
- Regime tributario
- NCM
- CFOP
- CST/CSOSN
- CEST quando aplicavel
- Origem da mercadoria
- Aliquotas de ICMS/PIS/COFINS

Campos vazios funcionam como regra generica. Quanto mais campos preenchidos, mais especifica a regra fica.

## O que falta para emissao real

- Escolher integracao direta SEFAZ/municipio ou provedor fiscal.
- Implementar assinatura digital e transmissao real.
- Guardar certificado e segredos criptografados.
- Homologar por UF/modelo fiscal.
- Implementar inutilizacao, contingencia, carta de correcao quando aplicavel e reprocessamento idempotente.
- Validar as regras tributarias com contador, porque CFOP, CST/CSOSN, CEST, NCM e aliquotas dependem da empresa, UF, operacao e regime.
