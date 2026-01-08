# Sistema de Patches - Validador de Jornada DP v4.1.x

## Fluxo de Trabalho

### Build Completo (nova versão major/minor)
```batch
cd build
build_full.bat
```
**Resultado**: `releases/Output/ValidadorJornada_Setup_X.X.X.exe`

### Build Patch (correções/melhorias)
1. Alterar versão em `ValidadorJornada.csproj`:
```xml
<Version>4.1.2</Version>
<AssemblyVersion>4.1.0</AssemblyVersion>
```

2. Executar:
```batch
cd build
build_patch.bat
> Versao base: 4.1.0
```

3. **Resultado**: `releases/Output/ValidadorJornada_Patch_X.X.X.exe`

## Estrutura de Releases

```
releases/
├── 4.1.0/              # Build completo (base)
│   ├── ValidadorJornada.exe
│   ├── *.dll
│   └── checksums.sha256
│
├── patch_4.1.1/        # Patch incremental
│   ├── files/
│   │   ├── ValidadorJornada.exe
│   │   └── [arquivos modificados]
│   ├── manifest.json
│   └── checksums.sha256
│
├── patch_4.1.2/        # Outro patch
│   ├── files/
│   ├── manifest.json
│   └── checksums.sha256
│
└── Output/             # Instaladores finais
    ├── ValidadorJornada_Setup_4.1.2.exe     # Completo
    └── ValidadorJornada_Patch_4.1.2.exe     # Patch
```

## Manifest JSON

### Estrutura Completa
```json
{
  "version": "4.1.2",
  "baseVersion": "4.1.0",
  "releaseDate": "2025-10-29T12:00:00",
  "files": [
    {
      "path": "ValidadorJornada.exe",
      "size": 245760,
      "hash": "abc123...",
      "action": "update",
      "reason": "Correção formatação horários"
    },
    {
      "path": "HorarioFormatter.cs",
      "size": 3584,
      "hash": "def456...",
      "action": "update",
      "reason": "Fix: retorna vazio ao invés de truncar"
    }
  ],
  "newDependencies": [
    {
      "name": "QuestPDF.dll",
      "version": "2024.3.0",
      "hash": "ghi789..."
    }
  ],
  "removedFiles": [
    "ObsoleteHelper.cs"
  ],
  "changelog": [
    "✅ Correção formatação horários >4 dígitos",
    "✅ Validação nome whitespace",
    "✅ Normalização centralizada"
  ]
}
```

### Ações Disponíveis
- `update`: Atualizar arquivo existente
- `add`: Adicionar novo arquivo
- `remove`: Remover arquivo
- `backup`: Criar backup antes de atualizar

## Scripts PowerShell

### get_version.ps1
Extrai versão do `.csproj`:
```powershell
$proj = Join-Path $PSScriptRoot "..\src\ValidadorJornada\ValidadorJornada.csproj"
[xml]$xml = Get-Content $proj
$version = $xml.Project.PropertyGroup.Version
Write-Output $version
```

### create_patch.ps1
Gera patch incremental comparando versões:
```powershell
.\create_patch.ps1 -BaseVersion "4.1.0" -NewVersion "4.1.2"
```

**Parâmetros**:
- `-BaseVersion`: Versão base para comparação
- `-NewVersion`: Nova versão (lida do .csproj)
- `-OutputPath`: Caminho saída (padrão: `releases/patch_{version}`)

**Processo**:
1. Valida existência versão base
2. Compara arquivos via SHA256
3. Copia apenas arquivos modificados
4. Gera manifest.json
5. Cria checksums.sha256

### sign.ps1
Assina executável com certificado:
```powershell
.\sign.ps1 -FilePath "ValidadorJornada.exe"
```

**Certificado**: Self-signed ou code signing

## Rollback Manual

### PowerShell
```powershell
.\tools\RollbackHelper.ps1 -Version "4.1.0"
```

### Batch
```batch
cd tools
RollbackHelper.bat
> Digite a versão base: 4.1.0
```

**Processo**:
1. Lista backups disponíveis
2. Valida integridade (SHA256)
3. Para aplicação se rodando
4. Restaura arquivos
5. Reinicia aplicação

## Instaladores Inno Setup

### installer_full.iss (Build Completo)
```inno
#define MyAppVersion "4.1.2"
#define MyAppName "Validador de Jornada DP"
#define MyAppExeName "ValidadorJornada.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-G7H8-I9J0-K1L2M3N4O5P6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={pf}\ValidadorJornada
OutputBaseFilename=ValidadorJornada_Setup_{#MyAppVersion}

[Files]
Source: "..\releases\{#MyAppVersion}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
```

