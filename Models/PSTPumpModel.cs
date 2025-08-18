using System;
using System.Timers;
using PSTARV2MonitoringApp.Services;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// ���� �߿��� ������ ������ PSTAR ���� ��
    /// </summary>
    public class PSTPumpModel : IDisposable
    {
        #region ���� ���� (FW�� ������ ������ ���)
        // PSTAR ���� ���� ����
        public bool RunStatus { get; private set; } = false;       // 0: STOP, 1: RUN
        public bool HeatStatus { get; private set; } = false;      // 0: HEAT_OFF, 1: HEAT_ON
        public bool ModeStatus { get; private set; } = false;      // 0: MANUAL_MODE, 1: STBY_MODE

        // ��� ������ (CAN �۽�)
        public bool STBY_Start { get; private set; } = false;      // tx_data[0]
        public bool RunLamp { get; private set; } = false;         // tx_data[1]
        public bool Overload { get; private set; } = false;        // tx_data[2]
        public bool RUN_req { get; private set; } = false;         // tx_data[4]
        public bool ResetButton { get; private set; } = false;     // tx_data[5]
        public bool StandByLamp { get; private set; } = false;     // tx_data[6]
        public bool TXLowpress { get; private set; } = false;      // tx_data[7]
        public bool StopLamp { get; private set; } = true;         // ���� ���� ����

        // �Է� ��ȣ
        private bool RunFB_I = false;                              // ���� �ǵ�� �Է�
        private bool RunRemote_I = false;                          // ���� ���� �Է�
        private bool StopRemote_I = false;                         // ���� ���� �Է�
        private bool Overload_I = false;                           // ������ �Է�
        private bool Lowpress_I = false;                           // ���� �Է�

        // �÷��� ����
        private bool Request_Flag = false;                         // ���� ��û �÷���
        private bool STBY_Overload = false;                        // STBY ������ �÷���
        private bool Stop_Overload = false;                        // ���� ������ �÷���
        private bool txLowpress = false;                           // ���� ���� ����
        private bool Error_Flag1 = false;                          // ID 1 ���� �÷���
        private bool Error_Flag2 = false;                          // ID 2 ���� �÷���
        private bool Error_Flag3 = false;                          // ID 3 ���� �÷���
        private bool InitFlag = false;                             // �ʱ�ȭ �÷���
        private bool ComStatus_Flag = false;                       // ���� ���� �÷���

        // Ÿ�̸� ����
        private int CountBuildUpTime_S = 0;                        // BuildUp �ð� ī����
        private int CountParallelTime_S = 0;                       // Parallel �ð� ī����
        private int CountBuildUpStart = 0;                         // BuildUp ���� �÷���
        private int CountParaStart = 0;                            // Parallel ���� �÷���
        private int BuildUpTime = 5;                               // BuildUp �ð� (��)
        private int ParallelTime = 10;                             // Parallel �ð� (��)

        // ���� ����
        private int ComStatus = 0;                                 // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
                                                                   // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

        // ��ġ ID�� CAN ID
        private readonly string _deviceId;
        private readonly uint _canId;

        // Ÿ�̸�
        private readonly Timer _transmitTimer;                     // CAN ���� Ÿ�̸�
        private readonly Timer _logicTimer;                        // ���� ó�� Ÿ�̸�

        // ���� ������ (�ٸ� �����κ���)
        private byte[] rx_data1 = new byte[8];                     // ID 1 ���� ������
        private byte[] rx_data2 = new byte[8];                     // ID 2 ���� ������
        private byte[] rx_data3 = new byte[8];                     // ID 3 ���� ������
        #endregion

        #region �̺�Ʈ
        // CAN ������ ���� �̺�Ʈ
        public event EventHandler<CANTransmitEventArgs> CANDataTransmitted;

        // ���� ���� �̺�Ʈ
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        #endregion

        /// <summary>
        /// ������
        /// </summary>
        public PSTPumpModel(string deviceId)
        {
            _deviceId = deviceId;

            // ��ġ ID�� ���� CAN ID ����
            switch (deviceId)
            {
                case "1": _canId = 0x100; break;
                case "2": _canId = 0x200; break;
                case "3": _canId = 0x300; break;
                default: _canId = 0x100; break;
            }

            // ���� ó�� Ÿ�̸� (100ms �ֱ�)
            _logicTimer = new Timer(100);
            _logicTimer.Elapsed += OnLogicTimerElapsed;
            _logicTimer.AutoReset = true;

            // CAN ���� Ÿ�̸� (300ms �ֱ� - PSTARFW.c�� CanDelay_mS)
            _transmitTimer = new Timer(300);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;
        }

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StartSimulation()
        {
            _logicTimer.Start();
            _transmitTimer.Start();
        }

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StopSimulation()
        {
            _logicTimer.Stop();
            _transmitTimer.Stop();
        }

        /// <summary>
        /// ���� Ÿ�̸� �̺�Ʈ - �߿����� ���� ���� ����
        /// </summary>
        private void OnLogicTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // PSTARFW.c�� ���� ���� ���� ����
            UpdatePressureStatus();
            ProcessInputs();
            RunStopProc(RunStatus);
            RunInput();
            HeatProc(HeatStatus);
            ModeProc(ModeStatus);
            OverloadProc(Overload_I);
            LowpressProc(Lowpress_I);
            ComFailErrorFlag();
            ReceiveRunReq();
            SendRunReq();
            ConnectFunction();
            StandByLampProc(ModeStatus);
            StbyStartAlarm();

            // ���� ���� �˸�
            NotifyStateChanged();
        }

        /// <summary>
        /// CAN ������ ���� Ÿ�̸� �̺�Ʈ
        /// </summary>
        private void OnTransmitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TransmitCANData();
        }

        /// <summary>
        /// CAN ������ ���� ó��
        /// </summary>
        public void ProcessReceivedCANFrame(CANFrame frame)
        {
            // CAN ID�� ���� ������ ���� ���ۿ� ����
            if (frame.Id == 0x100 && _canId != 0x100)
            {
                Array.Copy(frame.Data, rx_data1, Math.Min(frame.Data.Length, 8));
                Error_Flag1 = false;  // COM Fault ī���� ����
            }
            else if (frame.Id == 0x200 && _canId != 0x200)
            {
                Array.Copy(frame.Data, rx_data2, Math.Min(frame.Data.Length, 8));
                Error_Flag2 = false;  // COM Fault ī���� ����
            }
            else if (frame.Id == 0x300 && _canId != 0x300)
            {
                Array.Copy(frame.Data, rx_data3, Math.Min(frame.Data.Length, 8));
                Error_Flag3 = false;  // COM Fault ī���� ����
            }
        }

        /// <summary>
        /// CAN ������ ���� (tx_data)
        /// Ÿ�̸ӿ� ���� �ֱ������� OnTransmitTimerElapsed�� ȣ��ǰ�
        /// TransmitCANData�� ȣ��Ǹ� CANDataTransmitted �̺�Ʈ �ڵ鷯�� ȣ��ǰ�, TestViewModel�� OnPumpCANDataTransmitted�� ȣ��ȴ�.
        /// </summary>
        private void TransmitCANData()
        {
            // PSTARFW.c�� tx_data �迭�� �����ϰ� ����
            byte[] data = new byte[8];
            data[0] = (byte)(STBY_Start ? 1 : 0);
            data[1] = (byte)(RunLamp ? 1 : 0);
            data[2] = (byte)(Overload ? 1 : 0);
            data[3] = (byte)(ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[4] = (byte)(RUN_req ? 1 : 0);
            data[5] = (byte)(ResetButton ? 1 : 0);
            data[6] = (byte)(StandByLamp ? 1 : 0);
            data[7] = (byte)(TXLowpress ? 1 : 0);

            // CAN ������ ����
            // 250818TODO ������ CAN ������ ���� �����ϰ� CANFrame.cs �ڵ� ����
            var frame = new CANFrame
            {
                Id = _canId,
                Data = data,
                Timestamp = DateTime.Now
            };

            // �̺�Ʈ �߻�
            CANDataTransmitted?.Invoke(this, new CANTransmitEventArgs(frame));
        }

        #region PSTARFW.c �Լ� ����

        /// <summary>
        /// �з� ����ġ ���� ������Ʈ (Pressure Switch 1EA or 2EA)
        /// </summary>
        private void UpdatePressureStatus()
        {
            // �߿��� main() �Լ� ��ܺ��� ����
            TXLowpress = txLowpress;
        }

        /// <summary>
        /// �Է� ó�� (KeyProc, OverloadProc, LowpressProc �Լ� ����)
        /// </summary>
        private void ProcessInputs()
        {
            // KeyProc, OverloadProc, LowpressProc �� �Է� ó�� ���� ����
        }

        /// <summary>
        /// ����/���� ���� ó��
        /// </summary>
        private void RunStopProc(bool runStatus)
        {
            if (runStatus) // RUN
            {
                ResetButton = false;
                StopLamp = false;
            }
            else // STOP
            {
                StopLamp = true;
                RunLamp = false;
            }
        }

        /// <summary>
        /// ���� �Է� ó��
        /// </summary>
        private void RunInput()
        {
            if (RunFB_I) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                RunLamp = true;
                StopLamp = false;
            }
        }

        /// <summary>
        /// ���� ó��
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            // HeatProc ���� ����
        }

        /// <summary>
        /// ��� ó��
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            // ModeProc ���� ����
        }

        /// <summary>
        /// ������ ó��
        /// </summary>
        private void OverloadProc(bool overloadStatus)
        {
            if (overloadStatus)
            {
                if (RunStatus)
                {
                    Stop_Overload = true;
                    RunStatus = false;
                }

                Overload = true;
                CountParallelTime_S = 0;

                ResetButton = false;

                if (StandByLamp)
                {
                    StandByLamp = false; // Occur Overload -> StandBy x
                    STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                Overload = false;

                STBY_Overload = false;
                Stop_Overload = false;
            }
        }

        /// <summary>
        /// ���� ó��
        /// </summary>
        private void LowpressProc(bool lowpressStatus)
        {
            // LowpressProc ���� ����
        }

        /// <summary>
        /// ��� ���� �÷��� ó��
        /// </summary>
        private void ComFailErrorFlag()
        {
            // ComFailErrorFlag ���� ����
        }

        /// <summary>
        /// Run ��û ���� ó�� (StandBy ����)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (!Overload && StandByLamp)
            {
                if (!Request_Flag && (rx_data1[4] == 1 || rx_data2[4] == 1 || rx_data3[4] == 1))
                {
                    Request_Flag = true;
                    RunStatus = true;
                }
                else if (rx_data1[4] == 0 && rx_data2[4] == 0 && rx_data3[4] == 0)
                {
                    Request_Flag = false;
                }
            }
        }

        /// <summary>
        /// Run ��û ���� ó�� (���� ����)
        /// </summary>
        private void SendRunReq()
        {
            // SendRunReq ���� ����
        }

        /// <summary>
        /// StandBy ���� �˶� ó��
        /// </summary>
        private void StbyStartAlarm()
        {
            if (RunLamp && StandByLamp)
            {
                STBY_Start = true;
            }
            else
            {
                STBY_Start = false;
            }
        }

        /// <summary>
        /// ���� ���� �Լ�
        /// </summary>
        private void ConnectFunction()
        {
            // ConnectFunction ���� ����
        }

        /// <summary>
        /// StandBy ���� ó��
        /// </summary>
        private void StandByLampProc(bool modeStatus)
        {
            // StandByLampProc ���� ���� (�ſ� ������ ����)
        }
        #endregion

        #region �ܺ� �������̽� �޼���
        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStartButton()
        {
            if (!Overload)
            {
                if (!RunStatus)
                {
                    if (!ModeStatus) // MANUAL_MODE
                    {
                        RunStatus = true;
                    }
                }
            }
        }

        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStopButton()
        {
            RunStatus = false;
            ResetButton = true;
        }

        /// <summary>
        /// ��� ��ư ���� ó��
        /// </summary>
        public void PressModeButton()
        {
            ModeStatus = !ModeStatus;
        }

        /// <summary>
        /// ��Ʈ ��ư ���� ó��
        /// </summary>
        public void PressHeatButton()
        {
            HeatStatus = !HeatStatus;
        }

        /// <summary>
        /// ������ ���� ����
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            Overload_I = isOverload;
        }

        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            Lowpress_I = isLowPressure;
        }

        /// <summary>
        /// ���� ���� �˸�
        /// </summary>
        private void NotifyStateChanged()
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                DeviceId = _deviceId,
                IsRunning = RunStatus,
                IsStandByMode = ModeStatus,
                IsHeating = HeatStatus,
                IsStandByLamp = StandByLamp
            });
        }

        /// <summary>
        /// ���ҽ� ����
        /// </summary>
        public void Dispose()
        {
            _logicTimer?.Stop();
            _transmitTimer?.Stop();
            _logicTimer?.Dispose();
            _transmitTimer?.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// ��ġ ���� ���� �̺�Ʈ �μ�
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public bool IsRunning { get; set; }
        public bool IsStandByMode { get; set; }
        public bool IsHeating { get; set; }
        public bool IsStandByLamp { get; set; }
    }
}