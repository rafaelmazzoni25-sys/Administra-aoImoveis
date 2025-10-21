# Guia de uso no Visual Studio 2022

Este passo a passo descreve como preparar, abrir e executar a solução **Ferramenta Integrada de Gestão Imobiliária** no Visual Studio 2022 em um ambiente Windows totalmente offline.

## 1. Pré-requisitos

1. **Visual Studio 2022** (Community, Professional ou Enterprise) com os workloads:
   - *ASP.NET and web development* (garante o .NET 8 SDK e ferramentas web).
   - *Data storage and processing* (instala o SQL Server Express LocalDB). Caso já tenha o LocalDB instalado, este workload é opcional.
2. **SQL Server Express LocalDB** – pode ser instalado junto com o Visual Studio (via workload) ou manualmente pelo instalador do SQL Express.
3. **Node.js LTS** (opcional). Necessário apenas se desejar recompilar o CSS com Tailwind (`npm run build:css`).
4. Espaço em disco reservado para os anexos locais. O diretório padrão configurado é `D:\Imobiliaria\Arquivos`, mas pode ser alterado no `appsettings.json`.

## 2. Clonar o repositório

```powershell
cd C:\Projetos
git clone <URL-do-repositorio> AdministraAoImoveis
```

> Ajuste o caminho conforme a pasta onde guarda seus projetos.

## 3. Abrir a solução no Visual Studio 2022

1. Inicie o **Visual Studio 2022**.
2. Escolha **Open a project or solution** e selecione `AdministraAoImoveis\AdministraAoImoveis.sln`.
3. Aguarde o carregamento dos projetos e a restauração automática dos pacotes NuGet.

## 4. Configurar o armazenamento de arquivos

1. Crie a pasta que receberá os anexos. Exemplo:
   ```powershell
   New-Item -ItemType Directory -Path "D:\\Imobiliaria\\Arquivos" -Force
   ```
2. Caso deseje usar outro caminho, edite `src/AdministraAoImoveis.Web/appsettings.json` (ou crie um `appsettings.Development.json` com a mesma seção) e ajuste a chave `FileStorage:BasePath` para a pasta desejada.

## 5. Garantir a conexão com o LocalDB

1. Confirme que o SQL Server LocalDB está instalado executando no **Developer PowerShell para VS 2022**:
   ```powershell
   sqllocaldb info
   ```
   O nome padrão `MSSQLLocalDB` deve aparecer na lista.
2. Não é necessário criar o banco manualmente. Ao executar o projeto pela primeira vez, o `ApplicationDbContext` chama `EnsureCreated` e inicializa o usuário administrador.

## 6. Compilar a solução

No Visual Studio:

1. Menu **Build ▸ Build Solution** (atalho `Ctrl+Shift+B`).
2. Verifique se a compilação conclui sem erros.

## 7. Executar a aplicação

1. Defina o projeto `AdministraAoImoveis.Web` como *Startup Project* (botão direito sobre o projeto ▸ **Set as Startup Project**).
2. Escolha o perfil de execução **https** no seletor de execução do Visual Studio.
3. Pressione `F5` (Debug) ou `Ctrl+F5` (Start Without Debugging).
4. Na primeira execução o sistema irá:
   - Criar o banco `AdministraAoImoveis` no LocalDB.
   - Aplicar as sementes iniciais (perfis de acesso e usuário `admin@local` / senha `Adm1n!234`).
   - Gerar a estrutura básica de pastas de anexos caso não exista.

## 8. Acessar o sistema

1. Abra o navegador padrão iniciado pelo Visual Studio.
2. Acesse com as credenciais semeadas:
   - **Usuário**: `admin@local`
   - **Senha**: `Adm1n!234`
3. Após o primeiro login, altere a senha em **Configurações ▸ Perfil** para atender às políticas internas.

## 9. Recompilar o CSS (opcional)

Se você precisar atualizar o CSS gerado pelo Tailwind:

1. Abra um terminal na pasta `src/AdministraAoImoveis.Web`.
2. Execute:
   ```powershell
   npm install
   npm run build:css
   ```
3. O arquivo resultante é salvo em `wwwroot/css/site.css`.

## 10. Rotina de backup local (opcional)

O diretório `scripts` contém `backup-example.ps1`, que demonstra como copiar o banco LocalDB e a pasta de anexos.

1. Copie o script para um local seguro (ex.: `C:\Scripts\backup-imobiliaria.ps1`).
2. Ajuste as variáveis do início do arquivo (`$DatabaseName`, `$AttachmentPath`, `$BackupRoot`).
3. Agende a execução diária via **Task Scheduler** para manter cópias de segurança offline.

## 11. Dicas adicionais

- Utilize **Ferramentas ▸ Gerenciador de Pacotes NuGet ▸ Console** para aplicar migrações EF Core quando evoluir o schema (`Add-Migration`, `Update-Database`).
- Os logs de auditoria são gravados em `logs/audit-AAAA-MM-DD.log` dentro da pasta de execução. Certifique-se de incluir essa pasta em seus backups.
- Para testar os portais de Proprietário e Interessado, crie usuários adicionais e associe os perfis corretos via menu de administração.
- Toda a aplicação roda localmente; não configure endpoints externos ou serviços de terceiros.

Seguindo estas instruções, o projeto estará pronto para execução e manutenção diretamente no Visual Studio 2022.
