# Validador de Jornada DP

Sistema desktop para validação de jornadas de trabalho conforme CLT, desenvolvido em WPF .NET 8.

## 📋 Visão Geral

Aplicativo Windows que valida horários de trabalho, calcula duração, intervalos, interjornada (11h) e jornadas semanais/mensais. Suporta jornadas de 4h, 5h50, 7h20 e 8h, com modo especial para sábado (44h semanais).

## 🏗️ Arquitetura

```
ValidadorJornada/
├── build/
│   ├── build_full.bat      # Build completo
│   ├── build_patch.bat     # Build incremental
│   ├── create_patch.ps1    # Gerador de patches
│   ├── get_version.ps1     # Extração de versão
│   └── sign.ps1            # Assinatura digital
│
├── src/ValidadorJornada/
│   ├── Views/              # XAML + Code-behind
│   │   ├── MainWindow.xaml
│   │   ├── HistoricoWindow.xaml
│   │   ├── ConfigCodigoWindow.xaml
│   │   ├── ValidacaoLoteWindow.xaml      # [NOVO] Validação em lote
│   │   └── ExportDialog.xaml             # [NOVO] Exportação PDF
│   │
│   ├── ViewModels/         # MVVM Pattern
│   │   ├── MainViewModel.cs
│   │   ├── HistoricoViewModel.cs
│   │   ├── ValidacaoLoteViewModel.cs     # [NOVO]
│   │   ├── ExportViewModel.cs            # [NOVO]
│   │   ├── RelayCommand.cs
│   │   └── AsyncCommand.cs
│   │
│   ├── Core/
│   │   ├── Services/
│   │   │   ├── JornadaValidator.cs
│   │   │   ├── ValidacaoLoteService.cs           # [NOVO] Validação Excel
│   │   │   ├── ExcelValidatorService.cs          # [NOVO] Processamento Excel
│   │   │   ├── ValidacaoLoteJornadaValidator.cs  # [NOVO] Validador batch
│   │   │   ├── ExportService.cs                  # [NOVO] Exportação PDF
│   │   │   ├── CodigoService.cs
│   │   │   ├── ConfigService.cs
│   │   │   ├── HistoricoService.cs
│   │   │   └── SettingsService.cs
│   │   │
│   │   ├── Models/
│   │   │   ├── ValidationResult.cs
│   │   │   ├── Jornada.cs
│   │   │   ├── JornadaConfig.cs
│   │   │   ├── RelatorioValidacaoLote.cs    # [NOVO]
│   │   │   └── LinhaExcelValidacao.cs       # [NOVO]
│   │   │
│   │   └── Helpers/
│   │       ├── TimeHelper.cs
│   │       ├── HorarioFormatter.cs
│   │       ├── HorarioNormalizer.cs         # [NOVO]
│   │       ├── InputValidator.cs
│   │       ├── JsonFileHelper.cs            # [NOVO]
│   │       ├── ExcelHelper.cs
│   │       ├── ExcelLoteHelper.cs           # [NOVO]
│   │       └── PdfHelper.cs                 # [NOVO]
│   │
│   ├── Converters/
│   │   └── Converters.cs
│   │
│   ├── Resources/
│   │   ├── config.json
│   │   └── icon.ico
│   │
│   ├── App.xaml
│   └── ValidadorJornada.csproj
│
└── %AppData%/ValidadorJornada/
    ├── codigos.json
    ├── historico.json
    ├── settings.json
    └── logs/
        ├── export.log              # [NOVO]
        └── export_errors.log       # [NOVO]
```

## ⚙️ Funcionalidades

### 1. Validação Individual
- **Simples (2 horários)**: Jornadas sem intervalo (4h)
- **Com intervalo (4 horários)**: Jornadas 5h50, 7h20, 8h
- **Validações automáticas**:
  - Ordem cronológica
  - Limite diário (10h)
  - Intervalo mínimo/máximo
  - Período máximo entre entrada/saída

### 2. Validação em Lote **[NOVO]**
- **Importação**: Planilhas Excel (.xlsx, .xls)
- **Processamento assíncrono**: Barra de progresso em tempo real
- **Validações configuráveis**:
  - Períodos
  - Jornada completa
  - Intervalos
  - Horários agrupados
- **Colorização automática**: 
  - Verde (válido)
  - Vermelho (erro)
- **Aba de erros**: Geração automática com detalhes
- **Relatório PDF**: Estatísticas e análise detalhada

