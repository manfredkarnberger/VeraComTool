using System;
using System.ComponentModel;

namespace VeraCom.Models
{
    public class CanMessage : INotifyPropertyChanged
    {
        public uint CanID { get; set; }
        public byte DLC { get; set; }
        public byte[] Payload { get; set; }
        public int CycleTimeMs { get; set; }
        public long LastTickCount { get; set; } // Hilfsvariable für das Timing

        private int _txFrameCounter;
        public int TxFrameCounter
        {
            get => _txFrameCounter;
            set
            {
                _txFrameCounter = value;
                OnPropertyChanged(nameof(TxFrameCounter));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
