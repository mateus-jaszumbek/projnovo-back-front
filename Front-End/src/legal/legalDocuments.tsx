import type { ReactNode } from "react";
import { LEGAL_CONFIG, LEGAL_DOCUMENT_VERSIONS, legalVersionLabel } from "./legalConfig";

export type LegalDocumentKey = "terms" | "privacy" | "cookies";

type LegalSection = {
  title: string;
  content: ReactNode;
};

type LegalDocumentDefinition = {
  key: LegalDocumentKey;
  path: string;
  title: string;
  summary: string;
  version: string;
  sections: LegalSection[];
};

const termsDocument: LegalDocumentDefinition = {
  key: "terms",
  path: "/termos-de-uso",
  title: "Termos de Uso",
  summary:
    "Regras para uso da plataforma, cadastro de empresas, segurança de acesso e responsabilidades sobre os dados operacionais lançados no sistema.",
  version: LEGAL_DOCUMENT_VERSIONS.terms,
  sections: [
    {
      title: "1. Sobre a plataforma",
      content: (
        <p>
          O {LEGAL_CONFIG.appName} é uma plataforma web para gestão de assistências técnicas,
          ordens de serviço, cadastros, estoque, vendas, financeiro, fiscal e rotinas
          relacionadas. Ao criar uma conta, a empresa usuária passa a utilizar os recursos
          disponibilizados conforme o plano, as permissões internas e as regras descritas
          neste documento.
        </p>
      ),
    },
    {
      title: "2. Quem pode contratar e criar conta",
      content: (
        <p>
          O cadastro deve ser realizado por pessoa com poderes para representar a empresa ou
          por responsável autorizado internamente. A conta inicial criada no fluxo de cadastro
          assume perfil proprietário da empresa e responde pela administração de usuários,
          permissões e configurações do ambiente corporativo.
        </p>
      ),
    },
    {
      title: "3. Responsabilidades da empresa usuária",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>manter dados cadastrais corretos, completos e atualizados;</li>
          <li>proteger senhas, acessos e perfis concedidos aos colaboradores;</li>
          <li>
            utilizar a plataforma apenas para atividades lícitas e relacionadas ao negócio;
          </li>
          <li>
            garantir que os dados de clientes, aparelhos, ordens de serviço, vendas, notas e
            demais registros lançados possuam base legal adequada;
          </li>
          <li>
            respeitar direitos de terceiros, legislação aplicável e obrigações fiscais,
            consumeristas, trabalhistas e de proteção de dados.
          </li>
        </ul>
      ),
    },
    {
      title: "4. Dados operacionais inseridos pela empresa",
      content: (
        <p>
          Em relação aos dados que a empresa cadastra no sistema, como clientes, aparelhos,
          ordens de serviço, documentos fiscais e mensagens enviadas ao público final, a
          empresa usuária atua como controladora desses dados. O {LEGAL_CONFIG.appName} atua
          como fornecedor da infraestrutura e dos recursos tecnológicos necessários para o
          tratamento, nos limites da contratação e da legislação aplicável.
        </p>
      ),
    },
    {
      title: "5. Disponibilidade, integrações e mudanças",
      content: (
        <p>
          A plataforma pode receber evoluções, ajustes visuais, correções, novas integrações
          e mudanças operacionais para melhorar segurança, desempenho e experiência de uso.
          Podemos ainda suspender temporariamente partes do serviço para manutenção, correção
          de incidentes ou atendimento de exigências legais e técnicas.
        </p>
      ),
    },
    {
      title: "6. Condutas proibidas",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>tentar violar autenticação, limites de acesso ou segurança da aplicação;</li>
          <li>usar a plataforma para fraude, engenharia reversa ou automação abusiva;</li>
          <li>inserir conteúdo ilícito, ofensivo ou que infrinja direitos de terceiros;</li>
          <li>
            compartilhar credenciais de forma insegura ou permitir acesso não autorizado ao
            ambiente da empresa.
          </li>
        </ul>
      ),
    },
    {
      title: "7. Suspensão e encerramento",
      content: (
        <p>
          O acesso pode ser suspenso ou encerrado em caso de uso indevido, indícios de fraude,
          violação destes termos, determinação legal ou risco relevante à segurança da
          plataforma, dos dados ou de terceiros. A empresa também pode solicitar encerramento
          da conta, observados prazos legais e obrigações de guarda.
        </p>
      ),
    },
    {
      title: "8. Propriedade intelectual",
      content: (
        <p>
          O software, interfaces, fluxos, marcas, componentes visuais, códigos e materiais do
          {LEGAL_CONFIG.appName} permanecem protegidos pela legislação aplicável. O uso da
          plataforma não transfere titularidade intelectual à empresa usuária, apenas licença
          de uso nos limites contratados.
        </p>
      ),
    },
    {
      title: "9. Privacidade, LGPD e contato",
      content: (
        <p>
          O tratamento de dados pessoais vinculado à criação da conta e ao uso da plataforma
          segue a Política de Privacidade e LGPD deste site. Para dúvidas sobre estes Termos de
          Uso, entre em contato por {LEGAL_CONFIG.supportEmail}. A legislação aplicável é a
          brasileira, com observância da Lei Geral de Proteção de Dados Pessoais (Lei no
          13.709/2018).
        </p>
      ),
    },
  ],
};