### 3. Exportação para PDF **[NOVO]**
- **Jornadas individuais**: Relatórios separados por colaborador
- **Campos editáveis**:
  - Matrícula
  - Nome
  - Cargo
  - Data de alteração
- **Modos**:
  - Data única (todas jornadas mesma data)
  - Datas individuais (data específica por jornada)
- **Salvamento**: Área de Trabalho
- **Abertura automática**: Opcional após geração

### 4. Interjornada (11h)
- Valida descanso mínimo entre jornadas
- Suporta virada de dia
- Modo especial sábado: 8h (Seg-Sex) + 4h (Sáb) = 44h semanais

### 5. Sistema de Códigos
- **Importação**: Excel, CSV, JSON
- **Formato Excel**: Coluna A (código) | Coluna B (horários)
- **Formato CSV**: `codigo,horarios` (separadores: `,` `;` `\t`)
- **Associação automática**: Busca código ao validar

### 6. Histórico
- Últimas 100 validações
- Expiração 30 dias
- Listagem completa + últimas 5 na tela principal
- **Exportação múltipla**: Selecionar várias jornadas para PDF

### 7. Configurações
- Auto-formatação de horários (`0800` → `08:00`)
- Toggle interjornada on/off
- Persistência em `%AppData%`

## 🔧 Tecnologias

### Runtime
- **.NET 8.0 Windows**
- **WPF** (Windows Presentation Foundation)
- **MVVM Pattern**

### Dependências NuGet
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="ExcelDataReader" Version="3.6.0" />
<PackageReference Include="ExcelDataReader.DataSet" Version="3.6.0" />
<PackageReference Include="NJsonSchema" Version="11.3.0" />
<PackageReference Include="QuestPDF" Version="2024.3.0" />
<PackageReference Include="EPPlus" Version="7.5.2" />
```

## 🚀 Build

### Requisitos
- .NET 8 SDK
- PowerShell 5.1+
- Inno Setup 6 (opcional)

### Build Completo
```batch
cd build
build_full.bat
```

### Build Patch (Incremental)
```batch
cd build
build_patch.bat
```

**Processo**:
1. Detecta .NET 8 SDK
2. Extrai versão do `.csproj`
3. Restaura dependências
4. Compila (Release, win-x64)
5. Gera checksums SHA256
6. Cria instalador (Inno Setup)

**Saída**:
- `releases/{version}/` - Build completo
- `releases/patch_{version}/` - Patch incremental
- `releases/Output/` - Instaladores

## 📝 Configuração

### config.json
```json
{
  "jornadas": [
    {
      "duracaoMinutos": 240,
      "nome": "Jornada Parcial 04:00",
      "horasSemanais": 24,
      "horasMensais": 120,
      "intervaloMin": 0,
      "intervaloMax": 0,
      "diasValidos": ["util", "sabado"]
    },
    {
      "duracaoMinutos": 350,
      "nome": "Jornada Reduzida 05:50",
      "horasSemanais": 35,
      "horasMensais": 175,
      "intervaloMin": 15,
      "intervaloMax": 120,
      "diasValidos": ["util"]
    },
    {
      "duracaoMinutos": 440,
      "nome": "Jornada de 07:20",
      "horasSemanais": 44,
      "horasMensais": 220,
      "intervaloMin": 60,
      "intervaloMax": 120,
      "diasValidos": ["util", "sabado"]
    },
    {
      "duracaoMinutos": 480,
      "nome": "Jornada de 08:00",
      "horasSemanais": 44,
      "horasMensais": 220,
      "intervaloMin": 60,
      "intervaloMax": 120,
      "diasValidos": ["util"]
    }
  ],
  "periodoMaximoHoras": 10.0
}
```

## 💾 Dados Persistidos

**Localização**: `%AppData%\ValidadorJornada\`

- `codigos.json`: Mapeamento horários → códigos
- `historico.json`: Últimas 100 validações
- `settings.json`: Preferências do usuário
- `logs/export.log`: Logs de exportação
- `logs/export_errors.log`: Erros de exportação

## 🎯 Fluxo de Uso

### Validação Individual
1. Digite horários: `08:00 12:00 13:00 17:00`
2. Auto-formatação (opcional): `0800 1200` → `08:00 12:00`
3. Validação: Enter ou botão ⟳
4. Resultado: ✅/⚠️ com detalhes
5. Código: Exibido se configurado
6. Histórico: Salvo automaticamente

### Validação em Lote
1. Menu **Ferramentas** → **Validação em Lote**
2. Selecionar arquivo Excel
3. Configurar validações (períodos, jornada, intervalos)
4. Clicar **Validar**
5. Aguardar processamento (barra de progresso)
6. Verificar resultados (cores e aba de erros)
7. Opcional: Gerar relatório PDF

### Exportação PDF
1. Selecionar jornadas no histórico (Ctrl+clique múltiplo)
2. Botão **Exportar PDF**
3. Escolher modo (data única/individual)
4. Preencher campos (matrícula, nome, cargo)
5. Clicar **Gerar PDF**
6. Arquivo salvo na Área de Trabalho
7. Opcional: Abrir automaticamente

## 🔒 Segurança

- Validação de entrada (apenas `0-9`, `:`, espaço)
- Sanitização de paste
- Backup automático em corrupção
- Lock em operações concorrentes
- Validação SHA256 em patches

## 📦 Sistema de Patches

### Estrutura
```
releases/
├── 4.1.0/              # Build base
│   ├── *.exe, *.dll
│   └── checksums.sha256
├── patch_4.1.1/        # Patch incremental
│   ├── files/
│   ├── manifest.json
│   └── checksums.sha256
└── Output/
    ├── ValidadorJornada_Setup_4.1.1.exe
    └── ValidadorJornada_Patch_4.1.1.exe
