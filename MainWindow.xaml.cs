using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private bool _isDarkTheme = true;

        private static readonly string ThemeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DMDevLabs", "RecuperadorPST", "theme.txt");

        public MainWindow()
        {
            InitializeComponent();
            LoadThemePreference();
        }

        // ── Theme ──

        private void ToggleThemeClick(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            SaveThemePreference();
        }

        private void ApplyTheme()
        {
            if (_isDarkTheme)
            {
                SetBrush("Brush.PrimaryBg", "#0B141A");
                SetBrush("Brush.SecondaryBg", "#111B21");
                SetBrush("Brush.HeaderBg", "#1F2C34");
                SetBrush("Brush.InputBg", "#2A3942");
                SetBrush("Brush.Border", "#233138");
                SetBrush("Brush.Accent", "#00A884");
                SetBrush("Brush.AccentHover", "#06CF9C");
                SetBrush("Brush.AccentDark", "#008069");
                SetBrush("Brush.TextPrimary", "#E9EDEF");
                SetBrush("Brush.TextDim", "#8696A0");
                SetBrush("Brush.Hover", "#1A2A33");
                ThemeToggleButton.Content = "Modo Claro";
            }
            else
            {
                SetBrush("Brush.PrimaryBg", "#F0F2F5");
                SetBrush("Brush.SecondaryBg", "#FFFFFF");
                SetBrush("Brush.HeaderBg", "#FFFFFF");
                SetBrush("Brush.InputBg", "#F0F2F5");
                SetBrush("Brush.Border", "#D1D7DB");
                SetBrush("Brush.Accent", "#008069");
                SetBrush("Brush.AccentHover", "#00A884");
                SetBrush("Brush.AccentDark", "#006652");
                SetBrush("Brush.TextPrimary", "#111B21");
                SetBrush("Brush.TextDim", "#667781");
                SetBrush("Brush.Hover", "#E8E8E8");
                ThemeToggleButton.Content = "Modo Oscuro";
            }
        }

        private static void SetBrush(string key, string hex)
        {
            var brush = Application.Current.Resources[key] as SolidColorBrush;
            if (brush != null)
            {
                brush.Color = (Color)ColorConverter.ConvertFromString(hex);
            }
        }

        private void LoadThemePreference()
        {
            try
            {
                if (File.Exists(ThemeFilePath))
                {
                    string saved = File.ReadAllText(ThemeFilePath).Trim();
                    if (saved == "light")
                    {
                        _isDarkTheme = false;
                        ApplyTheme();
                    }
                }
            }
            catch { }
        }

        private void SaveThemePreference()
        {
            try
            {
                string folder = Path.GetDirectoryName(ThemeFilePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(ThemeFilePath, _isDarkTheme ? "dark" : "light");
            }
            catch { }
        }

        // ── File Selection ──

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo PST",
                Filter = "Outlook PST (*.pst)|*.pst|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                PstPathTextBox.Text = dialog.FileName;
            }
        }

        // ── Recovery ──

        private void ExecuteClick(object sender, RoutedEventArgs e)
        {
            string path = PstPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(path))
            {
                ShowResult("[!] Selecciona un archivo PST primero.", false);
                return;
            }

            if (!File.Exists(path))
            {
                ShowResult("[!] El archivo seleccionado no existe.", false);
                return;
            }

            try
            {
                RecoverPassword(path);
            }
            catch (Exception ex)
            {
                _lastRecoveredKey = string.Empty;
                ShowResult("[!] Error durante el analisis\n\n" + ex.Message, false);
            }
        }

        private void RecoverPassword(string pstPath)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("[*] PST Password Recovery");
            log.AppendLine("========================================");
            log.AppendLine();
            log.AppendLine("Archivo: " + pstPath);
            log.AppendLine("Formato: Outlook Personal Storage Table");
            log.AppendLine();

            using (PSTFile file = new PSTFile(pstPath))
            {
                var pc = new PSTParse.LTP.PropertyContext(
                    PSTParse.NDB.SpecialNIDs.NID_MESSAGE_STORE, file);

                var passwordProperty = pc.Properties
                    .FirstOrDefault(p => p.Key == 0x67ff);

                bool hasPassword = passwordProperty.Key == 0x67ff
                    && passwordProperty.Value.Data != null
                    && !passwordProperty.Value.Data.SequenceEqual(new byte[] { 0, 0, 0, 0 });

                if (!hasPassword)
                {
                    _lastRecoveredKey = string.Empty;
                    log.AppendLine("[OK] Estructura valida");
                    log.AppendLine("[i]  El archivo PST no tiene contraseña");
                    log.AppendLine();
                    log.AppendLine("No hay clave para copiar.");
                    ShowResult(log.ToString(), false);
                    return;
                }

                uint crcValue = BitConverter.ToUInt32(passwordProperty.Value.Data, 0);
                string crcHex = BitConverter.ToString(passwordProperty.Value.Data)
                    .Replace("-", "").ToLowerInvariant();

                char[] collision = new CRC32_PW().findReverseAscii(crcValue);
                string recoveredKey = collision != null
                    ? new string(collision).TrimEnd('\0')
                    : string.Empty;

                _lastRecoveredKey = recoveredKey;

                log.AppendLine("[OK] Estructura valida");
                log.AppendLine("[OK] Contraseña detectada");
                log.AppendLine();
                log.AppendLine("CRC Hash:           " + crcHex);
                log.AppendLine("Clave recuperada:   " + recoveredKey);
                log.AppendLine();
                log.AppendLine("Usa 'Copiar Clave' para guardarla en el portapapeles.");
                ShowResult(log.ToString(), true);
            }
        }

        private void ShowResult(string text, bool success)
        {
            DetailsTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(text));

            if (success)
            {
                var accent = Application.Current.Resources["Brush.Accent"] as SolidColorBrush;
                if (accent != null)
                    paragraph.Foreground = new SolidColorBrush(accent.Color);
            }

            DetailsTextBox.Document.Blocks.Add(paragraph);
        }

        // ── Copy Key ──

        private void CopyKeyClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastRecoveredKey))
            {
                MessageBox.Show(
                    "No hay clave recuperada. Ejecuta un analisis primero.",
                    "Informacion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(_lastRecoveredKey);
            MessageBox.Show(
                "Clave copiada al portapapeles.",
                "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Terms & Conditions ──

        private void ShowTermsClick(object sender, RoutedEventArgs e)
        {
            var bg = Application.Current.Resources["Brush.SecondaryBg"] as SolidColorBrush;
            var border = Application.Current.Resources["Brush.Border"] as SolidColorBrush;
            var textPrimary = Application.Current.Resources["Brush.TextPrimary"] as SolidColorBrush;
            var textDim = Application.Current.Resources["Brush.TextDim"] as SolidColorBrush;
            var accent = Application.Current.Resources["Brush.Accent"] as SolidColorBrush;

            var win = new Window
            {
                Title = "Terminos y Condiciones",
                Width = 560,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                Owner = this,
                Background = new SolidColorBrush(bg != null ? bg.Color : Colors.White),
                MinWidth = 400,
                MinHeight = 350
            };

            var root = new Grid { Margin = new Thickness(0) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new StackPanel { Margin = new Thickness(22, 18, 22, 12) };
            header.Children.Add(new TextBlock
            {
                Text = "Terminos y Condiciones de Uso",
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

            // Body
            string terms =
                "1. Acerca de la aplicacion\n" +
                "PST Password Recovery es un software gratuito y de codigo abierto desarrollado " +
                "por DMDev Labs (www.dmdevlabs.com), distribuido bajo la licencia GNU General " +
                "Public License v3 (GPL-3.0). Su unico proposito es permitir la recuperacion " +
                "de contraseñas de archivos PST de Microsoft Outlook mediante colision CRC32.\n\n" +

                "2. Privacidad y tratamiento de datos\n" +
                "- No recopilamos datos. Esta aplicacion no recopila, almacena, transmite ni " +
                "comparte informacion personal de ningun tipo.\n" +
                "- Procesamiento 100% local. Todos los archivos se procesan exclusivamente en " +
                "tu dispositivo. Ningun dato sale de tu equipo.\n" +
                "- Sin conexion a internet. La aplicacion no requiere ni utiliza conexion a " +
                "internet para funcionar.\n" +
                "- Sin telemetria ni rastreo. No utilizamos cookies, analytics, telemetria ni " +
                "ningun mecanismo de seguimiento.\n\n" +

                "3. Responsabilidad del usuario\n" +
                "- El usuario es el unico responsable de los archivos que carga en la aplicacion " +
                "y del uso que les da.\n" +
                "- El usuario debe asegurarse de contar con autorizacion para acceder a los " +
                "archivos PST que analiza.\n" +
                "- DMDev Labs no se hace responsable del uso indebido de la aplicacion.\n\n" +

                "4. Propiedad intelectual\n" +
                "- Microsoft Outlook y PST son marcas registradas de Microsoft Corporation. " +
                "Esta aplicacion no esta afiliada, patrocinada ni respaldada por Microsoft.\n" +
                "- Esta aplicacion utiliza la tecnica publica de colision CRC32 para recuperar " +
                "contraseñas de archivos PST.\n\n" +

                "5. Limitacion de responsabilidad\n" +
                "Esta aplicacion se proporciona \"tal cual\" (AS IS), sin garantias de ningun tipo. " +
                "DMDev Labs no sera responsable por daños directos, indirectos, incidentales o " +
                "consecuentes derivados del uso de esta aplicacion.\n\n" +

                "6. Licencia de codigo abierto\n" +
                "El codigo fuente esta disponible bajo la licencia GPL-3.0. Puedes copiar, modificar " +
                "y redistribuir el software siempre que mantengas la misma licencia y atribuyas la " +
                "autoria original a DMDev Labs.\n\n" +

                "7. Contacto\n" +
                "Para consultas, reportes de errores o contribuciones, visita www.dmdevlabs.com.";

            var body = new TextBox
            {
                Text = terms,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
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

            // Footer
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(22, 14, 22, 14)
            };

            var closeBtn = new Button
            {
                Content = "Aceptar",
                Width = 100,
                Height = 34,
                Background = new SolidColorBrush(accent != null ? accent.Color : Colors.Green),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
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
            var bg = Application.Current.Resources["Brush.SecondaryBg"] as SolidColorBrush;
            var textPrimary = Application.Current.Resources["Brush.TextPrimary"] as SolidColorBrush;
            var textDim = Application.Current.Resources["Brush.TextDim"] as SolidColorBrush;
            var accent = Application.Current.Resources["Brush.Accent"] as SolidColorBrush;

            var about = new Window
            {
                Title = "Acerca de",
                Width = 460,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Background = new SolidColorBrush(bg != null ? bg.Color : Colors.White)
            };

            var root = new StackPanel { Margin = new Thickness(28) };

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
                Text = "v2.0.0",
                FontSize = 12,
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                Margin = new Thickness(0, 0, 0, 16)
            });

            root.Children.Add(new TextBlock
            {
                Text = "Herramienta open source para recuperar contraseñas\n" +
                       "de archivos Outlook PST mediante colision CRC32.\n\n" +
                       "Las contraseñas PST usan un hash CRC32 debil.\n" +
                       "Esta herramienta encuentra una colision valida\n" +
                       "que permite abrir el archivo protegido.",
                FontSize = 12,
                Foreground = new SolidColorBrush(textDim != null ? textDim.Color : Colors.Gray),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 0, 0, 16)
            });

            root.Children.Add(new TextBlock
            {
                Text = "Licencia: GPL-3.0\nDesarrollado por DMDev Labs\nwww.dmdevlabs.com",
                FontSize = 11,
                Foreground = new SolidColorBrush(accent != null ? accent.Color : Colors.Green),
                LineHeight = 18
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
