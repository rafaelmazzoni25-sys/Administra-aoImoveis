# Ferramenta Integrada de Gestão Imobiliária

Esta solução contém a base do sistema web local solicitado, construída com ASP.NET Core 8, MVC e Entity Framework Core utilizando SQL Server LocalDB. Todos os módulos descritos no documento original possuem entidades, serviços e telas iniciais para operação offline em rede local.

## Estrutura do Projeto

- `AdministraAoImoveis.sln` – solução principal com a aplicação web.
- `src/AdministraAoImoveis.Web` – projeto ASP.NET Core MVC contendo:
  - **Domain**: entidades e enums representando imóveis, negociações, vistorias, atividades, agenda, portal interno, auditoria e arquivos locais.
  - **Data**: `ApplicationDbContext` e `ApplicationDbInitializer` com configuração EF Core + Identity.
  - **Infrastructure**: serviços de armazenamento de arquivos locais e auditoria com logs em disco.
  - **Controllers/Views**: telas MVC cobrindo dashboard, imóveis, negociações (kanban), vistorias, pendências, agenda, relatórios e portais internos.
  - **wwwroot**: assets estáticos preparados para TailwindCSS (bundle local via `npm run build:css`).

## Requisitos

- .NET SDK 8.0
- SQL Server Express LocalDB (instalado no host Windows)
- Node.js (opcional, apenas para recompilar o CSS com Tailwind)

## Preparação do Ambiente

1. Restaurar pacotes e compilar:
   ```bash
   dotnet restore
   dotnet build
   ```
2. Aplicação de banco (usando criação automática):
   ```bash
   dotnet run --project src/AdministraAoImoveis.Web
   ```
   O sistema cria o banco (EnsureCreated) e gera usuário `admin@local` (senha `Adm1n!234`).
3. (Opcional) Gerar CSS via Tailwind:
   ```bash
   cd src/AdministraAoImoveis.Web
   npm install
   npm run build:css
   ```

## Configuração

- `appsettings.json` define a connection string e o diretório local para anexos.
- Ajuste `FileStorage:BasePath` para a pasta desejada (ex.: `D:\Imobiliaria\Arquivos`).
- Logs de auditoria são gravados em `logs/audit-AAAAMMDD.log` dentro do diretório da aplicação.

## Próximos Passos

- Criar migrações EF Core para controlar o schema.
- Implementar ações CRUD completas e regras específicas por módulo.
- Adicionar políticas de autorização refinadas por perfil.
- Expandir notificações in-app e dashboards.

O projeto foi estruturado para operar 100% offline e pode ser estendido conforme as regras de negócio detalhadas no documento original.
