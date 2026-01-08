using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class CodigoService : IDisposable
    {
        private readonly string _codigosPath;
        private Dictionary<string, string>? _cacheCodigos;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly bool _skipHeaders;
        private bool _disposed = false;

        public CodigoService(bool skipHeaders = true)
        {
            _skipHeaders = skipHeaders;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "ValidadorJornada");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
                
            _codigosPath = Path.Combine(appFolder, "codigos.json");
        }

        public string? BuscarCodigo(string horarios)
        {
            if (_disposed) return null;
            
            if (!_lock.TryEnterReadLock(1000))
                return null;
                
            try
            {
                var codigos = CarregarCodigos();
                var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                
                if (codigos.TryGetValue(horarioNormalizado, out var codigo))
                {
                    return string.IsNullOrWhiteSpace(codigo) ? null : codigo;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao buscar código: {ex.Message}");
                return null;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        public void SalvarCodigo(string horarios, string codigo)
        {
            if (_disposed) return;
            
            if (!_lock.TryEnterWriteLock(1000))
                return;
                
            try
            {
                var codigos = CarregarCodigos();
                var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    codigos.Remove(horarioNormalizado);
                }
                else
                {
                    codigos[horarioNormalizado] = codigo;
                }
                
                PersistirCodigos(codigos);
                InvalidarCache();
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public Dictionary<string, string> ObterTodosCodigos()
        {
            if (_disposed) return new Dictionary<string, string>();
            
            if (!_lock.TryEnterReadLock(1000))
                return new Dictionary<string, string>();
                
            try
            {
                return new Dictionary<string, string>(CarregarCodigos());
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        public void RemoverCodigo(string horarios)
        {
            if (_disposed) return;
            
            if (!_lock.TryEnterWriteLock(1000))
                return;
                
            try
            {
                var codigos = CarregarCodigos();
                var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                
                if (codigos.Remove(horarioNormalizado))
                {
                    PersistirCodigos(codigos);
                    InvalidarCache();
                }
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public bool IsAtivo()
        {
            if (_disposed) return false;
            
            if (!_lock.TryEnterReadLock(1000))
                return false;
                
            try
            {
                return CarregarCodigos().Count > 0;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        public DateTime? ObterDataAtualizacao()
        {
            try
            {
                return File.Exists(_codigosPath) ? File.GetLastWriteTime(_codigosPath) : null;
            }
            catch
            {
                return null;
            }
        }

        public int ObterTotalCodigos()
        {
            if (_disposed) return 0;
            
            if (!_lock.TryEnterReadLock(1000))
                return 0;
                
            try
            {
                return CarregarCodigos().Count;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }

        public void SetAtivo(bool ativo)
        {
            if (_disposed) return;
            
            if (!_lock.TryEnterWriteLock(1000))
                return;
                
            try
            {
                if (!ativo)
                {
                    PersistirCodigos(new Dictionary<string, string>());
                    InvalidarCache();
                }
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }

        public ImportResult ImportarArquivo(string caminhoArquivo)
        {
            var result = new ImportResult();
            
            if (_disposed)
            {
                result.Mensagem = "Serviço foi finalizado";
                return result;
            }
            
            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException("Arquivo não encontrado");

            var extensao = Path.GetExtension(caminhoArquivo).ToLower();
            Dictionary<string, string> codigosImportados;

            switch (extensao)
            {
                case ".xlsx":
                case ".xls":
                    codigosImportados = ImportarExcel(caminhoArquivo, result);
                    break;
                case ".csv":
                    codigosImportados = ImportarCsv(caminhoArquivo, result);
                    break;
                case ".json":
                    codigosImportados = ImportarJson(caminhoArquivo);
                    break;
                default:
                    throw new InvalidOperationException("Formato não suportado. Use .xlsx, .xls, .csv ou .json");
            }

            if (codigosImportados == null || codigosImportados.Count == 0)
                throw new InvalidOperationException("Nenhum código encontrado no arquivo");

            if (!_lock.TryEnterWriteLock(2000))
            {
                result.Mensagem = "Timeout ao obter lock para importação";
                return result;
            }
            
            try
            {
                var codigosAtuais = CarregarCodigos();
                foreach (var item in codigosImportados)
                {
                    codigosAtuais[item.Key] = item.Value;
                }

                PersistirCodigos(codigosAtuais);
                InvalidarCache();
                
                result.TotalImportado = codigosImportados.Count;
                result.Sucesso = true;
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
            
            return result;
        }

        private Dictionary<string, string> CarregarCodigos()
        {
            if (_cacheCodigos != null)
                return _cacheCodigos;

            // ✅ REFATORADO: Usa JsonFileHelper
            _cacheCodigos = JsonFileHelper.Load<Dictionary<string, string>>(_codigosPath) 
                ?? new Dictionary<string, string>();
            
            return _cacheCodigos;
        }

        private void PersistirCodigos(Dictionary<string, string> codigos)
        {
            // ✅ REFATORADO: Usa JsonFileHelper
            JsonFileHelper.Save(_codigosPath, codigos);
            _cacheCodigos = codigos;
        }

        private Dictionary<string, string> ImportarExcel(string caminhoArquivo, ImportResult result)
        {
            var codigos = new Dictionary<string, string>();
            
            try
            {
                var dados = ExcelHelper.LerArquivo(caminhoArquivo, _skipHeaders);
                result.TotalLinhas = dados.Count;
                
                foreach (var (codigo, horarios) in dados)
                {
                    if (!string.IsNullOrWhiteSpace(codigo) && !string.IsNullOrWhiteSpace(horarios))
                    {
                        var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                        codigos[horarioNormalizado] = codigo.Trim();
                        result.LinhasProcessadas++;
                    }
                }

                return codigos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao ler Excel: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> ImportarCsv(string caminhoArquivo, ImportResult result)
        {
            var codigos = new Dictionary<string, string>();
            var linhas = File.ReadAllLines(caminhoArquivo, System.Text.Encoding.UTF8);
            result.TotalLinhas = linhas.Length;
            
            int startIndex = _skipHeaders && linhas.Length > 0 ? 1 : 0;
            
            for (int i = startIndex; i < linhas.Length; i++)
            {
                var linha = linhas[i];
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                var partes = linha.Split(new[] { ',', ';', '\t' }, 2);
                
                if (partes.Length == 2)
                {
                    var codigo = partes[0].Trim().Trim('"');
                    var horarios = partes[1].Trim().Trim('"');
                    
                    if (!string.IsNullOrWhiteSpace(codigo) && !string.IsNullOrWhiteSpace(horarios))
                    {
                        var horarioNormalizado = HorarioNormalizer.Normalizar(horarios);
                        codigos[horarioNormalizado] = codigo;
                        result.LinhasProcessadas++;
                    }
                }
            }

            return codigos;
        }

        private Dictionary<string, string> ImportarJson(string caminhoArquivo)
        {
            // ✅ REFATORADO: Usa JsonFileHelper
            return JsonFileHelper.Load<Dictionary<string, string>>(caminhoArquivo) 
                ?? new Dictionary<string, string>();
        }

        private void InvalidarCache()
        {
            _cacheCodigos = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _lock?.Dispose();
                }
                catch { }
            }

            _disposed = true;
        }

        ~CodigoService()
        {
            Dispose(false);
        }
    }

    public class ImportResult
    {
        public bool Sucesso { get; set; }
        public int TotalLinhas { get; set; }
        public int LinhasProcessadas { get; set; }
        public int TotalImportado { get; set; }
        public string? Mensagem { get; set; }
    }
}
