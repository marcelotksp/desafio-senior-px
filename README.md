# Desafio Sênior PX — Support Desk

Sistema de suporte interno com dois perfis: **Requester** (abre chamados) e **Agent** (atende chamados).

---

## Pré-requisitos

- Visual Studio 2022 Community (workload ASP.NET e .NET Framework 4.8)
- SQL Server LocalDB (instalado com o VS) ou SQL Server Express
- `nuget.exe` disponível no PATH para restaurar pacotes do projeto de testes

---

## Setup

### 1. Banco de dados

```sql
CREATE DATABASE HubieTest;
```

Execute em sequência:

- `database/01_schema.sql` — cria todas as tabelas
- `database/02_seed.sql` — insere usuários e categorias de teste

Credenciais padrão:

| Perfil    | Login       | Senha    |
| --------- | ----------- | -------- |
| Requester | `requester` | `123456` |
| Agent     | `agent`     | `123456` |

> Se usar SQL Express ou instância nomeada, ajuste a connection string em `src/HubieTest.Web/Web.config → <connectionStrings>`.

### 2. Aplicação

Abra `src/HubieTest.sln` no Visual Studio 2022 e pressione **F5**. O IIS Express sobe automaticamente.

### 3. Testes

No Developer Command Prompt do VS 2022, dentro de `src/`:

```
nuget restore HubieTest.Tests\packages.config -PackagesDirectory packages
```

Em seguida: **Test → Run All Tests** no Visual Studio.

---

## O que foi implementado

### Regras de negócio — ticketBusiness

Fluxo de status: `OPEN → IN_PROGRESS → ANSWERED → CLOSED`, com as transições validadas explicitamente em `validateTransition`:

| De          | Para        | Quem pode          |
| ----------- | ----------- | ------------------ |
| OPEN        | IN_PROGRESS | AGENT (via assign) |
| OPEN        | CLOSED      | AGENT              |
| IN_PROGRESS | ANSWERED    | AGENT              |
| IN_PROGRESS | CLOSED      | AGENT              |
| ANSWERED    | CLOSED      | AGENT ou REQUESTER |
| ANSWERED    | IN_PROGRESS | AGENT              |
| CLOSED      | qualquer    | ninguém (final)    |

Controle de acesso aplicado em cada operação: REQUESTER acessa apenas seus próprios tickets; AGENT acessa a fila global.

### Upload e download de anexos

Upload via `multipart/form-data` com validação de tamanho (máx. 10 MB), whitelist de extensões (jpg, jpeg, png, gif, pdf, doc, docx, xls, xlsx, txt, zip) e `Path.GetFileName` para prevenção de path traversal. Arquivos armazenados em `~/App_Data/uploads/{ticketId}/`. Download autenticado via streaming — token aceito por query string para compatibilidade com links diretos.

### Frontend AngularJS

SPA com `ui-router`, redirecionamento automático por perfil e renovação de token via interceptor HTTP. Modal global de erros e validações substituindo mensagens inline

### Testes unitários

Testes com NUnit 3 + Moq cobrindo `ticketBusiness` e `userBusiness`:

- `open()`: rejeita agent, título vazio, categoria ausente, seta campos corretamente
- `listMyTickets()` / `listQueue()`: verificação de perfil, sanitização de status inválido
- `get()`: not found, ownership check do requester, acesso livre do agent
- `assign()`: perfil, not found, status incorreto, seta agente e status
- `changeStatus()`: todas as 6 transições válidas e 6 inválidas via `[TestCase]`, seta `TICKET_CLOSED_DT`
- `addInteraction()`: mensagem vazia, ticket fechado, ownership, authorship do JWT, trim
- `registerAttachment()`: seta `USER_ID` e `ATTACHMENT_CREATED_DT`
- `auth()`: user not found, senha errada, credenciais corretas com JWT válido verificado via `TryValidate`

---

## O que ficou fora do escopo

- **Testes automatizados de integração e de frontend** — os testes cobrem exclusivamente a camada de negócio via mock.
- **Paginação** — listagens retornam todos os registros sem limite.
- **Notificações em tempo real** — não há WebSocket nem polling; o usuário precisa recarregar para ver novas mensagens.
- **Upload múltiplo** — a UI aceita um arquivo por vez.

---

## Decisões e trade-offs

### Injeção de dependência sem contêiner IoC

Dado que .NET Framework 4.8 sem Web API não tem um pipeline de DI nativo e é uma aplicação para teste, não foi incluido um pacote externo para DI
Em um cenário produtivo o ideal seria a implementação do DI
