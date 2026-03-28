using VeraCom.Models;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using VeraCom.Models;

namespace PcanSqliteSender.Services;

public class PcanService
{
    private ushort _handle = PCANBasic.PCAN_USBBUS1;

    //.Drivers.TPCANHandle.PCAN_USBBUS1;
    private MultimediaTimer _timer = new();
    private List<CanMessage> _messages = new();
    private long _tickCounter = 0;

    public bool IsRunning { get; private set; }

    // ✅ EVENT
    public event Action<CanMessage> MessageSent;

    public void Start(IEnumerable<CanMessage> messages)
    {
        var res = PCANBasic.Initialize(_handle, TPCANBaudrate.PCAN_BAUD_500K);
        if (res != TPCANStatus.PCAN_ERROR_OK) throw new Exception("PCAN-Init fehlgeschlagen");

        _messages = messages.ToList();
        _tickCounter = 0;
        _timer.Tick += OnTimerTick;
        _timer.Start(1); // 1ms Auflösung
        IsRunning = true;
    }

    private void OnTimerTick()
    {
        _tickCounter++;
        foreach (var msg in _messages)
        {
            if (_tickCounter % msg.CycleTimeMs == 0)
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

        var result = PCANBasic.Write(_handle, ref msg);

        if (result == TPCANStatus.PCAN_ERROR_OK)
        {
            // 👉 Event feuern (KEIN UI Zugriff hier!)
            MessageSent?.Invoke(task);
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        PCANBasic.Uninitialize(_handle);
        IsRunning = false;
    }
}