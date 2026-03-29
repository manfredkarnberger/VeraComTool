using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using PcanSqliteSender.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using VeraCom.Models;
using VeraComTool.Properties;

namespace VeraCom
{
    public partial class MainWindow : Window
    {
        private PcanService _pcanService = new();
        private DatabaseService _dbService = new();

        // ObservableCollection für DataGrid Binding
        public ObservableCollection<CanMessage> Messages { get; set; } = new();
        public ObservableCollection<CanMessage> ReceivedMessages { get; set; } = new();
        
        private Dictionary<uint, CanMessage> _receivedMap = new();

        private string currentDbPath = "";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _pcanService.MessageSent += OnMessageSent;
            _pcanService.MessageReceived += OnMessageReceived; // ✅

            LoadLastDatabase();
        }

        private void LoadLastDatabase()
        {
            string lastPath = Settings.Default.LastDatabasePath;

            if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={lastPath}");
                    connection.Open();

                    currentDbPath = lastPath;
                    TxtDatabasePath.Text = lastPath;

                    BtnStart.IsEnabled = true;
                    BtnStop.IsEnabled = true;

                    // Nachrichten laden und ObservableCollection füllen
                    var loadedMessages = _dbService.LoadMessages(currentDbPath);
                    Messages.Clear();
                    foreach (var msg in loadedMessages)
                        Messages.Add(msg);
                }
                catch
                {
                    TxtDatabasePath.Text = "";
                    BtnStart.IsEnabled = false;
                    BtnStop.IsEnabled = false;
                }
            }
        }

        // ✅ Event Handler für MessageSent
        private void OnMessageSent(CanMessage msg)
        {
            // UI-Thread sicherstellen
            Dispatcher.Invoke(() =>
            {
                // TxFrameCounter erhöhen → DataGrid aktualisiert automatisch
                msg.TxFrameCounter++;
            });
        }

        private void OnMessageReceived(CanMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (_receivedMap.ContainsKey(msg.CanID))
                {
                    var existing = _receivedMap[msg.CanID];

                    var now = DateTime.Now;

                    // 🔥 Zykluszeit berechnen
                    if (existing.LastTimestamp != default)
                    {
                        existing.RxCycleTime = Math.Round((now - existing.LastTimestamp).TotalMilliseconds);
                    }

                    existing.LastTimestamp = now;

                    // Daten aktualisieren
                    existing.DLC = msg.DLC;
                    existing.Payload = msg.Payload;
                    existing.Timestamp = now;

                    existing.Refresh();
                }
                else
                {
                    msg.Timestamp = DateTime.Now;
                    msg.LastTimestamp = msg.Timestamp;
                    msg.RxCycleTime = 0;

                    _receivedMap[msg.CanID] = msg;
                    ReceivedMessages.Add(msg);

                    // Sortieren nach CAN-ID
                    var sorted = ReceivedMessages.OrderBy(m => m.CanID).ToList();
                    ReceivedMessages.Clear();
                    foreach (var m in sorted)
                        ReceivedMessages.Add(m);
                }
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentDbPath) || !File.Exists(currentDbPath))
            {
                MessageBox.Show("Datenbank existiert nicht!");
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = false;
                return;
            }

            try
            {
                // ⚠ Start mit ObservableCollection, KEIN ToList()!
                _pcanService.Start(Messages);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _pcanService.Stop();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _pcanService.Stop();

            if (!string.IsNullOrEmpty(currentDbPath))
            {
                Settings.Default.LastDatabasePath = currentDbPath;
                Settings.Default.Save();
            }

            base.OnClosing(e);
        }

        private void DateiOeffnen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "SQLite-Datenbank öffnen",
                Filter = "SQLite Dateien (*.db;*.sqlite)|*.db;*.sqlite|Alle Dateien (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string selectedPath = ofd.FileName;

                if (!File.Exists(selectedPath))
                {
                    MessageBox.Show("Die ausgewählte Datei existiert nicht.");
                    return;
                }

                try
                {
                    currentDbPath = selectedPath;

                    // Nachrichten laden
                    var loadedMessages = _dbService.LoadMessages(currentDbPath);
                    Messages.Clear();
                    foreach (var msg in loadedMessages)
                        Messages.Add(msg);

                    Settings.Default.LastDatabasePath = currentDbPath;
                    Settings.Default.Save();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Fehler beim Laden der Datenbank: " + ex.Message);
                    return;
                }

                TxtDatabasePath.Text = currentDbPath;
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = true;
            }
        }

        private void Beenden_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}