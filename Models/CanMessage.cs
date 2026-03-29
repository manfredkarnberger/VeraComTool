using System;
using System.ComponentModel;

namespace VeraCom.Models
{
    public class CanMessage : INotifyPropertyChanged
    {
        public uint CanID { get; set; }
        public byte DLC { get; set; }
        public byte[] Payload { get; set; }

        // 🔄 Senden
        public int CycleTimeMs { get; set; }

        private int _txFrameCounter;
        public int TxFrameCounter
        {
            get => _txFrameCounter;
            set { _txFrameCounter = value; OnPropertyChanged(nameof(TxFrameCounter)); }
        }

        // 📥 Empfang
        public DateTime Timestamp { get; set; }

        private double _rxCycleTime;
        public double RxCycleTime
        {
            get => _rxCycleTime;
            set { _rxCycleTime = value; OnPropertyChanged(nameof(RxCycleTime)); }
        }

        public DateTime LastTimestamp { get; set; }

        // Payload als Hex
        public string PayloadString => Payload != null ? BitConverter.ToString(Payload) : "";

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // 👉 Für UI Refresh
        public void Refresh()
        {
            OnPropertyChanged(nameof(DLC));
            OnPropertyChanged(nameof(PayloadString));
            OnPropertyChanged(nameof(Timestamp));
            OnPropertyChanged(nameof(RxCycleTime));
        }
    }
}