const privacyDocument: LegalDocumentDefinition = {
  key: "privacy",
  path: "/privacidade-lgpd",
  title: "Política de Privacidade e LGPD",
  summary:
    "Como coletamos, usamos, armazenamos e protegemos dados da conta, da empresa e das rotinas operacionais registradas no sistema.",
  version: LEGAL_DOCUMENT_VERSIONS.privacy,
  sections: [
    {
      title: "1. Quem trata os dados desta plataforma",
      content: (
        <p>
          Para os dados relacionados ao cadastro e à administração da conta do site,
          {` ${LEGAL_CONFIG.controllerName}`} atua como controladora. Para os dados de clientes,
          aparelhos, ordens de serviço, vendas e demais registros que cada empresa insere no
          sistema, a própria empresa usuária atua como controladora, enquanto a plataforma atua
          como operadora/fornecedora de infraestrutura, nos termos da LGPD.
        </p>
      ),
    },
    {
      title: "2. Quais dados podem ser tratados",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>
            dados de cadastro da empresa e do usuário responsável, como nome, e-mail, telefone
            e CNPJ;
          </li>
          <li>
            dados operacionais lançados pela empresa, como clientes, aparelhos, ordens de
            serviço, peças, vendas, notas fiscais, mensagens e anexos;
          </li>
          <li>
            dados de autenticação, logs técnicos, data e hora de acesso, IP aproximado,
            navegador, erros e registros de segurança;
          </li>
          <li>
            preferências de cookies, consentimentos exibidos no site e demais informações
            necessárias para operação e segurança da aplicação.
          </li>
        </ul>
      ),
    },
    {
      title: "3. Finalidades e bases legais",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>execução do contrato e disponibilização da plataforma contratada;</li>
          <li>criação, autenticação e administração de contas de acesso;</li>
          <li>suporte técnico, prevenção a fraudes e proteção do ambiente;</li>
          <li>
            cumprimento de obrigações legais, regulatórias, fiscais, contábeis e de guarda;
          </li>
          <li>
            exercício regular de direitos em processo judicial, administrativo ou arbitral;
          </li>
          <li>
            consentimento, quando a funcionalidade depender de autorização específica, como
            categorias opcionais de cookies e recursos equivalentes.
          </li>
        </ul>
      ),
    },
    {
      title: "4. Compartilhamento de dados",
      content: (
        <p>
          Os dados podem ser compartilhados com provedores de hospedagem, banco de dados,
          armazenamento de arquivos, serviços de autenticação, envio de e-mails, integrações
          fiscais e outros operadores indispensáveis ao funcionamento do sistema. Também pode
          haver compartilhamento com autoridades públicas e órgãos competentes quando houver
          obrigação legal, regulatória ou requisição válida.
        </p>
      ),
    },
    {
      title: "5. Transferência internacional",
      content: (
        <p>
          Parte da infraestrutura tecnológica pode utilizar provedores com servidores no Brasil
          ou no exterior. Quando isso ocorrer, o tratamento seguirá medidas contratuais,
          organizacionais e técnicas compatíveis com a LGPD e com o nível de risco envolvido.
        </p>
      ),
    },
    {
      title: "6. Segurança da informação",
      content: (
        <p>
          Adotamos medidas de segurança compatíveis com o contexto do serviço, incluindo
          autenticação, segregação lógica por empresa, controle de permissões, limitação de
          acesso, registro de eventos, rotinas de atualização, proteção de infraestrutura e
          mecanismos para reduzir riscos de acesso indevido, destruição, alteração ou
          divulgação não autorizada.
        </p>
      ),
    },
    {
      title: "7. Retenção e descarte",
      content: (
        <p>
          Os dados são mantidos pelo tempo necessário para cumprir as finalidades do serviço, o
          período contratual, obrigações legais, prazos prescricionais e exigências de defesa
          de direitos. Encerrada a necessidade, o descarte ou anonimização observará os prazos
          legais, limitações técnicas e regras aplicáveis ao ambiente contratado.
        </p>
      ),
    },
    {
      title: "8. Direitos do titular",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>confirmação da existência de tratamento;</li>
          <li>acesso, correção e atualização de dados;</li>
          <li>anonimização, bloqueio ou eliminação quando cabível;</li>
          <li>portabilidade e informação sobre compartilhamentos;</li>
          <li>revogação de consentimento, quando ele for a base aplicável;</li>
          <li>peticionamento perante a ANPD, nos termos legais.</li>
        </ul>
      ),
    },
    {
      title: "9. Como exercer direitos e contato",
      content: (
        <p>
          Solicitações relacionadas a dados da conta do site podem ser enviadas para
          {` ${LEGAL_CONFIG.privacyEmail}`}. Para dados lançados pela empresa usuária no sistema,
          o titular deverá preferencialmente contatar a própria empresa responsável pelo
          atendimento, já que ela é a controladora direta desses registros. Endereço informado
          para referência: {LEGAL_CONFIG.address}. Documento do controlador:{" "}
          {LEGAL_CONFIG.controllerDocument}.
        </p>
      ),
    },
  ],
};

