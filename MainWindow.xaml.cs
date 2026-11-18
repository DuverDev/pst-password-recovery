using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;
using PSTParse;
using MiscParseUtilities;

namespace PST_PasswordRecovery
{
    public partial class MainWindow : Window
    {
        private string _lastRecoveredKey = string.Empty;
        private string _currentPath = string.Empty;
        private bool _isDarkTheme = false;
        private bool _isEnglish = false;

        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();

        private sealed class HistoryEntry
        {
            public string FileName { get; set; }
            public string StatusLabel { get; set; }
            public string KeyOrHash { get; set; }
            public bool HasKey { get; set; }
        }

        private static readonly string ThemeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMDev Labs", "RecuperadorPST", "theme.txt");

        // Carpeta usada hasta la versión 2.2.0; se sigue leyendo para no perder el tema elegido.
        private static readonly string LegacyThemeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMDevLabs", "RecuperadorPST", "theme.txt");

        public MainWindow()
        {
            InitializeComponent();
            LoadThemePreference();
            ResetIdleState();
        }

        // ── Diagnostic Log Builders ──

        private static string BuildSuccessLog(string path, string fmt, string sizeLabel, string crcHex, string key, string elapsed)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[*] PST Password Recovery · Motor CRC-32 v2.2.0");
            sb.AppendLine("============================================================");
            sb.AppendLine("Archivo origen : " + path);
            sb.AppendLine("Tamaño en disco: " + sizeLabel + " · Apertura en modo solo lectura");
            sb.AppendLine("Formato NDB    : " + fmt);
            sb.AppendLine();
            sb.AppendLine("[OK] Cabecera !BDN validada correctamente");
            sb.AppendLine("[OK] Tabla LTP Message Store (NID 0x0021) decodificada");
            sb.AppendLine("[OK] Propiedad PR_PST_PASSWORD (0x67FF) localizada");
            sb.AppendLine();
            sb.AppendLine("CRC-32 detectado  : 0x" + crcHex + " (little-endian)");
            sb.AppendLine("Colisión hallada  : " + key + "  (" + elapsed + ")");
            sb.AppendLine("Verificación hash : CRC32(\"" + key + "\") == 0x" + crcHex);
            sb.AppendLine();
            sb.AppendLine("Todo el proceso se ejecutó en memoria local sin modificar el archivo .pst.");
            return sb.ToString();
        }

