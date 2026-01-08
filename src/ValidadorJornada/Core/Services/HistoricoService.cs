using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class HistoricoService : IDisposable
    {
        private readonly string _historicoPath;
        private List<HistoricoItem>? _cacheHistorico;
        private DateTime _ultimaAtualizacaoCache;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly int _cacheMinutos;
        private string? _jornadaPrincipalPendente = null;
        private bool _disposed = false;

        public HistoricoService(int cacheMinutos = 30)
        {
            _cacheMinutos = cacheMinutos;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "ValidadorJornada");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
                
            _historicoPath = Path.Combine(appFolder, "historico.json");
        }

        public void SalvarJornadaPrincipal(ValidationResult resultado, string horariosInput)
        {
            _jornadaPrincipalPendente = horariosInput;
            Salvar(resultado, horariosInput, false);
        }

        public void SalvarJornadaVinculada(ValidationResult resultado, string horariosSabado)
        {
            if (!string.IsNullOrEmpty(_jornadaPrincipalPendente))
            {
                var horariosCompletos = $"{_jornadaPrincipalPendente} + {horariosSabado}";
                Salvar(resultado, horariosCompletos, true);
                _jornadaPrincipalPendente = null;
            }
        }

        public void Salvar(ValidationResult resultado, string horariosInput, bool isVinculada = false)
        {
            if (_disposed) return;
            
            if (!_lock.TryEnterWriteLock(5000))
            {
                System.Diagnostics.Debug.WriteLine("Timeout ao obter lock para salvar");
                return;
            }

            try
            {
                var historico = CarregarHistoricoCompleto();
                
                var horariosNormalizados = HorarioNormalizer.Normalizar(horariosInput);
                
                historico.RemoveAll(h => 
                    HorarioNormalizer.Normalizar(h.Horarios ?? string.Empty) == horariosNormalizados &&
                    (DateTime.Now - h.Data).TotalMinutes < 5);
                
                var resultadoFormatado = FormatarResultadoComCodigo(resultado);
                
                var novoItem = new HistoricoItem
                {
                    Data = DateTime.Now,
                    Horarios = horariosInput ?? string.Empty,
                    Resultado = resultadoFormatado ?? string.Empty,
                    Valido = resultado.Valido,
                    IsVinculada = isVinculada
                };

                historico.Add(novoItem);

                historico.RemoveAll(h => (DateTime.Now - h.Data).TotalDays > 40);

                if (historico.Count > 200)
                {
                    historico = historico.OrderByDescending(h => h.Data).Take(200).ToList();
                }

                SalvarHistoricoCompleto(historico);
                InvalidarCache();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar histórico: {ex.Message}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private string FormatarResultadoComCodigo(ValidationResult resultado)
        {
            if (resultado == null) return string.Empty;
            
            var texto = resultado.Mensagem ?? string.Empty;
            
            if (texto.Contains("(Código:"))
                return texto;
            
            if (!string.IsNullOrWhiteSpace(resultado.Codigo))
            {
                var semEmoji = texto.TrimStart('✅', '❌', '⚠', ' ');
                texto = $"✅ {semEmoji} (Código: {resultado.Codigo})";
            }
            
            return texto;
        }

        public List<string> ObterTodos()
        {
            if (_disposed) return new List<string>();
            
            if (!_lock.TryEnterReadLock(5000))
            {
                return new List<string> { "⚠️ Erro ao carregar histórico" };
            }

            try
            {
                if (_cacheHistorico != null && 
                    (DateTime.Now - _ultimaAtualizacaoCache).TotalMinutes < _cacheMinutos)
                {
                    return FormatarHistorico(_cacheHistorico);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (!_lock.TryEnterWriteLock(5000))
            {
                return new List<string> { "⚠️ Erro ao carregar histórico" };
            }

            try
            {
                var historico = CarregarHistoricoCompleto();
                _cacheHistorico = historico;
                _ultimaAtualizacaoCache = DateTime.Now;
                
                return FormatarHistorico(historico);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter histórico: {ex.Message}");
                return new List<string> { "⚠️ Erro ao carregar histórico" };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<string> ObterRecentes(int quantidade)
        {
            if (_disposed) return new List<string>();
            
            if (!_lock.TryEnterReadLock(5000))
            {
                return new List<string>();
            }

            try
            {
                if (_cacheHistorico != null && 
                    (DateTime.Now - _ultimaAtualizacaoCache).TotalMinutes < _cacheMinutos)
                {
                    return FormatarHistorico(_cacheHistorico.OrderByDescending(h => h.Data).Take(quantidade).ToList());
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (!_lock.TryEnterWriteLock(5000))
                return new List<string>();

            try
            {
                var historico = CarregarHistoricoCompleto();
                _cacheHistorico = historico;
                _ultimaAtualizacaoCache = DateTime.Now;
                return FormatarHistorico(historico.OrderByDescending(h => h.Data).Take(quantidade).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter recentes: {ex.Message}");
                return new List<string>();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void LimparTudo()
        {
            if (_disposed) return;
            
            if (!_lock.TryEnterWriteLock(5000))
                return;

            try
            {
                if (File.Exists(_historicoPath))
                    File.Delete(_historicoPath);
                
                InvalidarCache();
                _jornadaPrincipalPendente = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private List<HistoricoItem> CarregarHistoricoCompleto()
        {
            // ✅ REFATORADO: Usa JsonFileHelper
            var items = JsonFileHelper.Load<List<HistoricoItem>>(_historicoPath) 
                ?? new List<HistoricoItem>();
            
            // Proteção contra null
            foreach (var item in items)
            {
                item.Horarios ??= string.Empty;
                item.Resultado ??= string.Empty;
            }
            
            return items;
        }

        private void SalvarHistoricoCompleto(List<HistoricoItem> historico)
        {
            // ✅ REFATORADO: Usa JsonFileHelper
            JsonFileHelper.Save(_historicoPath, historico);
        }

        private List<string> FormatarHistorico(List<HistoricoItem> items)
        {
            if (items == null) return new List<string>();
            
            return items
                .Where(h => h != null)
                .OrderByDescending(h => h.Data)
                .Select(h => $"[{h.Data:dd/MM/yyyy HH:mm}] {h.Horarios ?? string.Empty}\n{h.Resultado ?? string.Empty}")
                .ToList();
        }

        private void InvalidarCache()
        {
            _cacheHistorico = null;
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

        ~HistoricoService()
        {
            Dispose(false);
        }
    }

    public class HistoricoItem
    {
        public DateTime Data { get; set; }
        public string Horarios { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public bool Valido { get; set; }
        public bool IsVinculada { get; set; } = false;
    }
}