const cookiesDocument: LegalDocumentDefinition = {
  key: "cookies",
  path: "/politica-de-cookies",
  title: "Política de Cookies",
  summary:
    "Explica como o site usa cookies e tecnologias similares para manter preferências, segurança e eventual medição de uso.",
  version: LEGAL_DOCUMENT_VERSIONS.cookies,
  sections: [
    {
      title: "1. O que são cookies e tecnologias similares",
      content: (
        <p>
          Cookies são pequenos arquivos salvos no navegador para lembrar preferências e apoiar
          o funcionamento do site. Além deles, o {LEGAL_CONFIG.appName} também pode utilizar
          tecnologias similares, como armazenamento local do navegador, para manter sessão,
          preferências e estabilidade do serviço.
        </p>
      ),
    },
    {
      title: "2. O que usamos hoje neste site",
      content: (
        <ul className="list-disc space-y-2 pl-5">
          <li>
            cookie essencial de consentimento para registrar a sua escolha sobre cookies;
          </li>
          <li>
            armazenamento local do navegador para manter a sessão autenticada e preferências
            técnicas do uso da plataforma;
          </li>
          <li>
            categorias opcionais de medição e melhoria somente se forem ativadas futuramente e
            aceitas por você no banner de cookies.
          </li>
        </ul>
      ),
    },
    {
      title: "3. Categorias de cookies",
      content: (
        <div className="space-y-4">
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <h3 className="text-sm font-semibold text-slate-900">Essenciais</h3>
            <p className="mt-1 text-sm text-slate-600">
              Necessários para segurança, login, estabilidade, balanceamento da experiência e
              registro da sua preferência de cookies. Não podem ser desligados no painel.
            </p>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <h3 className="text-sm font-semibold text-slate-900">Medição e melhoria</h3>
            <p className="mt-1 text-sm text-slate-600">
              Categoria opcional para estatísticas de navegação e melhoria do produto. Só deve
              ser ativada após o seu consentimento, quando houver ferramenta compatível
              instalada na plataforma.
            </p>
          </div>
        </div>
      ),
    },
    {
      title: "4. Como você pode gerenciar",
      content: (
        <p>
          Você pode aceitar, recusar categorias opcionais ou revisar sua preferência a qualquer
          momento pelo botão de cookies exibido no site. Também é possível gerenciar cookies no
          navegador, lembrando que o bloqueio de itens essenciais pode afetar login, segurança
          e funcionamento de partes da plataforma.
        </p>
      ),
    },
    {
      title: "5. Prazo de armazenamento",
      content: (
        <p>
          O registro da sua preferência de cookies é mantido por período limitado e pode ser
          solicitado novamente quando houver atualização relevante desta política, mudança
          técnica significativa ou expiração do prazo configurado.
        </p>
      ),
    },
    {
      title: "6. Contato",
      content: (
        <p>
          Dúvidas sobre esta política podem ser encaminhadas para {LEGAL_CONFIG.privacyEmail} ou
          para o canal geral {LEGAL_CONFIG.supportEmail}.
        </p>
      ),
    },
  ],
};

export const LEGAL_DOCUMENTS: Record<LegalDocumentKey, LegalDocumentDefinition> = {
  terms: termsDocument,
  privacy: privacyDocument,
  cookies: cookiesDocument,
};

export function getLegalDocument(key: LegalDocumentKey) {
  return LEGAL_DOCUMENTS[key];
}

export function documentVersionBadge(version: string) {
  return `Versão ${legalVersionLabel(version)}`;
}