```

### Manifest (patch)
```json
{
  "version": "4.1.1",
  "baseVersion": "4.1.0",
  "files": [
    {
      "path": "ValidadorJornada.exe",
      "size": 245760,
      "hash": "abc123...",
      "action": "update"
    }
  ],
  "newDependencies": ["QuestPDF.dll"],
  "removedFiles": []
}
```

### Garantias
- ✅ Validação SHA256
- ✅ Backup automático
- ✅ Rollback em falha
- ✅ Smoke test pós-instalação
- ✅ Verificação versão base

## 📊 Jornadas Suportadas

| Duração | Nome | Semanal | Mensal | Intervalo | Dias |
|---------|------|---------|--------|-----------|------|
| 04:00 | Parcial | 24h | 120h | Sem intervalo | Útil/Sáb |
| 05:50 | Reduzida | 35h | 175h | 15-120 min | Útil |
| 07:20 | Normal | 44h | 220h | 60-120 min | Útil/Sáb |
| 08:00 | Completa | 44h | 220h | 60-120 min | Útil |

## 📈 Regras de Validação

### Interjornada
- **Mínimo**: 11 horas consecutivas
- **Cálculo**: Fim jornada anterior → início próxima
- **Virada de dia**: Automática

### Períodos
- **Máximo diário**: 10 horas
- **Período contínuo**: 4 horas sem intervalo
- **Intervalo mínimo**: 15-60 min (conforme jornada)
- **Intervalo máximo**: 120 min

### Formato de Entrada
- **Válido**: `08:00`, `0800`, `8:00`, `800`
- **Auto-formatação**: Converte para `HH:MM`
- **Separador**: Espaço

## 🔄 Histórico e Cache

### Histórico
- Capacidade: 100 registros
- Expiração: 30 dias
- Ordenação: Mais recentes primeiro
- Duplicatas: Removidas automaticamente

### Cache
- Códigos: Em memória após primeira leitura
- Configurações: Carregadas uma vez
- Invalidação: Automática após importação

## 🛠️ Manutenção

### Backup Automático
```
%AppData%\ValidadorJornada\
├── codigos.json.corrupted_20250108120000.bak
└── historico.json.corrupted_20250108120000.bak
```

### Logs
```
%AppData%\ValidadorJornada\
├── errors.log              # Erros gerais
├── logs/export.log         # Exportações
└── logs/export_errors.log  # Erros de exportação
```

### Rollback Manual
```powershell
.\tools\RollbackHelper.ps1
```

## 🐛 Troubleshooting

**Erro: ".NET SDK não encontrado"**
→ Instalar .NET 8 SDK

**Erro: "Arquivo corrompido"**
→ Verificar backup `.corrupted_*.bak`

**Código não aparece**
→ Verificar formato do arquivo

**Interjornada não valida**
→ Ativar checkbox "Validar Interjornada"

**Validação lote trava**
→ Verificar se Excel está aberto

**PDF não abre**
→ Instalar leitor PDF (Adobe Reader)

**Exportação falha**
→ Verificar permissões Área de Trabalho

---

**Versão**: 4.1.2  
**Autor**: Samuel Fernandes - DP  
**Data**: Outubro 2025  
**Licença**: Uso interno
