using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Media;
using Vanara.PInvoke;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using HtmlAgilityPack;

namespace BackgroundTerminal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class _Debug
    {
        private readonly RichTextBox _tb;

        private string _color;
        private SolidColorBrush _colorBrush = Brushes.White;

        public _Debug(RichTextBox tb)
        {
            _tb = tb;
        }

        public void clear()
        {
            _tb.Document.Blocks.Clear();
        }

        public void Error(string message)
        {
            AppendText($"| ERROR | {DateTime.Now:HH:mm} | {message}", Brushes.Red);
        }

        public void Log(string message)
        {
            AppendText($"| INFO | {DateTime.Now:HH:mm} | {message}", _colorBrush);
        }

        public void Write(string message, string style = "default")
        {
            if (style.ToLower() == "lolcat")
            {
                AppendLolcatText(message);
            }
            else
            {
                AppendText(message, _colorBrush);
            }
        }

        private void AppendText(string text, Brush color)
        {
            var paragraph = new Paragraph(new Run(text)) { Foreground = color };
            _tb.Document.Blocks.Add(paragraph);
            _tb.ScrollToEnd();
        }

        private void AppendLolcatText(string text)
        {
            var paragraph = new Paragraph();
            var colors = new[] { Brushes.Red, Brushes.Orange, Brushes.Yellow, Brushes.LimeGreen, Brushes.SkyBlue, Brushes.Violet, Brushes.BlueViolet };
            int colorIndex = 0;

            foreach (char c in text)
            {
                var run = new Run(c.ToString()) { Foreground = colors[colorIndex] };
                paragraph.Inlines.Add(run);
                colorIndex = (colorIndex + 1) % colors.Length;
            }

            _tb.Document.Blocks.Add(paragraph);
            _tb.ScrollToEnd();
        }

        public void setColor(string color)
        {
            _color = color;
            _colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        public string useCmd(string command, int timeoutMs = 5000)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("CP866");
            process.StartInfo.StandardErrorEncoding = Encoding.GetEncoding("CP866");

            process.Start();

            // Считаем потоки асинхронно, чтобы избежать блокировки буфера
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            bool exited = process.WaitForExit(timeoutMs);
            if (!exited)
            {
                try
                {
                    process.Kill();
                }
                catch { /* Процесс мог завершиться уже */ }
                Error($"Команда '{command}' превысила таймаут {timeoutMs} мс и была прервана.");
                return"";
            }

            // Дождёмся окончательного чтения потоков
            Task.WaitAll(outputTask, errorTask);

            string output = outputTask.Result;
            string error = errorTask.Result;

            return string.IsNullOrWhiteSpace(error) ? output : output + "\nОшибка:\n" + error;
        }
    }
    public partial class MainWindow : Window
    {
        private const string HELP_TEXT = @"
╔═════════════════════════════════════════════════════════════════╗
║                     СПРАВОЧНИК КОМАНД                           ║
╠═════════════════════════════════════════════════════════════════╣
║ Справка и информация                                            ║
╠════════════════════════════╦════════════════════════════════════╣
║ Команда и параметры        │ Описание                           ║
╠════════════════════════════╬════════════════════════════════════╣
║ help                       │ Показать эту справку               ║
║ colorhelp                  │ Примеры допустимых цветов          ║
║ time                       │ Показать текущее время             ║
║ newquote                   │ Показать новую случайную цитату    ║
║ posts                      │ Обновить посты DTF и Shikimori     ║
╠═════════════════════════════════════════════════════════════════╣
║ Управление интерфейсом                                          ║
╠════════════════════════════╦════════════════════════════════════╣
║ clear                      │ Очистить экран консоли             ║
║ setcolor <цвет>            │ Установить цвет интерфейса (текст) ║
║ setback <цвет>             │ Установить фоновый цвет окна       ║
║ bringmeback <px>           │ Установить высоту окна             ║
╠═════════════════════════════════════════════════════════════════╣
║ Системные и служебные                                           ║
╠════════════════════════════╦════════════════════════════════════╣
║ autolaunch                 │ Включить/отключить автозапуск      ║
║ close                      │ Закрыть программу                  ║
║ cmd <команда>              │ Выполнить команду в cmd            ║
║ crush                      │ Сгенерировать тестовую ошибку      ║
╠═════════════════════════════════════════════════════════════════╣
║ Пасхалки и развлечения                                          ║
╠════════════════════════════╦════════════════════════════════════╣
║ hi/hello/привет            │ Приветствие и звук                 ║
║ nyan                       │ Воспроизвести nyanpasu             ║
║ rickroll/rickrol           │ Пасхалка                           ║
║ rps <фигура>               │ Камень-Ножницы-Бумага              ║
╚═════════════════════════════════════════════════════════════════╝";

        private const string RPS_HELP = @"
        ====== КМБ - правила ======
        Выберите одну из фигур:
        • Камень (rock, r, камень, к)
        • Бумага (paper, p, бумага, б)
        • Ножницы (scissors, s, ножницы, н)

        Побеждает:
        - Камень ломает ножницы
        - Ножницы режут бумагу
        - Бумага накрывает камень

        Пример: rps rock
        ";

        private const int HWND_BOTTOM = 1;

        private const uint SWP_NOMOVE = 0x0002;

        // Под всеми
        private const uint SWP_NOSIZE = 0x0001;

        private const uint SWP_SHOWWINDOW = 0x0040;

        // ==== МЕТРИКИ =====
        private static readonly PerformanceCounter CpuCounter = new("Processor", "% Processor Time", "_Total");

        private static readonly PerformanceCounter RamCounter = new("Memory", "Available MBytes");
        //для измерения сети
        static float LastBytesRecived = 0;
        static float LastBytesSent = 0;

        // Для измерения дисковой активности
        private static DateTime _lastDiskTime = DateTime.Now;

        private static long _lastReadBytes = 0;

        private static long _lastWriteBytes = 0;

        _Debug Debug;

        private string json;

        private List<QuoteItem> quotes;

        private Random random = new Random((int)DateTime.Now.Ticks);

        private string randomQuote;

        private WeatherService weatherService = new WeatherService(new HttpClient());
        
        public RssParser rssParser;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += OnSourceInitialized;


            Debug = new(Output4);


            Dictionary<string, string> SysInfo = GetSystemInfo();
            foreach (string key in SysInfo.Keys)
            {
                Output0.Text += (key + ": ").PadRight(20) + SysInfo[key] + "\n";
            }


            //Изначальные строки ЦИТАТЫ
            json = File.ReadAllText("quotes.json");
            // Десериализация JSON в список объектов QuoteItem
            quotes = JsonSerializer.Deserialize<List<QuoteItem>>(json);

            Output3.Text = quotes[random.Next(quotes.Count)].quote;

            // ===== ФОНОВАЯ ЗАДАЧА =====

            rssParser = new RssParser();
            Task.Run(async () =>
            {
                var dtf = await rssParser.GetRandomDtfPostAsync();
                var shiki = await rssParser.GetRandomShikimoriPostAsync();
                // так как мы не в UI-потоке — если надо что-то сделать с UI:
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var DtfPost = rssParser.GetRandomDtfPostAsync();
                    RTBdtf.Document.Blocks.Clear();

                    var title = new Paragraph(new Run(dtf.title)) { FontSize = 14 };
                    var content = new Paragraph(new Run(rssParser.TrimWithEllipsis(rssParser.CleanDescription(dtf.description), 150))) { FontSize = 9 };
                    RTBdtf.Document.Blocks.Add(title); RTBdtf.Document.Blocks.Add(content);


                    var shikiPost = rssParser.GetRandomShikimoriPostAsync();
                    //RTBshiki.Document.Blocks.Clear();

                    title = new Paragraph(new Run(shiki.title)) { FontSize = 14 };
                    content = new Paragraph(new Run(rssParser.TrimWithEllipsis(rssParser.CleanDescription(shiki.description), 150))) { FontSize = 9 };
                    RTBdtf.Document.Blocks.Add(title); RTBdtf.Document.Blocks.Add(content);
                });
            });

            var clock = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            clock.Tick += (sender, e) =>
            {
                Output1_1.Text = $"{DateTime.Now:HH:mm}";
                Output1_2.Text = $"{DateTime.Now:D}";
            };
            clock.Start();

            var quoteUpdater = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(900)
            };
            quoteUpdater.Tick += (sender, e) =>
            {
                // Выбор случайной цитаты
                updateQuote();
            };
            quoteUpdater.Start();

            // ====== ЗАМЕР ПРОИЗВОДА =====
            var usageUpdater = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            usageUpdater.Tick += (sender, e) =>
                       {
                           var metrics = GetSystemLoad();
                           float cpu = metrics["CPU"];
                           float mem = metrics["Memory"];
                           float rec = metrics["NetworkReceivedKBps"]; // Convert to MB/s
                           float sen = metrics["NetworkSentKBps"]; // Convert to MB/s
                           float dread = metrics["DiskReadKBps"];
                           float dwrite = metrics["DiskWriteKBps"];

                           if (cpu < 1f) Output2_1.Text = "<1%\n";
                           else Output2_1.Text = (Math.Floor(cpu) + "%\n");
                           Output2_2.Text = ("|" + new string('=', (int)Math.Floor(cpu) / 10) + new string(' ', 10 - ((int)Math.Floor(cpu) / 10)) + "|\n");

                           Output2_1.Text += (Math.Floor(mem) + "%\n");
                           Output2_2.Text += ("|" + new string('=', (int)Math.Floor(mem) / 10) + new string(' ', 10 - ((int)Math.Floor(mem) / 10)) + "|\n");

                           Output2_1.Text += $"{Math.Abs(rec):F1}mb/s\n";
                           Output2_1.Text += $"{Math.Abs(sen):F1}mb/s\n";
                       };
            usageUpdater.Start();

            // ====== ПОЛУЧЕНИЕ ПОГОДЫ =====
            var weatherUpdater = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(21000) // Интервал обновления (6 часов)
            };

            // Метод для обновления погоды

            // Обработчик события Tick для таймера
            weatherUpdater.Tick += async (sender, e) =>
            {
                await UpdateWeatherAsync();
            };

            // Запускаем таймер для последующих обновлений
            weatherUpdater.Start();


        }

        // ====== СИСТЕМНАЯ ИНФА =======
        public static Dictionary<string, string> GetSystemInfo()
        {
            var info = new Dictionary<string, string>();

            // Операционная система
            using (var osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (var os in osSearcher.Get())
                {
                    info["OS"] = os["Caption"]?.ToString().Trim();
                    info["OS Architecture"] = os["OSArchitecture"]?.ToString();
                    info["Version"] = os["Version"]?.ToString();
                    info["Build"] = os["BuildNumber"]?.ToString();
                }
            }

            // Компьютер
            using (var computerSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (var comp in computerSearcher.Get())
                {
                    info["Manufacturer"] = comp["Manufacturer"]?.ToString();
                    info["Model"] = comp["Model"]?.ToString();
                    info["Computer Name"] = comp["Name"]?.ToString();
                    info["RAM"] = $"{Math.Round(Convert.ToDouble(comp["TotalPhysicalMemory"]) / (1024 * 1024 * 1024), 2)} GB";
                }
            }

            // Процессор
            using (var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var cpu in cpuSearcher.Get())
                {
                    info["CPU"] = cpu["Name"]?.ToString();
                    info["Cores"] = cpu["NumberOfCores"]?.ToString();
                    info["Logical Processors"] = cpu["NumberOfLogicalProcessors"]?.ToString();
                }
            }

            // Видеокарта
            using (var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var gpu in gpuSearcher.Get())
                {
                    info["GPU"] = gpu["Name"]?.ToString();
                }
            }

            // Имя пользователя
            info["User"] = Environment.UserName;

            // Домен / рабочая группа
            info["Domain"] = Environment.UserDomainName;

            // Версия .NET
            info[".NET Version"] = Environment.Version.ToString();

            // Тип системы
            info["Platform"] = Environment.OSVersion.Platform.ToString();
            info["64-bit OS"] = Environment.Is64BitOperatingSystem ? "Yes" : "No";

            return info;
        }

        public static Dictionary<string, float> GetSystemLoad()
        {
            var result = new Dictionary<string, float>();

            try
            {
                // Процессор (%)
                result["CPU"] = NextCpuValue();

                // Память (%) - использование относительно общей
                long totalMemoryMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
                long availableMemoryMb = (long)RamCounter.NextValue();
                float memoryUsagePercent = (float)((totalMemoryMb - availableMemoryMb) * 100.0 / totalMemoryMb);
                result["Memory"] = memoryUsagePercent;

                // Сеть (KB/s)
                var networkUsage = GetNetworkUsage();
                result["NetworkReceivedKBps"] = networkUsage.ReceivedKbPerSecond / 1024f;
                result["NetworkSentKBps"] = networkUsage.SentKbPerSecond / 1024f;

                // Диск (чтение/запись в KB/s)
                var diskUsage = GetDiskUsage();
                result["DiskReadKBps"] = diskUsage.ReadKbPerSecond;
                result["DiskWriteKBps"] = diskUsage.WriteKbPerSecond;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении метрик: {ex.Message}");
            }

            return result;
        }

        private static (float ReadKbPerSecond, float WriteKbPerSecond) GetDiskUsage()
        {
            // Пример простого подсчёта IO за период
            var now = DateTime.Now;
            var timeDiff = (now - _lastDiskTime).TotalSeconds;
            if (timeDiff < 0.1) timeDiff = 0.1; // Избегаем деления на 0

            long currentRead = 0;
            long currentWrite = 0;

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    // Здесь можно использовать WMI для получения точных значений чтения/записи
                    // А пока заглушка:
                    currentRead += 0; // Заменить на реальные значения при использовании WMI
                    currentWrite += 0;
                }
            }

            float readSpeed = (currentRead - _lastReadBytes) / 1024.0f / (float)timeDiff;
            float writeSpeed = (currentWrite - _lastWriteBytes) / 1024.0f / (float)timeDiff;

            _lastReadBytes = currentRead;
            _lastWriteBytes = currentWrite;
            _lastDiskTime = now;

            return (readSpeed, writeSpeed);
        }

        private static (float ReceivedKbPerSecond, float SentKbPerSecond) GetNetworkUsage()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            double received = 0, sent = 0;

            foreach (var adapter in interfaces)
            {
                IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                received += stats.BytesReceived;
                sent += stats.BytesSent;
            }

            // Calculate KB/s by dividing total bytes by 1024 and elapsed seconds
            double elapsedSeconds = 1; // Assuming this method is called every second
            float receivedKbPerSecond = (float)((received-LastBytesRecived) / 1024.0 / elapsedSeconds);
            float sentKbPerSecond = (float)((sent - LastBytesSent) / 1024.0 / elapsedSeconds);

            LastBytesRecived = (float)received;
            LastBytesSent = (float)sent;

            return (receivedKbPerSecond, sentKbPerSecond);
        }

        private static float NextCpuValue()
        {
            float usage = CpuCounter.NextValue();
            System.Threading.Thread.Sleep(100); // Первое значение может быть некорректным
            return CpuCounter.NextValue();
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                List<string> divider = new();
                List<string> args = new();
                List<string> postArgs = new();
                string command = InputBox.Text.Trim();

                if (command.Contains('|'))
                {
                    divider = command.Split('|').ToList();
                    // Используем RemoveEmptyEntries для корректного разделения
                    args = divider[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                    postArgs = divider.Count > 1
                        ? divider[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>();
                }
                else
                {
                    args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                string style = "default"; // По умолчанию стиль - "default"
                InputBox.Clear();


                if (postArgs != null && postArgs.Contains("lolcat"))
                {
                    style = "lolcat";
                    // Не удаляем последний элемент из args, иначе команда исчезает
                }

                if (args.Count == 0)
                {
                    Debug.Log("Пустая команда. Введите 'help' для справки.\n");
                    return;
                }

                // ===== КОМАНДЫ ======
                // TODO: сделать Класс и метод Console.log и т.п. и перенести все сообщения консоли на них + lolcat

                switch (args[0].ToLower())
                {
                    // --- Справка и информация ---
                    case "colorhelp":
                        Debug.Write(
                            "Примеры цветов:\n" +
                            "• Red        | Green\n" +
                            "• Blue       | Yellow\n" +
                            "• Orange     | Purple\n" +
                            "• Pink       | Brown\n" +
                            "• Black      | White\n" +
                            "• Gray       | LightGray\n" +
                            "• DarkGray   | Cyan\n" +
                            "• Magenta    | Lime\n" +
                            "• Navy       | Teal\n" +
                            "• Olive      | Maroon\n" +
                            "• Aqua       | Fuchsia\n" +
                            "• Silver     | Gold\n" +
                            "• Beige      | Coral\n" +
                            "• Crimson    | Indigo\n" +
                            "• Ivory      | Khaki\n" +
                            "• Lavender   | Salmon\n" +
                            "• Sienna     | Tan\n" +
                            "• Tomato     | Violet\n" +
                            "• Wheat      | #RRGGBB (например, #FF44FF)\n",
                            style
                        );
                        break;
                    case "help":
                        Debug.Write(HELP_TEXT + "\n", style);
                        break;
                    case "newquote":
                        Debug.Log("Выбрана новая цитата.");
                        updateQuote();
                        break;
                    case "posts":
                        Debug.Log("Обновляю посты...");
                        Task.Run(async () =>
                        {
                            var dtf = await rssParser.GetRandomDtfPostAsync();
                            var shiki = await rssParser.GetRandomShikimoriPostAsync();
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                RTBdtf.Document.Blocks.Clear();
                                var title = new Paragraph(new Run(dtf.title)) { FontSize = 14 };
                                var content = new Paragraph(new Run(rssParser.TrimWithEllipsis(rssParser.CleanDescription(dtf.description), 150))) { FontSize = 9 };
                                RTBdtf.Document.Blocks.Add(title); RTBdtf.Document.Blocks.Add(content);
                                title = new Paragraph(new Run(shiki.title)) { FontSize = 14 };
                                content = new Paragraph(new Run(rssParser.TrimWithEllipsis(rssParser.CleanDescription(shiki.description), 150))) { FontSize = 9 };
                                RTBdtf.Document.Blocks.Add(title); RTBdtf.Document.Blocks.Add(content);
                            });
                        });
                        break;
                    case "time":
                        Debug.Write($"Текущее время: {DateTime.Now}\n", style);
                        break;

                    // --- Управление интерфейсом ---
                    case "clear":
                        Debug.Write("Экран очищен");
                        Debug.clear();
                        break;
                    case "setcolor":
                        try
                        {
                            Output0.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output1_1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output1_2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output2_0.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output2_1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output2_2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output3.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Output4.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            OutputW0.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            OutputW1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            OutputW2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Debug.setColor(args[1]);
                            Debug.Write($"Цвет установлен на {Output4.Foreground}");
                        }
                        catch (Exception ex)
                        {
                            Debug.Error(ex.Message);
                            Debug.Write("Видимо, " + args[1] + " не является подходящим цветом. Пример правильного: #FF44FF или red, blue.");
                        }
                        break;
                    case "setback":
                        try
                        {
                            MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(args[1]));
                            Debug.Write($"Фоновый цвет установлен на {args[1]}.");
                        }
                        catch (Exception ex)
                        {
                            Debug.Error(ex.Message);
                            Debug.Write("Видимо, " + args[1] + " не является подходящим цветом. Пример правильного: #FF44FF или red, blue.");
                        }
                        break;
                    case "bringmeback":
                        this.Height = System.Windows.SystemParameters.PrimaryScreenHeight - int.Parse(args[1]);
                        Debug.Write($"Высота окна установлена на {this.Height}.");
                        break;

                    // --- Системные и служебные ---
                    case "autolaunch":
                        if (AutoLaunch.IsEnabled())
                        {
                            AutoLaunch.Disable();
                            Debug.Write("Автозапуск отключён.\n", style);
                        }
                        else
                        {
                            AutoLaunch.Enable();
                            Debug.Write("Автозапуск включён.\n", style);
                        }
                        break;
                    case "cmd":
                        if (args.Count > 1)
                        {
                            string cmd = string.Join(" ", args.Skip(1));
                            Debug.Write($"Передаю \"{cmd}\" в cmd");
                            Debug.Write(Debug.useCmd(cmd), style);
                        }
                        else
                        {
                            Debug.Error("Вы не ввели команду.\n");
                        }
                        break;
                    case "crush":
                        try
                        {
                            Debug.Write("Генерация тестовой ошибки...",style);
                            throw new Exception("Crush");
                        }
                        catch (Exception ex)
                        {
                            Debug.Error($"Ошибка: {ex.Message}\n");
                        }
                        break;
                    case "close":
                        Debug.Write("Закрытие программы...\n", style);
                        System.Windows.Application.Current.Shutdown();
                        break;

                    // --- Пасхалки и развлечения ---
                    case "hi":
                    case "hello":
                    case "привет":
                        List<string> sounds = new List<string> { "BBHello", "hellou", "HLHello", "ObiWanHello" };
                        PlaySound("sounds/" + sounds[new Random((int)DateTime.Now.Ticks).Next(sounds.Count())] + ".mp3");
                        Debug.Write("Привет!\n", style);
                        break;
                    case "nyan":
                        Debug.Write("Ня!");
                        PlaySound("sounds/nyanpasu.mp3");
                        break;
                    case "rickroll":
                    case "rickrol":
                        Debug.Write("He gave you up...\n", style);
                        break;
                    case "rps":
                        if (args.Count > 1)
                        {
                            List<string> figures = new List<string>() { "Камень", "Бумага", "Ножницы" };
                            int player = -1;
                            int cpu = random.Next(3);
                            Debug.Write("Я выбираю " + figures[cpu], style);
                            switch (args[1])
                            {
                                case "rock":
                                case "r":
                                case "камень":
                                case "к":
                                    player = 0;
                                    break;
                                case "paper":
                                case "p":
                                case "бумага":
                                case "б":
                                    player = 1;
                                    break;
                                case "scissors":
                                case "s":
                                case "ножницы":
                                case "н":
                                    player = 2;
                                    break;
                                default:
                                    Debug.Error("Неизвестная фигура. Введите 'rps' для справки.\n");
                                    break;
                            }
                            int result = ((player - cpu + 3) % 3);
                            if (player != 0 && player != 1 && player != 2)
                            {
                                break;
                            }
                            switch (result)
                            {
                                case 0:
                                    Debug.Write("Ничья!", style);
                                    break;
                                case 1:
                                    Debug.Write("Ты победил!", style);
                                    break;
                                case 2:
                                    Debug.Write("Ты проиграл!", style);
                                    break;
                            }
                        }
                        else
                        {
                            Debug.Write(RPS_HELP, style);
                        }
                        break;
                    // --- Неизвестная команда ---
                    default:
                        Debug.Error($"Команда '{args[0]}' не найдена. Введите 'help' для справки.\n");
                        break;
                }
                

                style = "default";
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //MediaInfoProvider mediaInfoProvider = new MediaInfoProvider();
            //Dictionary<string, string> AudioInfo = mediaInfoProvider.GetCurrentMediaInfoAsync().Result;
            //BackgroundTask(AudioInfo);

            InputBox.Focus();

            // Сразу запускаем обновление погоды при старте программы
            await UpdateWeatherAsync();
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, (IntPtr)HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void updateQuote()
        {
            randomQuote = quotes[random.Next(quotes.Count)].quote;
            Output3.Text = quotes[random.Next(quotes.Count)].quote;
        }

        private async Task PlaySound(string path)
        {
            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            var player = new MediaPlayer();

            player.Open(uri);
            player.Play();
        }

        private async Task UpdateWeatherAsync()
        {
            // Выводим сообщение о начале обновления
            Debug.Log("Получение данных о погоде...\n");

            // Получаем данные о погоде
            var weatherData = await weatherService.GetWeatherDataAsync(55.7558f, 37.6173f); // Москва
            if (weatherData.ContainsKey("error"))
            {
                Debug.Error($"Ошибка получения данных: {weatherData["error"]}\n");
                Debug.Log($"Ссылка: {weatherData["link"]}\n");
                return;
            }

            //TODO: Обновляем интерфейс
            OutputW0.Text = $"{weatherService.ConvertWeatherCodeToDesc((int)weatherData["weather_code"])}";
            OutputW1.Text = $"🌡 {weatherData["temperature"]}°C\n🤲 {weatherData["apparent_temperature"]:F1}°C";
            OutputW2.Text = $"💨 {weatherData["windspeed"]} м/с\n{weatherData["wind_direction"]}";
        }

        public class QuoteItem()
        {
            public string quote { get; set; }
        }
    }

    // ===== ЗЛОЕБУЧАЯ НЕРАБОЧАЯ ЗАЛУПА ====
    public class MediaInfoProvider
    {
        private MediaManager mediaManager;

        public MediaInfoProvider()
        {
            mediaManager = new MediaManager();
            mediaManager.Start();
        }

        public async Task<Dictionary<string, string>> GetCurrentMediaInfoAsync()
        {
            var result = new Dictionary<string, string>();

            var session = mediaManager.GetFocusedSession();
            if (session == null || session.ControlSession == null)
            {
                result["Status"] = "NONE";
                result["Title"] = "NONE";
                result["Artist"] = "NONE";
                result["Duration"] = "NONE";
                result["Position"] = "NONE";
                return result;
            }

            var mediaProps = await session.ControlSession.TryGetMediaPropertiesAsync();
            var playbackInfo = session.ControlSession.GetPlaybackInfo();
            var timeline = playbackInfo;

            result["Title"] = !string.IsNullOrWhiteSpace(mediaProps?.Title) ? mediaProps.Title : "UNK";
            result["Artist"] = !string.IsNullOrWhiteSpace(mediaProps?.Artist) ? mediaProps.Artist : "UNK";
            result["Status"] = playbackInfo?.PlaybackStatus.ToString() ?? "UNK";

            result["Duration"] = "NA";
            result["Position"] = "NA";

            return result;
        }

        public void Stop()
        {
            mediaManager.Dispose();
        }
    }

    // ==== АВТОЗАПУСК ====
    public static class AutoLaunch
    {
        private const string AppName = "MyWpfApp";
        private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Добавляет приложение в автозагрузку
        /// </summary>
        public static void Enable()
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegPath, true))
            {
                reg.SetValue(AppName, exePath);
            }
        }

        /// <summary>
        /// Удаляет приложение из автозагрузки
        /// </summary>
        public static void Disable()
        {
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegPath, true))
            {
                if (reg.GetValue(AppName) != null)
                    reg.DeleteValue(AppName);
            }
        }

        /// <summary>
        /// Проверяет, включён ли автозапуск
        /// </summary>
        public static bool IsEnabled()
        {
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegPath, false))
            {
                return reg.GetValue(AppName) != null;
            }
        }
    }

    //TODO: Нарисовать и интегрировать иконки
    // ===== ПОЛУЧЕНИЕ ПОГОДЫ =====
    public class WeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, object>> GetWeatherDataAsync(double latitude, double longitude)
        {
            var weatherData = new Dictionary<string, object>();

            try
            {
                string _latidude = latitude.ToString().Split(',')[0] + '.' + latitude.ToString().Split(',')[1];
                string _longitude = longitude.ToString().Split(',')[0] + '.' + longitude.ToString().Split(',')[1];
                // Запрос данных от open-meteo.com
                var response = await _httpClient.GetStringAsync(
                    $"https://api.open-meteo.com/v1/forecast?latitude=" + _latidude + "&longitude=" + _longitude + "&current_weather=true");

                var jsonDocument = JsonDocument.Parse(response);
                var currentWeather = jsonDocument.RootElement.GetProperty("current_weather");

                // Получаем основные данные
                double temperature = currentWeather.GetProperty("temperature").GetDouble();
                double windspeed = currentWeather.GetProperty("windspeed").GetDouble();
                double winddirection = currentWeather.GetProperty("winddirection").GetDouble();
                int weathercode = currentWeather.GetProperty("weathercode").GetInt32();

                // Конвертируем код погоды в текстовый id

                // Рассчитываем "ощущаемую" температуру (упрощённая формула)
                double apparentTemperature = CalculateApparentTemperature(temperature, windspeed);

                // Получаем направление ветра в виде стрелочки
                string windDirectionArrow = GetWindDirectionArrow(winddirection);

                // Заполняем словарь
                weatherData.Add("weather_code", weathercode);
                weatherData.Add("temperature", temperature);
                weatherData.Add("apparent_temperature", apparentTemperature);
                weatherData.Add("windspeed", windspeed);
                weatherData.Add("wind_direction", windDirectionArrow);
            }
            catch (Exception ex)
            {
                weatherData.Add("error", ex.Message);
                weatherData.Add("link", $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true");
            }

            return weatherData;
        }

        private double CalculateApparentTemperature(double temperature, double windspeed)
        {
            // Упрощённая формула для ощущаемой температуры (wind chill)
            if (temperature >= 10 || windspeed <= 3)
                return temperature;

            return 13.12 + 0.6215 * temperature - 11.37 * Math.Pow(windspeed, 0.16)
                   + 0.3965 * temperature * Math.Pow(windspeed, 0.16);
        }

        public string ConvertWeatherCodeToDesc(int weatherCode)
        {
            // Конвертация кодов погоды WMO в текстовые идентификаторы
            return weatherCode switch
            {
                0 => "Ясно",
                1 => "Преимущественно ясно",
                2 => "Облачно с прояснениями",
                3 => "Пасмурно",
                45 => "Туман",
                48 => "Туман с инеем",
                51 => "Лёгкая морось",
                53 => "Морось",
                55 => "Сильная морось",
                56 => "Лёгкий ледяной дождь",
                57 => "Сильный ледяной дождь",
                61 => "Лёгкий дождь",
                63 => "Дождь",
                65 => "Сильный дождь",
                66 => "Лёгкий ледяной дождь",
                67 => "Сильный ледяной дождь",
                71 => "Лёгкий снег",
                73 => "Снег",
                75 => "Сильный снег",
                77 => "Град",
                80 => "Кратковременный дождь",
                81 => "Умеренные ливни",
                82 => "Сильные ливни",
                85 => "Слабый снежный ливень",
                86 => "Сильный снежный ливень",
                95 => "Гроза",
                96 => "Гроза с градом",
                99 => "Гроза с крупным градом (ух!",
                _ => "unknown"
            };
        }
        private string GetWindDirectionArrow(double degrees)
        {
            // Конвертация градусов в направление стрелочки
            return degrees switch
            {
                >= 337.5 or < 22.5 => "↓",   // Север
                >= 22.5 and < 67.5 => "↙",   // Северо-восток
                >= 67.5 and < 112.5 => "←",   // Восток
                >= 112.5 and < 157.5 => "↖",  // Юго-восток
                >= 157.5 and < 202.5 => "↑",  // Юг
                >= 202.5 and < 247.5 => "↗",  // Юго-запад
                >= 247.5 and < 292.5 => "→",  // Запад
                >= 292.5 and < 337.5 => "↘", // Северо-запад
                _ => "?"
            };
        }
    }
    // ===== DTF и shikimori =====
    public class RssPost()
    {
        public string title { get; set; }
        public string description { get; set; }
        public string link { get; set; }
        public string? error { get; set; }
    }
    public class RssParser
    {
        private readonly HttpClient _httpClient = new HttpClient();

        

        private async Task<RssPost> GetRandomPostAsync(string feedUrl, string source)
        {
            try
            {
                using var stream = await _httpClient.GetStreamAsync(feedUrl);
                using var reader = XmlReader.Create(stream);
                var feed = SyndicationFeed.Load(reader);

                if (feed == null || !feed.Items.Any())
                    return new RssPost() {error = $"⚠️ Нет записей в RSS-ленте {source}."};

                var items = feed.Items.ToList();
                var random = new Random();
                var post = items[random.Next(items.Count)];

                return new RssPost() { 
                    title = post.Title.Text, 
                    description = post.Summary.Text, 
                    link = post.Links.FirstOrDefault().Uri.ToString()};
            }
            catch (Exception ex)
            {
                new RssPost() { error = $"❌ Ошибка при загрузке {source}: {ex.Message}" };
            }
            return new RssPost();
        }

        public Task<RssPost> GetRandomDtfPostAsync()
        {
            return GetRandomPostAsync("https://dtf.ru/rss/all", "DTF");
        }

        public Task<RssPost> GetRandomShikimoriPostAsync()
        {
            return GetRandomPostAsync("https://shikimori.one/forum/news.rss", "Shikimori");
        }

        public string CleanDescription(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Удалим все quote-блоки, если не нужны
            var quotes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'b-quote')]");
            if (quotes != null)
            {
                foreach (var quote in quotes)
                    quote.Remove();
            }

            // Оставим только InnerText (без всех тегов)
            return doc.DocumentNode.InnerText;
        }
        public string TrimWithEllipsis(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength).TrimEnd() + "…";
        }
    }
}