using VeraCom.Models;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PcanSqliteSender.Services
{
    public class PcanService
    {
        private ushort _handle = PCANBasic.PCAN_USBBUS1;
        private MultimediaTimer _timer = new();
        private List<CanMessage> _messages = new();
        private long _tickCounter = 0;

        public bool IsRunning { get; private set; }

        public event Action<CanMessage> MessageSent;
        public event Action<CanMessage> MessageReceived; // ✅ neu

        private Thread _receiveThread;
        private bool _receiveRunning = false;

        public void Start(IEnumerable<CanMessage> messages)
        {
            var res = PCANBasic.Initialize(_handle, TPCANBaudrate.PCAN_BAUD_500K);
            if (res != TPCANStatus.PCAN_ERROR_OK)
                throw new Exception("PCAN-Init fehlgeschlagen");

            _messages = messages.ToList();
            _tickCounter = 0;

            _timer.Tick += OnTimerTick;
            _timer.Start(1);

            // Empfangs-Thread starten
            _receiveRunning = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();

            IsRunning = true;
        }

        private void OnTimerTick()
        {
            _tickCounter++;
            foreach (var msg in _messages)
            {
                if (msg.CycleTimeMs > 0 && _tickCounter % msg.CycleTimeMs == 0)
                {
                    Send(msg);
                }
            }
        }

        private void Send(CanMessage task)
        {
            TPCANMsg msg = new TPCANMsg
            {
                ID = task.CanID,
                LEN = task.DLC,
                DATA = new byte[8],
                MSGTYPE = TPCANMessageType.PCAN_MESSAGE_STANDARD
            };
            Array.Copy(task.Payload, msg.DATA, Math.Min(task.DLC, (byte)8));

            if(msg.ID == 1345)
            {
                UInt64 speed = 30; // 110 km/h
                speed *= 10;
                msg.DATA[2] = Convert.ToByte(speed>>8);
                msg.DATA[3] = Convert.ToByte(speed&0xFF);
            }

            if (PCANBasic.Write(_handle, ref msg) == TPCANStatus.PCAN_ERROR_OK)
            {
                MessageSent?.Invoke(task);
            }
        }

        private void ReceiveLoop()
        {
            while (_receiveRunning)
            {
                TPCANMsg msg;
                TPCANTimestamp timestamp;
                var res = PCANBasic.Read(_handle, out msg, out timestamp);
                if (res == TPCANStatus.PCAN_ERROR_OK)
                {
                    var canMsg = new CanMessage
                    {
                        CanID = msg.ID,
                        DLC = msg.LEN,
                        Payload = msg.DATA,
                        Timestamp = DateTime.Now
                    };
                    MessageReceived?.Invoke(canMsg); // Event feuern
                }
                Thread.Sleep(1); // kleine Pause
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;

            _receiveRunning = false;
            _receiveThread?.Join();

            PCANBasic.Uninitialize(_handle);
            IsRunning = false;
        }
    }
}