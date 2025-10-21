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
2. Aplicação do banco (via migrações EF Core):
   ```bash
   dotnet ef database update --project src/AdministraAoImoveis.Web
   ```
   Alternativamente, execute `dotnet run --project src/AdministraAoImoveis.Web` para aplicar as migrações automaticamente na inicialização. O sistema gera o usuário `admin@local` (senha `Adm1n!234`).
3. (Opcional) Gerar CSS via Tailwind:
   ```bash
   cd src/AdministraAoImoveis.Web
   npm install
   npm run build:css
   ```

## Configuração

- `appsettings.json` define a connection string e o diretório local para anexos.
- Ajuste `FileStorage:BasePath` para a pasta desejada. Valores relativos são resolvidos a partir do `ContentRootPath` da aplicação e podem ser configurados também via variável de ambiente (`FileStorage__BasePath`).
- Logs de auditoria são gravados em `logs/audit-AAAAMMDD.log` dentro do diretório da aplicação.

## Próximos Passos

- Publicar migrações EF Core para versionar o schema do banco de dados.
- Refinar políticas de autorização em nível de ação e aplicar testes automatizados de autorização.
- Automatizar rotinas de integração contínua (build, lint e testes) para garantir a qualidade do código.

O projeto foi estruturado para operar 100% offline e pode ser estendido conforme as regras de negócio detalhadas no documento original.