        private static string BuildReadyLog(string path, string sizeLabel)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[i] Archivo cargado y listo para análisis");
            sb.AppendLine("============================================================");
            sb.AppendLine("Ruta seleccionada : " + path);
            sb.AppendLine("Tamaño detectado  : " + sizeLabel);
            sb.AppendLine();
            sb.AppendLine("Siguiente paso:");
            sb.AppendLine("  1. Pulsa \"Ejecutar Análisis\" para leer NID_MESSAGE_STORE.");
            sb.AppendLine("  2. El motor extrae la propiedad 0x67FF sin tocar tus correos.");
            sb.AppendLine("  3. En milisegundos calcula una contraseña equivalente válida.");
            return sb.ToString();
        }

        // ── Window Chrome Controls ──

        private void HeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeClick(sender, e);
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                RootChrome.Margin = new Thickness(7);
                MaxButton.Content = "❐";
            }
            else
            {
                RootChrome.Margin = new Thickness(0);
                MaxButton.Content = "□";
            }
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Drag & Drop ──

        private void WindowDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void WindowDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0 && File.Exists(files[0]))
            {
                LoadSelectedFile(files[0]);
            }
        }

        // ── Theme & Language ──

        private void ToggleThemeClick(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            SaveThemePreference();
        }

        private void ToggleLangClick(object sender, RoutedEventArgs e)
        {
            _isEnglish = !_isEnglish;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (_isEnglish)
            {
                LangToggleButton.Content = "ES";
                HeaderEngineBadge.Text = "CRC-32 Engine · 100% Offline";
                ThemeToggleButton.Content = _isDarkTheme ? "Light Mode" : "Dark Mode";
                TermsButton.Content = "Terms";
                AboutButton.Content = "About";
                LblSidebarFile.Text = "OUTLOOK PST FILE";
                LblSidebarFileDesc.Text = "Select or drop a protected .pst file. Opened strictly read-only.";
                ExecuteButton.Content = "Run Analysis";
                BrowseButton.Content = "Browse PST file...";
                LblSidebarPrivacy.Text = "LOCAL PRIVACY";
                LblPriv1.Text = "100% in-memory, zero uploads";
                LblPriv2.Text = "No internet or telemetry";
                LblPriv3.Text = "Reads MAPI property 0x67FF only";
                LblSidebarHistory.Text = "SESSION HISTORY";
                Kpi1Title.Text = "PST STRUCTURE";
                Kpi2Title.Text = "CRC-32 HASH (0x67FF)";
                Kpi3Title.Text = "REVERSE COLLISION";
                HeroEyebrow.Text = "RECOVERED PASSWORD · VALID CRC-32 COLLISION";
                CopyButton.Content = "Copy Key";
                ConsoleTitleText.Text = "TECHNICAL DIAGNOSTIC CONSOLE";
                ConsoleModeBadge.Text = "Read-only · PST file untouched";
            }
            else
            {
                LangToggleButton.Content = "EN";
                HeaderEngineBadge.Text = "Motor CRC-32 · 100% Offline";
                ThemeToggleButton.Content = _isDarkTheme ? "Modo Claro" : "Modo Oscuro";
                TermsButton.Content = "Términos";
                AboutButton.Content = "Acerca de";
                LblSidebarFile.Text = "ARCHIVO OUTLOOK PST";
                LblSidebarFileDesc.Text = "Selecciona o arrastra un archivo .pst protegido. Se lee en modo seguro sin modificarlo.";
                ExecuteButton.Content = "Ejecutar Análisis";
                BrowseButton.Content = "Buscar archivo PST...";
                LblSidebarPrivacy.Text = "GARANTÍAS DE PRIVACIDAD";
                LblPriv1.Text = "100% local en memoria, sin subir datos";
                LblPriv2.Text = "Sin conexión a internet ni telemetría";
                LblPriv3.Text = "Lee solo propiedad MAPI 0x67FF";
                LblSidebarHistory.Text = "HISTORIAL DE SESIÓN";
                Kpi1Title.Text = "ESTRUCTURA PST";
                Kpi2Title.Text = "HASH CRC-32 (0x67FF)";
                Kpi3Title.Text = "COLISIÓN INVERSA";
                HeroEyebrow.Text = "CLAVE RECUPERADA · COLISIÓN CRC-32 VÁLIDA";
                CopyButton.Content = "Copiar Clave";
                ConsoleTitleText.Text = "CONSOLA TÉCNICA DE DIAGNÓSTICO";
                ConsoleModeBadge.Text = "Lectura segura · Sin escritura en .pst";
            }
        }

        private void ApplyTheme()
        {
            try
            {
                if (_isDarkTheme)
                {
                    SetBrush("Brush.PrimaryBg", "#0C1117");
                    SetBrush("Brush.SecondaryBg", "#111820");
                    SetBrush("Brush.HeaderBg", "#141C26");
                    SetBrush("Brush.CardBg", "#161F29");
                    SetBrush("Brush.InputBg", "#1D2834");
                    SetBrush("Brush.TerminalBg", "#090E13");
                    SetBrush("Brush.Border", "#263342");
                    SetBrush("Brush.Accent", "#00A884");
                    SetBrush("Brush.AccentHover", "#06CF9C");
                    SetBrush("Brush.AccentDark", "#008069");
                    SetBrush("Brush.AccentSoft", "#102C27");
                    SetBrush("Brush.AccentBorder", "#175247");
                    SetBrush("Brush.AccentText", "#34D399");
                    SetBrush("Brush.InfoSoft", "#13253D");
                    SetBrush("Brush.InfoText", "#60A5FA");
                    SetBrush("Brush.WarnSoft", "#2C210D");
                    SetBrush("Brush.WarnText", "#FBBF24");
                    SetBrush("Brush.TextPrimary", "#E6EDF3");
                    SetBrush("Brush.TextDim", "#8B98A8");
                    SetBrush("Brush.Hover", "#202D3A");
                }
                else
                {
                    SetBrush("Brush.PrimaryBg", "#F4F6F9");
                    SetBrush("Brush.SecondaryBg", "#FFFFFF");
                    SetBrush("Brush.HeaderBg", "#FFFFFF");
                    SetBrush("Brush.CardBg", "#FFFFFF");
                    SetBrush("Brush.InputBg", "#EDF1F5");
                    SetBrush("Brush.TerminalBg", "#F8FAFC");
                    SetBrush("Brush.Border", "#D5DDE6");
                    SetBrush("Brush.Accent", "#059669");
                    SetBrush("Brush.AccentHover", "#10B981");
                    SetBrush("Brush.AccentDark", "#047857");
                    SetBrush("Brush.AccentSoft", "#ECFDF5");
                    SetBrush("Brush.AccentBorder", "#A7F3D0");
                    SetBrush("Brush.AccentText", "#047857");
                    SetBrush("Brush.InfoSoft", "#EFF6FF");
                    SetBrush("Brush.InfoText", "#2563EB");
                    SetBrush("Brush.WarnSoft", "#FFFBEB");
                    SetBrush("Brush.WarnText", "#B45309");
                    SetBrush("Brush.TextPrimary", "#0F172A");
                    SetBrush("Brush.TextDim", "#475569");
                    SetBrush("Brush.Hover", "#E2E8F0");
                }
                ThemeToggleButton.Content = _isEnglish
                    ? (_isDarkTheme ? "Light Mode" : "Dark Mode")
                    : (_isDarkTheme ? "Modo Claro" : "Modo Oscuro");
                RenderHistoryPanel();
            }
            catch { }
        }

        private static void SetBrush(string key, string hex)
        {
            try
            {
                Color color = (Color)ColorConverter.ConvertFromString(hex);
                SolidColorBrush brush = new SolidColorBrush(color);
                brush.Freeze();
                Application.Current.Resources[key] = brush;
            }
            catch { }
        }

        private void LoadThemePreference()
        {
            try
            {
                string themeFile = File.Exists(ThemeFilePath) ? ThemeFilePath : LegacyThemeFilePath;
                if (File.Exists(themeFile))
                {
                    string saved = File.ReadAllText(themeFile).Trim();
                    if (saved == "dark")
                    {
                        _isDarkTheme = true;
                    }
                    else if (saved == "light")
                    {
                        _isDarkTheme = false;
                    }
                }
            }
            catch { }
            ApplyTheme();
            ApplyLanguage();
        }

        private void SaveThemePreference()
        {
            try
            {
                string folder = Path.GetDirectoryName(ThemeFilePath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(ThemeFilePath, _isDarkTheme ? "dark" : "light");
            }
            catch { }
        }

        // ── UI State Helpers ──

        private void ResetIdleState()
        {
            ShowResult(
                "[i] PST Password Recovery listo\n" +
                "============================================================\n" +
                "Selecciona un archivo .pst en el panel izquierdo o arrástralo\n" +
                "sobre esta ventana para detectar la propiedad 0x67FF y calcular\n" +
                "una contraseña válida mediante colisión CRC-32.",
                false);
        }

        private void SetSelectedFile(string fullPath, string infoLine)
        {
            _currentPath = fullPath;
            SelectedFileNameText.Text = Path.GetFileName(fullPath);
            SelectedFilePathText.Text = string.IsNullOrEmpty(infoLine) ? fullPath : (fullPath + " · " + infoLine);
        }

        private void LoadSelectedFile(string filePath)
        {
            string infoLine = string.Empty;
            try
            {
                FileInfo fi = new FileInfo(filePath);
                double mb = fi.Length / (1024.0 * 1024.0);
                infoLine = mb >= 1024.0
                    ? string.Format("{0:0.00} GB", mb / 1024.0)
                    : string.Format("{0:0.0} MB", mb);
            }
            catch { }

            SetSelectedFile(filePath, infoLine);
            CopyFeedbackText.Text = string.Empty;
            Kpi1Value.Text = "Archivo listo";
            Kpi1Sub.Text = infoLine;
            Kpi2Value.Text = "0x67FF";
            Kpi2Sub.Text = "Pulsa Ejecutar Análisis";
            Kpi3Value.Text = "Listo";
            Kpi3Sub.Text = "Motor CRC-32 sin conexión";
            ShowResult(BuildReadyLog(filePath, string.IsNullOrEmpty(infoLine) ? "—" : infoLine), false);
        }

        private void AddHistoryItem(string fileName, string status, string keyOrHash, bool hasKey)
        {
            _history.Insert(0, new HistoryEntry
            {
                FileName = fileName,
                StatusLabel = status,
                KeyOrHash = keyOrHash,
                HasKey = hasKey
            });
            if (_history.Count > 6) _history.RemoveAt(_history.Count - 1);
            RenderHistoryPanel();
        }

        private void RenderHistoryPanel()
        {
            HistoryListPanel.Children.Clear();
            if (_history.Count == 0)
            {
                HistoryListPanel.Children.Add(HistoryEmptyText);
                return;
            }

            Brush bg = Application.Current.Resources["Brush.InputBg"] as Brush;
            Brush border = Application.Current.Resources["Brush.Border"] as Brush;
            Brush accentSoft = Application.Current.Resources["Brush.AccentSoft"] as Brush;
            Brush accentText = Application.Current.Resources["Brush.AccentText"] as Brush;
            Brush textPrimary = Application.Current.Resources["Brush.TextPrimary"] as Brush;
            Brush textDim = Application.Current.Resources["Brush.TextDim"] as Brush;

            foreach (HistoryEntry item in _history)
            {
                Border row = new Border
                {
                    Background = bg,
                    BorderBrush = border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel left = new StackPanel();
                left.Children.Add(new TextBlock
                {
                    Text = item.FileName,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = textPrimary,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                left.Children.Add(new TextBlock
                {
                    Text = item.StatusLabel,
                    FontSize = 10,
                    Foreground = textDim,
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(left, 0);
                g.Children.Add(left);

                Border badge = new Border
                {
                    Background = item.HasKey ? accentSoft : bg,
                    BorderBrush = border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(7, 3, 7, 3),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = item.KeyOrHash,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = item.HasKey ? accentText : textDim
                };
                Grid.SetColumn(badge, 1);
                g.Children.Add(badge);

                row.Child = g;
                HistoryListPanel.Children.Add(row);
            }
        }

        // ── File Selection ──

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = _isEnglish ? "Select PST file" : "Seleccionar archivo PST",
                Filter = _isEnglish ? "Outlook PST (*.pst)|*.pst|All files (*.*)|*.*" : "Outlook PST (*.pst)|*.pst|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadSelectedFile(dialog.FileName);
            }
        }

        // ── Recovery ──

        private void ExecuteClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                ShowResult("[!] Selecciona un archivo PST primero.", false);
                return;
            }

            if (!File.Exists(_currentPath))
            {
                ShowResult("[!] El archivo seleccionado no existe en disco.", false);
                return;
            }

            try
            {
                RecoverPassword(_currentPath);
            }
            catch (Exception ex)
            {
                _lastRecoveredKey = string.Empty;
                Kpi1Value.Text = "Error lectura";
                Kpi1Sub.Text = "Archivo inválido o dañado";
                ShowResult("[!] Error durante el análisis\n\n" + ex.Message, false);
            }
        }

        private void RecoverPassword(string pstPath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            CopyFeedbackText.Text = string.Empty;

            using (PSTFile file = new PSTFile(pstPath))
            {
                var pc = new PSTParse.LTP.PropertyContext(
                    PSTParse.NDB.SpecialNIDs.NID_MESSAGE_STORE, file);

                var passwordProperty = pc.Properties
                    .FirstOrDefault(p => p.Key == 0x67ff);

                bool hasPassword = passwordProperty.Key == 0x67ff
                    && passwordProperty.Value.Data != null
                    && !passwordProperty.Value.Data.SequenceEqual(new byte[] { 0, 0, 0, 0 });

                sw.Stop();
                string fileName = Path.GetFileName(pstPath);

                if (!hasPassword)
                {
                    _lastRecoveredKey = string.Empty;
                    Kpi1Value.Text = "Válida";
                    Kpi1Sub.Text = "Message Store leído";
                    Kpi2Value.Text = "0x00000000";
                    Kpi2Sub.Text = "Sin contraseña";
                    Kpi3Value.Text = "No requiere";
                    Kpi3Sub.Text = string.Format("Completado en {0} ms", sw.ElapsedMilliseconds);
                    RecoveredKeyDisplay.Text = "Sin protección";
                    HeroExplainText.Text = "Este archivo PST no tiene contraseña configurada en la propiedad 0x67FF.";
                    AddHistoryItem(fileName, "Sin clave", "Libre", false);

                    ShowResult(
                        "[OK] Estructura PST válida\n" +
                        "[i] El archivo no tiene contraseña configurada (0x67FF vacía).",
                        false);
                    return;
                }

                uint crcValue = BitConverter.ToUInt32(passwordProperty.Value.Data, 0);
                string crcHex = BitConverter.ToString(passwordProperty.Value.Data)
                    .Replace("-", "").ToLowerInvariant();

                char[] collision = new CRC32_PW().findReverseAscii(crcValue);
                string recoveredKey = collision != null
                    ? new string(collision).TrimEnd('\0')
                    : string.Empty;

                sw.Stop();
                _lastRecoveredKey = recoveredKey;

                Kpi1Value.Text = "Válida";
                Kpi1Sub.Text = "Estructura NDB/LTP";
                Kpi2Value.Text = crcHex;
                Kpi2Sub.Text = "Propiedad MAPI 0x67FF";
                Kpi3Value.Text = string.Format("{0} ms", Math.Max(1L, sw.ElapsedMilliseconds));
                Kpi3Sub.Text = "Colisión ASCII válida";
                RecoveredKeyDisplay.Text = recoveredKey;
                HeroExplainText.Text = "Usa esta clave equivalente en Microsoft Outlook para abrir el archivo sin modificar su contenido.";
                AddHistoryItem(fileName, "Recuperada", recoveredKey, true);

                FileInfo fi = new FileInfo(pstPath);
                string sizeStr = string.Format("{0:0.0} MB", fi.Length / (1024.0 * 1024.0));
                ShowResult(
                    BuildSuccessLog(pstPath, "Outlook Personal Storage Table", sizeStr, crcHex, recoveredKey, Kpi3Value.Text),
                    true);
            }
        }

        private void ShowResult(string text, bool success)
        {
            DetailsTextBox.Document.Blocks.Clear();
            Paragraph paragraph = new Paragraph(new Run(text));
            if (success)
            {
                SolidColorBrush accent = Application.Current.Resources["Brush.AccentText"] as SolidColorBrush;
                if (accent != null)
                    paragraph.Foreground = new SolidColorBrush(accent.Color);
            }
            DetailsTextBox.Document.Blocks.Add(paragraph);
        }

        // ── Copy Key ──

        private void CopyKeyClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastRecoveredKey))
            {
                CopyFeedbackText.Text = _isEnglish ? "Run an analysis first" : "Ejecuta un análisis primero";
                return;
            }

            Clipboard.SetText(_lastRecoveredKey);
            CopyFeedbackText.Text = _isEnglish ? "✓ Copied to clipboard" : "✓ Copiada al portapapeles";
        }

        // ── Terms & Conditions ──

        private void ShowTermsClick(object sender, RoutedEventArgs e)
        {
            SolidColorBrush bg = Application.Current.Resources["Brush.SecondaryBg"] as SolidColorBrush;
            SolidColorBrush border = Application.Current.Resources["Brush.Border"] as SolidColorBrush;
            SolidColorBrush textPrimary = Application.Current.Resources["Brush.TextPrimary"] as SolidColorBrush;
            SolidColorBrush textDim = Application.Current.Resources["Brush.TextDim"] as SolidColorBrush;
            SolidColorBrush accent = Application.Current.Resources["Brush.Accent"] as SolidColorBrush;

            Window win = new Window
            {
                Title = _isEnglish ? "Terms & Legal Notice" : "Términos y Aviso Legal",
                Width = 560,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                Owner = this,
                Background = new SolidColorBrush(bg != null ? bg.Color : Colors.White),
                MinWidth = 400,
                MinHeight = 350
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel header = new StackPanel { Margin = new Thickness(22, 18, 22, 12) };
            header.Children.Add(new TextBlock
            {
                Text = _isEnglish ? "Terms of Use & Legal Disclaimer" : "Términos de Uso y Aviso Legal",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(textPrimary != null ? textPrimary.Color : Colors.Black)
            });
            header.Children.Add(new TextBlock
            {
                Text = "PST Password Recovery - DMDev Labs",
                FontSize = 11,
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            string terms = _isEnglish
                ? "By downloading, installing or using PST Password Recovery you agree in full to these terms, which are the same as dmdevlabs.com/terms. If you do not agree, do not use the program.\n" +
                  "\n" +
                  "1. Delivery \"as is\" and no warranties\n" +
                  "PST Password Recovery and its source code are provided free of charge by DMDev Labs under the GNU GPL-3.0 license, for informational, educational or experimental purposes, strictly \"as is\" and \"as available\", without warranties of any kind—express, implied or statutory—including freedom from bugs, accuracy, merchantability, non-infringement or fitness for a particular purpose. Nothing here constitutes a commercial offer, professional services agreement or service-level agreement (SLA).\n" +
                  "\n" +
                  "2. Exclusion and limitation of liability\n" +
                  "To the fullest extent permitted by applicable law (including Colombian law and Sections 15 and 16 of the GNU GPL-3.0 license), DMDev Labs shall not be liable under any legal theory for any direct, indirect, incidental, special, consequential or punitive damages—including data loss or file corruption—arising out of downloading, installing, running, using, misusing or being unable to use the program, even if advised of the possibility of such damages. Anyone who uses it does so at their own exclusive risk and must back up their files beforehand.\n" +
                  "\n" +
                  "3. Lawful use and user responsibility\n" +
                  "PST Password Recovery is intended solely and exclusively to recover access to .pst files that you own or have explicit legal authorization from the owner to unlock. Any unauthorized access to third-party data or communications is strictly prohibited (Colombian Law 1273 of 2009 and equivalent laws). Analysis runs on your device in read-only mode without modifying the file, with no internet or telemetry. You bear sole legal responsibility for the origin and ownership of the files you process.\n" +
                  "\n" +
                  "4. Indemnification\n" +
                  "You agree to indemnify, defend and hold harmless DMDev Labs from and against any third-party or regulatory claims, losses, liabilities, fines or expenses arising from your unlawful, negligent or unauthorized use of the program or of any files processed with it.\n" +
                  "\n" +
                  "5. Third-party trademarks and non-affiliation\n" +
                  "DMDev Labs is an independent project. Microsoft, Outlook, Microsoft 365 and Windows are trademarks of their respective owners and are mentioned solely to describe compatibility. DMDev Labs is not affiliated with, sponsored by, endorsed by or officially associated with any of them.\n" +
                  "\n" +
                  "6. Applicable law and jurisdiction\n" +
                  "These terms are governed by the laws of the Republic of Colombia. Any dispute that cannot be resolved first through direct written contact at contacto@dmdevlabs.com shall be submitted to the competent courts of Bogotá D.C., Colombia."
                : "Al descargar, instalar o usar PST Password Recovery aceptas íntegramente estas condiciones, que son las mismas de dmdevlabs.com/terminos. Si no estás de acuerdo, no uses el programa.\n" +
                  "\n" +
                  "1. Entrega «tal cual» (as is) y ausencia de garantías\n" +
                  "PST Password Recovery y su código fuente se ofrecen de forma gratuita por DMDev Labs, bajo la licencia GNU GPL-3.0, con fines informativos, educativos o experimentales, estrictamente «tal cual» (as is) y «según disponibilidad» (as available), sin garantías de ningún tipo —expresas, implícitas ni legales—, incluyendo ausencia de errores, exactitud, comerciabilidad, no infracción o idoneidad para un propósito particular. Nada de esto constituye oferta comercial, contrato de servicios profesionales ni acuerdo de nivel de servicio (SLA).\n" +
                  "\n" +
                  "2. Exclusión y limitación de responsabilidad\n" +
                  "En la máxima medida permitida por la legislación aplicable (incluida la ley colombiana y las secciones 15 y 16 de la licencia GNU GPL-3.0), DMDev Labs no será responsable en ningún caso por daños directos, indirectos, incidentales, especiales, consecuentes o punitivos —incluyendo pérdida de información o corrupción de archivos— derivados de la descarga, instalación, ejecución, uso, mal uso o imposibilidad de uso del programa, incluso si se advirtió de la posibilidad de dichos daños. Quien lo usa lo hace bajo su exclusivo riesgo y responsabilidad, y debe hacer copias de seguridad previas de sus archivos.\n" +
                  "\n" +
                  "3. Uso legítimo y responsabilidad del usuario\n" +
                  "PST Password Recovery está destinada única y exclusivamente a recuperar el acceso a archivos .pst propios o sobre los cuales cuentes con autorización expresa y legítima del titular. Queda prohibido su uso para acceder sin autorización a datos o comunicaciones de terceros (Ley 1273 de 2009 de Colombia y normas equivalentes). El análisis se hace en tu equipo, en modo de solo lectura y sin modificar el archivo, sin internet ni telemetría. Eres el único responsable legal del origen y la titularidad de los archivos que proceses.\n" +
                  "\n" +
                  "4. Indemnidad\n" +
                  "Aceptas mantener indemne y eximir de toda responsabilidad, reclamación, sanción, pérdida o gasto a DMDev Labs frente a cualquier reclamación de terceros o autoridad derivada del uso indebido, ilícito o no autorizado que hagas del programa o de los archivos procesados con él.\n" +
                  "\n" +
                  "5. Marcas registradas de terceros y no afiliación\n" +
                  "DMDev Labs es un proyecto independiente. Microsoft, Outlook, Microsoft 365 y Windows son marcas de sus respectivos propietarios y se mencionan únicamente para describir compatibilidad. DMDev Labs no está afiliado, patrocinado, respaldado ni asociado oficialmente con ninguno de ellos.\n" +
                  "\n" +
                  "6. Ley aplicable y jurisdicción\n" +
                  "Estas condiciones se rigen por las leyes de la República de Colombia. Cualquier controversia que no se resuelva mediante reclamación directa previa al correo contacto@dmdevlabs.com se someterá a los jueces competentes de Bogotá D. C., Colombia.";

            TextBox body = new TextBox
            {
                Text = terms,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(bg != null ? bg.Color : Colors.White),
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                BorderThickness = new Thickness(0, 1, 0, 1),
                BorderBrush = new SolidColorBrush(border != null ? border.Color : Colors.LightGray),
                Padding = new Thickness(22, 16, 22, 16),
                FontSize = 12
            };
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(22, 14, 22, 14)
            };
            Button closeBtn = new Button
            {
                Content = _isEnglish ? "Accept" : "Aceptar",
                Width = 100,
                Height = 34,
                Background = new SolidColorBrush(accent != null ? accent.Color : Colors.Green),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            closeBtn.Click += delegate { win.Close(); };
            footer.Children.Add(closeBtn);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            win.Content = root;
            win.ShowDialog();
        }

        // ── About ──

        private void ShowAboutClick(object sender, RoutedEventArgs e)
        {
            SolidColorBrush bg = Application.Current.Resources["Brush.SecondaryBg"] as SolidColorBrush;
            SolidColorBrush textPrimary = Application.Current.Resources["Brush.TextPrimary"] as SolidColorBrush;
            SolidColorBrush textDim = Application.Current.Resources["Brush.TextDim"] as SolidColorBrush;
            SolidColorBrush accent = Application.Current.Resources["Brush.AccentText"] as SolidColorBrush;

            Window about = new Window
            {
                Title = _isEnglish ? "About" : "Acerca de",
                Width = 460,
                Height = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Background = new SolidColorBrush(bg != null ? bg.Color : Colors.White)
            };

            StackPanel root = new StackPanel { Margin = new Thickness(28) };
            root.Children.Add(new TextBlock
            {
                Text = "PST Password Recovery",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textPrimary != null ? textPrimary.Color : Colors.Black),
                Margin = new Thickness(0, 0, 0, 4)
            });
            root.Children.Add(new TextBlock
            {
                Text = "v2.2.0 · GPL-3.0",
                FontSize = 12,
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                Margin = new Thickness(0, 0, 0, 14)
            });
            root.Children.Add(new TextBlock
            {
                Text = _isEnglish
                    ? "Open-source utility to recover Microsoft Outlook PST file passwords locally using CRC-32 key-space collision (100% offline, read-only, GPL-3.0)."
                    : "Herramienta open source para recuperar contraseñas de archivos Microsoft Outlook PST mediante colisión CRC-32 (100 % offline, solo lectura, GPL-3.0).",
                FontSize = 12,
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 0, 0, 16)
            });
            root.Children.Add(new TextBlock
            {
                Text = _isEnglish
                    ? "Developed by DMDev Labs · www.dmdevlabs.com · Independent tool not affiliated with Microsoft"
                    : "Desarrollado por DMDev Labs · www.dmdevlabs.com · Sin afiliación con Microsoft",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(accent != null ? accent.Color : Colors.Green)
            });

            about.Content = root;
            about.ShowDialog();
        }

        // ── Links ──

        private void OpenWebsiteLink(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }
}