### installer_patch.iss (Patch)
```inno
[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={pf}\ValidadorJornada
OutputBaseFilename=ValidadorJornada_Patch_{#MyAppVersion}

[Code]
function InitializeSetup(): Boolean;
begin
  // Valida versão base instalada
  Result := CheckBaseVersion('4.1.0');
end;
```

## Processo Automático

### 1. Build Completo
```batch
build_full.bat
```
**Executa**:
1. Limpa `bin/` e `obj/`
2. `dotnet restore`
3. `dotnet publish -c Release -r win-x64`
4. Copia para `releases/{version}/`
5. Gera checksums SHA256
6. Compila instalador Inno Setup
7. Saída: `releases/Output/ValidadorJornada_Setup_{version}.exe`

### 2. Build Patch
```batch
build_patch.bat
```
**Solicita**:
- Versão base (ex: 4.1.0)

**Executa**:
1. Valida versão base existe
2. Build completo
3. `create_patch.ps1` (compara SHA256)
4. Gera `manifest.json`
5. Compila patch installer
6. Saída: `releases/Output/ValidadorJornada_Patch_{version}.exe`

## Validações e Garantias

### SHA256 Checksums
```
releases/4.1.2/checksums.sha256
```
Conteúdo:
```
abc123... ValidadorJornada.exe
def456... Newtonsoft.Json.dll
ghi789... ExcelDataReader.dll
```

### Smoke Test Pós-Instalação
Executado automaticamente após patch:
1. Verifica arquivos essenciais
2. Testa abertura aplicação
3. Valida checksums
4. Confirma versão instalada

### Rollback Automático
Disparado se:
- Checksum inválido
- Arquivo crítico faltando
- Aplicação não inicia
- Timeout (30s) sem resposta

**Ações**:
1. Log erro detalhado
2. Restaura backup automático
3. Notifica usuário
4. Reverte para versão base

## Logs e Diagnóstico

### build.log
```
releases/patch_4.1.2/build.log
```
Conteúdo:
```
2025-10-29 12:00:00 - Iniciando build patch 4.1.2
2025-10-29 12:00:05 - Versão base: 4.1.0 encontrada
2025-10-29 12:00:10 - Comparando arquivos...
2025-10-29 12:00:15 - Arquivos modificados: 3
2025-10-29 12:00:20 - Manifest gerado
2025-10-29 12:00:25 - Checksums calculados
2025-10-29 12:00:30 - Build concluído com sucesso
```

### patch_install.log
```
%AppData%\ValidadorJornada\patch_install.log
```
Conteúdo:
```
2025-10-29 14:30:00 - Instalando patch 4.1.2
2025-10-29 14:30:05 - Backup criado: 4.1.0_backup
2025-10-29 14:30:10 - Validando checksums... OK
2025-10-29 14:30:15 - Aplicando 3 arquivos...
2025-10-29 14:30:20 - Smoke test... OK
2025-10-29 14:30:25 - Patch instalado com sucesso
```

## Boas Práticas

### Versionamento
- **Major** (X.0.0): Mudanças quebram compatibilidade
- **Minor** (X.Y.0): Novas funcionalidades retrocompatíveis
- **Patch** (X.Y.Z): Correções bugs

### Changelog
Sempre incluir no manifest:
```json
"changelog": [
  "✅ Fix: Formatação horários >4 dígitos retorna vazio",
  "✅ New: Validação em lote Excel",
  "✅ Improve: Performance normalização 30%"
]
```

### Testes Pré-Release
1. Build completo em máquina limpa
2. Instalação fresh
3. Upgrade de versão anterior
4. Aplicação patch
5. Rollback
6. Teste funcionalidades críticas

### Distribuição
- Build completo: FTP/drive compartilhado
- Patch: Auto-update interno (futuro)
- Documentação: Wiki interna
- Release notes: Email DP

## Troubleshooting

### Erro: "Versão base não encontrada"
```
✅ Solução: Executar build_full.bat primeiro
```

### Erro: "Nenhuma diferença detectada"
```
✅ Solução: Normal se nenhum arquivo alterado
```

### Erro: "Checksum inválido"
```
✅ Solução: Recompilar build completo
```

### Erro: "Instalador falha na validação"
```
✅ Solução: Verificar versão base instalada
```

### Rollback falha
```
✅ Solução: Reinstalar versão base completa
```

## Comandos Rápidos

```batch
# Build completo
cd build && build_full.bat

# Patch de 4.1.0 para 4.1.2
cd build && build_patch.bat

# Verificar checksums
certutil -hashfile ValidadorJornada.exe SHA256

# Rollback para 4.1.0
.\tools\RollbackHelper.ps1 -Version "4.1.0"

# Listar patches disponíveis
dir releases\patch_*

# Extrair versão do .csproj
powershell -File build\get_version.ps1
```

---

**Atualizado**: 29/10/2025  
**Versão Sistema**: 4.1.x  
**Responsável**: Samuel Fernandes - DP
