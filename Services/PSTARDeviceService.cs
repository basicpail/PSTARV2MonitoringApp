using System;
using System.Timers;
using PSTARV2MonitoringApp.Models;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// ���� ��ġ ������ ����
    /// </summary>
    public class PSTARDeviceService : IDisposable
    {
        #region ����
        // ��ġ ID�� CAN ID, interval
        private readonly string _deviceId;
        private readonly uint _canId;
        private readonly int _canTransmitInterval = 500; // CAN ���� �ֱ� (ms)

        // CAN ���� Ÿ�̸� (���� Ÿ�̸Ӵ� ����)
        private readonly Timer _transmitTimer;

        // �� ���۷��� (��ġ �� ����)
        private PSTARDeviceModel _model;

        // PSTARFW ���� (�ʿ��� �͸� �߰�)
        private int _buildUpTime = 5;   // BuildUp �ð� (��)
        private int _parallelTime = 10; // Parallel �ð� (��)
        private int _seqTime_mS = 10;   // Sequential Time (��)
        private bool _requestFlag = false;      // ���� ��û �÷���
        #endregion

        #region �̺�Ʈ
        // CAN ������ ���� �̺�Ʈ
        public event EventHandler<CANTransmitEventArgs> CANDataTransmitted;

        // ���� ���� �̺�Ʈ
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        #endregion

        #region ������Ƽ
        // ���� ������ ���� �б� ���� ������Ƽ
        public bool RunStatus => _model?.RunStatus ?? false;
        public bool HeatStatus => _model?.HeatStatus ?? false;
        public bool ModeStatus => _model?.ModeStatus ?? false;
        public bool STBY_Start => _model?.STBY_Start ?? false;
        public bool RunLamp => _model?.RunLamp ?? false;
        public bool Overload => _model?.Overload ?? false;
        public bool RUN_req => _model?.RUN_req ?? false;
        public bool ResetButton => _model?.ResetButton ?? false;
        public bool StandByLamp => _model?.StandByLamp ?? false;
        public bool TXLowpress => _model?.TXLowpress ?? false;
        public bool StopLamp => _model?.StopLamp ?? true;
        public bool OldLowpress => _model?.OldLowpress ?? false;
        public bool OldRunStatus => _model?.OldRunStatus ?? false;
        public int CountBuildUpTime => _model?.CountBuildUpTime ?? 0;
        public int CountHeatingOnTime_S => _model?.CountHeatingOnTime_S ?? 0;
        public int HeatingOnTime => _model ?.HeatingOnTime ?? 3;
        public int CountRunReq_S => _model?.CountRunReq_S ?? 0;
        public int CountComFault1_S => _model?.CountComFault1_S ?? 0;
        public int CountComFault2_S => _model?.CountComFault2_S ?? 0;
        public int CountComFault3_S => _model?.CountComFault3_S ?? 0;
        public int CountComInit => _model?.CountComInit ?? 0;
        public bool StandBy_3_1RUN_Flag => _model?.StandBy_3_1RUN_Flag ?? false;
        public int CountSeqTime_S => _model?.CountSeqTime_S ?? 0;
        public bool CountOverload_Flag => _model?.CountOverload_Flag ?? false;
        #endregion


        #region CANDataIndex
        /// <summary>
        /// CAN ������ �ε��� ����
        /// </summary>
        public static class CANDataIndices
        {
            // CAN ������ �ε��� ����
            public const int STBY_START = 0;     // Standby ���� (tx_data[0])
            public const int RUN_LAMP = 1;       // ���� ���� ���� (tx_data[1])
            public const int OVERLOAD = 2;       // ������ ���� (tx_data[2])
            public const int MODE_STATUS = 3;    // ��� ���� (tx_data[3]) - 0: MANUAL, 1: STBY
            public const int RUN_REQ = 4;        // ���� ��û (tx_data[4])
            public const int RESET_BUTTON = 5;   // ���� ��ư ���� (tx_data[5])
            public const int STANDBY_LAMP = 6;   // Standby ���� ���� (tx_data[6])
            public const int TX_LOWPRESS = 7;    // ���� ���� (tx_data[7])
        }
        #endregion

        /// <summary>
        /// ������
        /// </summary>
        public PSTARDeviceService(string deviceId)
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

            // CAN ���� Ÿ�̸�
            _transmitTimer = new Timer(_canTransmitInterval);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;
        }

        /// <summary>
        /// ��ġ �� ����
        /// </summary>
        public void SetModel(PSTARDeviceModel model)
        {
            // ���� �� �̺�Ʈ ���� ����
            if (_model != null)
            {
                _model.StateChanged -= OnModelStateChanged;
            }

            // �� �� ����
            _model = model;

            // �� ���� ������ �̺�Ʈ ����
            if (_model != null)
            {
                _model.StateChanged += OnModelStateChanged;
            }
        }

        /// <summary>
        /// �� ���� ���� �̺�Ʈ ó��
        /// </summary>
        private void OnModelStateChanged(object sender, EventArgs e)
        {
            // �� ���°� ����Ǹ� ���� ����
            ExecutePSTARLogic();

            // ���� ���� �˸�
            NotifyStateChanged();
        }

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StartSimulation()
        {
            _transmitTimer.Start();

            // �ʱ� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StopSimulation()
        {
            _transmitTimer.Stop();
        }

        /// <summary>
        /// ���� ���� (���� OnLogicTimerElapsed�� ����)
        /// </summary>
        private void ExecutePSTARLogic()
        {
            if (_model == null) return;

            // PSTARFW.c�� ���� ���� ���� ����
            HandlePowerRecovery();
            UpdatePressureStatus();
            ProcessInputs();
            RunStopProc(_model.RunStatus);
            RunInput();
            HeatProc(_model.HeatStatus);
            ModeProc(_model.ModeStatus);
            OverloadProc(_model.Overload_I);
            LowpressProc(_model.Lowpress_I);
            ComFailErrorFlag();
            ReceiveRunReq();
            SendRunReq();
            ConnectFunction();
            StandByLampProc(_model.ModeStatus);
            StbyStartAlarm();
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
            if (_model == null) return;

            // �𵨿� ������ ó�� ����
            _model.ProcessReceivedCANFrame(frame);
        }

        /// <summary>
        /// CAN ������ ����
        /// </summary>
        private void TransmitCANData()
        {
            if (_model == null) return;

            // �𵨿��� ������ �о CAN ������ ����
            byte[] data = new byte[8];
            data[0] = (byte)(_model.STBY_Start ? 1 : 0);
            data[1] = (byte)(_model.RunLamp ? 1 : 0);
            data[2] = (byte)(_model.Overload ? 1 : 0);
            data[3] = (byte)(_model.ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[4] = (byte)(_model.RUN_req ? 1 : 0);
            data[5] = (byte)(_model.ResetButton ? 1 : 0);
            data[6] = (byte)(_model.StandByLamp ? 1 : 0);
            data[7] = (byte)(_model.TXLowpress ? 1 : 0);

            // CAN ������ ����
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

        // SeqTime_mS ���� �߰�

        /// <summary>
        /// ���� ���� ���� (POWER RECOVERY AFTER POWER FAIL)
        /// </summary>
        private void HandlePowerRecovery()
        {
            if (_model == null) return;

            if (_model.InitFlag)
            {
                if (_seqTime_mS < 630) // 0~62s : UVR
                {
                    // Push the Reset Button -> STANDBY LAMP ON (Change MAIN Pump : MAIN -> STBY)
                    if (_model.ModeStatus &&
                        ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                         (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                         (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� C#������ ����
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(2 RUN) - MAIN 1 & MAIN2)
                    else if ((_deviceId == "1" && _model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1) ||
                             (_deviceId == "2" && _model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1) ||
                             (_deviceId == "3" && _model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� C#������ ����
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(1 RUN) - ST'BY)
                    else if (_model.ComStatus == 5 && // StandBy_3_1RUN
                            ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1 && _model.RX_Data1[6] == 1) ||
                             (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1 && _model.RX_Data2[6] == 1) ||
                             (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1 && _model.RX_Data3[6] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� C#������ ����
                        _model.InitFlag = false;
                    }
                    // Sequential Time (MANUAL RUN / 2 STAND BY RUN & RUN / Power Recovery Before 1s (MAIN -> MAIN))
                    else
                    {
                        // ���� ���� �� ��ȣ ����
                        _model.StopLamp = false;

                        // CountSeqTime_mS�� SeqTime_mS�� �����ϸ� RUN ���·� ����
                        if (_model.CountSeqTime_S * 1000 >= _seqTime_mS) // CountSeqTime_S�� �� ����, _seqTime_mS�� �и��� ����
                        {
                            _model.RunStatus = true;
                            _model.InitFlag = false;
                            _model.CountSeqTime_S = 0;
                        }
                    }
                }
                else if (_seqTime_mS == 630) // 63s : UVP
                {
                    _model.RunStatus = false;
                    // EEPROM ���� �κ��� C#������ ����
                    _model.InitFlag = false;
                }
            }
            else
            {
                _model.CountSeqTime_S = 0;
            }

            // �ٸ� ��ġ�� ������ ���� Ȯ��
            if (_model.RX_Data1[2] == 1 || _model.RX_Data2[2] == 1 || _model.RX_Data3[2] == 1)
            {
                _model.CountOverload_Flag = true;
            }
            else
            {
                _model.CountOverload_Flag = false;
            }
        }

        /// <summary>
        /// �з� ����ġ ���� ������Ʈ (Pressure Switch 1EA or 2EA)
        /// </summary>
        private void UpdatePressureStatus()
        {
            if (_model == null) return;

            // �߿��� main() �Լ� ��ܺ��� ����
            _model.TXLowpress = _model.TxLowpressInternal;
        }

        /// <summary>
        /// �Է� ó�� (KeyProc, OverloadProc, LowpressProc �Լ� ����)
        /// </summary>
        private void ProcessInputs()
        {
            if (_model == null) return;

            // �߿������ �� �κ��� ������ �Է� ó���� ���������
            // ���⼭�� �Է��� UI�� ��ɿ� ���ؼ� ����ǹǷ� �ܼ� ���� üũ�� ��

            // ������ �Է� ��ȣ�� ���� �� ������ ó��
            if (_model.Overload_I)
            {
                OverloadProc(true);
            }

            // ���� �Է� ��ȣ�� ���� �� ���� ó��
            if (_model.Lowpress_I)
            {
                LowpressProc(true);
            }
        }

        /// <summary>
        /// ����/���� ���� ó��
        /// </summary>
        private void RunStopProc(bool runStatus)
        {
            if (_model == null) return;

            if (runStatus) // RUN
            {
                _model.ResetButton = false;
                _model.StopLamp = false;
            }
            else // STOP
            {
                _model.StopLamp = true;
                _model.RunLamp = false;
            }
        }

        /// <summary>
        /// ���� �Է� ó��
        /// </summary>
        private void RunInput()
        {
            if (_model == null) return;

            if (_model.RunFB_I) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                _model.RunLamp = true;
                _model.StopLamp = false;
            }
        }

        /// <summary>
        /// ���� ó��
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            if (_model == null) return;

            if (!heatStatus) // HEAT OFF
            {
                _model.IsHeatOn = false;
                _model.IsHeating = false;
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.IsHeatOn = true;

                if (!_model.RunStatus) // STOP ���¿����� ���� ��ȣ Ȱ��ȭ
                {
                    if (_model.CountHeatingOnTime_S >= _model.HeatingOnTime)
                    {
                        _model.IsHeating = true;
                    }
                }
                else // RUN ���¿����� ���� ��ȣ ��Ȱ��ȭ
                {
                    _model.IsHeating = false;
                    _model.CountHeatingOnTime_S = 0;
                }
            }
        }

        /// <summary>
        /// ��� ó��
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            if (_model == null) return;

            if (!modeStatus) // MANUAL_MODE
            {
                _model.IsManualMode = true;
                _model.IsStandbyMode = false;
            }
            else // STBY_MODE
            {
                _model.IsManualMode = false;
                _model.IsStandbyMode = true;
            }
        }

        /// <summary>
        /// ������ ó��
        /// </summary>
        private void OverloadProc(bool overloadStatus)
        {
            if (_model == null) return;

            if (overloadStatus)
            {
                if (_model.RunStatus)
                {
                    _model.Stop_Overload = true;
                    _model.RunStatus = false;
                }

                _model.Overload = true;
                _model.CountParallelTime_S = 0;

                _model.ResetButton = false;

                if (_model.StandByLamp)
                {
                    _model.StandByLamp = false; // Occur Overload -> StandBy x
                    _model.STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                _model.Overload = false;

                _model.STBY_Overload = false;
                _model.Stop_Overload = false;
            }
        }

        /// <summary>
        /// ���� ó��
        /// </summary>
        private void LowpressProc(bool lowpressStatus)
        {
            if (_model == null) return;

            if (lowpressStatus)
            {
                _model.IsLowPressure = true;
                _model.TxLowpressInternal = true;

                // BuildUp �ð� ���� ����
                if (_model.OldRunStatus == true && _model.RunStatus == true)
                {
                    if (!_model.OldLowpress && _model.IsLowPressure)
                    {
                        _model.CountBuildUpTime = 3; // 3�� (RUN -> LowPressure)
                    }
                }

                // STOP -> LowPressure -> RUN
                if (_model.OldLowpress && _model.IsLowPressure)
                {
                    if (!_model.OldRunStatus && _model.RunStatus)
                    {
                        if (!_model.StandByLamp)
                        {
                            _model.CountBuildUpTime = _buildUpTime; // ������
                        }
                        else
                        {
                            _model.CountBuildUpTime = 3; // 3��
                        }
                    }
                }

                // BuildUp ���� ����
                if (_model.IsLowPressure && _model.RunLamp &&
                    ((_model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1) ||
                     (_model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1) ||
                     (_model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1)))
                {
                    _model.CountBuildUpStart = 1;
                }
                else
                {
                    _model.CountBuildUpStart = 0;
                }

                // BuildUp �� Parallel �ð� ����
                if (_model.RunLamp && _model.StandByLamp && _model.IsLowPressure)
                {
                    if (_model.CountBuildUpTime_S >= _model.CountBuildUpTime)
                    {
                        _model.CountParaStart = 0;
                        _model.CountParallelTime_S = 0;
                        _model.CountBuildUpStart = 0;
                        _model.CountBuildUpTime_S = 0;
                    }
                }
                else if (_model.IsLowPressure &&
                    ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                     (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                     (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                {
                    if (_model.CountBuildUpTime_S >= _model.CountBuildUpTime)
                    {
                        _model.CountParaStart = 1;
                        _model.CountBuildUpStart = 0;
                        _model.CountBuildUpTime_S = 0;
                    }
                }

                // Parallel �ð� �ʰ� ó��
                if (_model.CountParaStart == 1)
                {
                    if (_model.CountParallelTime_S >= _parallelTime)
                    {
                        _model.RunStatus = false;
                        _model.CountParaStart = 0;
                        _model.CountParallelTime_S = 0;
                    }
                }
            }
            else
            {
                _model.IsLowPressure = false;
                _model.TxLowpressInternal = false;

                // ī���� �ʱ�ȭ
                _model.CountBuildUpTime_S = 0;
                _model.CountBuildUpStart = 0;
            }

            // ���� ���� ����
            _model.OldLowpress = _model.IsLowPressure;
            _model.OldRunStatus = _model.RunStatus;
        }

        /// <summary>
        /// ��� ���� �÷��� ó��
        /// </summary>
        private void ComFailErrorFlag()
        {
            if (_model == null) return;

            // CAN ID�� ���� �ٸ� ��ġ���� ��� ���� Ȯ��
            string deviceId = _deviceId;

            // CAN ID 1�� ��ġ�� ��� ���� Ȯ��
            if (_model.CountComFault1_S > 1 && _model.Error_Flag1 == false && deviceId != "1")
            {
                _model.Error_Flag1 = true;

                // �ڵ� ��ȯ (StandBy���� �۵� ���̴� ��ġ ���� �� ����)
                if (_model.StandByLamp && _model.RX_Data1[1] == 1)
                {
                    _model.RunStatus = true;
                }

                // ���� ������ �ʱ�ȭ
                _model.RX_Data1 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault1_S <= 1 && _model.Error_Flag1 && deviceId != "1")
                {
                    _model.Error_Flag1 = false;
                }
            }

            // ���� ���� ������� ���� ������ ����
            if (_model.RX_Data1[3] == 0 && deviceId != "1") // MANUAL_MODE
            {
                _model.Error_Flag1 = true;
            }
            else if (_model.RX_Data1[3] == 1 && deviceId != "1") // STBY_MODE
            {
                _model.Error_Flag1 = false;
            }

            // CAN ID 2�� ��ġ�� ��� ���� Ȯ�� (���� ������ ����)
            if (_model.CountComFault2_S > 1 && _model.Error_Flag2 == false && deviceId != "2")
            {
                _model.Error_Flag2 = true;

                if (_model.StandByLamp && _model.RX_Data2[1] == 1)
                {
                    _model.RunStatus = true;
                }

                _model.RX_Data2 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault2_S <= 1 && _model.Error_Flag2 && deviceId != "2")
                {
                    _model.Error_Flag2 = false;
                }
            }

            if (_model.RX_Data2[3] == 0 && deviceId != "2")
            {
                _model.Error_Flag2 = true;
            }
            else if (_model.RX_Data2[3] == 1 && deviceId != "2")
            {
                _model.Error_Flag2 = false;
            }

            // CAN ID 3�� ��ġ�� ��� ���� Ȯ��
            if (_model.CountComFault3_S > 1 && _model.Error_Flag3 == false && deviceId != "3")
            {
                _model.Error_Flag3 = true;

                if (_model.StandByLamp && _model.RX_Data3[1] == 1)
                {
                    _model.RunStatus = true;
                }

                _model.RX_Data3 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault3_S <= 1 && _model.Error_Flag3 && deviceId != "3")
                {
                    _model.Error_Flag3 = false;
                }
            }

            if (_model.RX_Data3[3] == 0 && deviceId != "3")
            {
                _model.Error_Flag3 = true;
            }
            else if (_model.RX_Data3[3] == 1 && deviceId != "3")
            {
                _model.Error_Flag3 = false;
            }
        }

        /// <summary>
        /// Run ��û ���� ó�� (StandBy ����)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (_model == null) return;

            if (!_model.Overload && _model.StandByLamp)
            {
                if (!_requestFlag &&
                    (_model.RX_Data1[4] == 1 || _model.RX_Data2[4] == 1 || _model.RX_Data3[4] == 1))
                {
                    _requestFlag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[4] == 0 && _model.RX_Data2[4] == 0 && _model.RX_Data3[4] == 0)
                {
                    _requestFlag = false;
                }
            }
        }

        /// <summary>
        /// Run ��û ���� ó�� (���� ����)
        /// </summary>
        private void SendRunReq()
        {
            if (_model == null) return;

            if (_model.ModeStatus && _model.ComStatus != 0) // STBY_MODE & Connected
            {
                // ���� ���¿��� ���� ���� ��� Run ��û ����
                if (_model.IsLowPressure && _model.RunLamp &&
                    _model.CountBuildUpTime_S == _model.CountBuildUpTime &&
                    !_model.RUN_req)
                {
                    _model.RUN_req = true;
                }
                // ������ ���¿��� Run ��û ����
                else if (_model.Stop_Overload && _model.Overload &&
                        !_model.STBY_Overload &&
                        _model.CountRunReq_S == 1 &&
                        !_model.RUN_req)
                {
                    _model.RUN_req = true;
                }
                // ���� ���·� ���� �� Run ��û ���
                else if (!_model.Overload && !_model.IsLowPressure && !_model.RunLamp &&
                        (_model.RX_Data1[1] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
                // ���� ���¿��� �ٸ� ������ ���� ���� ��� Run ��û ���
                else if (_model.IsLowPressure &&
                        (_model.RX_Data1[1] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        !_model.RunLamp && _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
                // ������ ���°� ���ӵ� ��� Run ��û ���
                else if (_model.Overload && _model.CountRunReq_S > 1 && _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
            }
        }

        /// <summary>
        /// StandBy ���� �˶� ó��
        /// </summary>
        private void StbyStartAlarm()
        {
            if (_model == null) return;

            if (_model.RunLamp && _model.StandByLamp)
            {
                _model.STBY_Start = true;
            }
            else
            {
                _model.STBY_Start = false;
            }
        }

        ////////////////////////////ConnectFunction ���� ����////////////////////////////////////
        /// <summary>
        /// ���� ���� �Լ� - PSTARFW.c�� ConnectFunction ����
        /// </summary>
        private void ConnectFunction()
        {
            if (_model == null) return;

            // �� �Լ��� �ٸ� CAN ��� ���¿� ���� ���� ��ġ�� ���� ���¸� ����
            // ���� ���� �ڵ� (�߿���� ����)
            // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
            // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

            switch (_deviceId)
            {
                case "1":
                    HandleConnectDevice1();
                    break;
                case "2":
                    HandleConnectDevice2();
                    break;
                case "3":
                    HandleConnectDevice3();
                    break;
            }
        }

        /// <summary>
        /// ��ġ ID 1�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice1()
        {
            // MANUAL ����̸� ���� ���� (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag2 && _model.Error_Flag3)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3�� ���� (StandBy_3)
            if (!_model.Error_Flag2 && !_model.Error_Flag3)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                if ((_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2�� ���� (StandBy_2 �Ǵ� StandBy_3to2)
            if ((!_model.Error_Flag2 && _model.Error_Flag3) || (_model.Error_Flag2 && !_model.Error_Flag3))
            {
                if (_model.ComStatus == 3) // ������ StandBy_3�̾��ٸ�
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // �̹� StandBy_2 ���¶��
                {
                    // ����
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // �޸� ���¿� ���� ����
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN ���¿��ٸ�
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }

        /// <summary>
        /// ��ġ ID 2�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice2()
        {
            // MANUAL ����̸� ���� ���� (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag1 && _model.Error_Flag3)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3�� ���� (StandBy_3)
            if (!_model.Error_Flag1 && !_model.Error_Flag3)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                if ((_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2�� ���� (StandBy_2 �Ǵ� StandBy_3to2)
            if ((!_model.Error_Flag1 && _model.Error_Flag3) || (_model.Error_Flag1 && !_model.Error_Flag3))
            {
                if (_model.ComStatus == 3) // ������ StandBy_3�̾��ٸ�
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // �̹� StandBy_2 ���¶��
                {
                    // ����
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // �޸� ���¿� ���� ����
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN ���¿��ٸ�
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }

        /// <summary>
        /// ��ġ ID 3�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice3()
        {
            // MANUAL ����̸� ���� ���� (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag1 && _model.Error_Flag2)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3�� ���� (StandBy_3)
            if (!_model.Error_Flag1 && !_model.Error_Flag2)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                if ((_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2�� ���� (StandBy_2 �Ǵ� StandBy_3to2)
            if ((!_model.Error_Flag1 && _model.Error_Flag2) || (_model.Error_Flag1 && !_model.Error_Flag2))
            {
                if (_model.ComStatus == 3) // ������ StandBy_3�̾��ٸ�
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // �̹� StandBy_2 ���¶��
                {
                    // ����
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // �޸� ���¿� ���� ����
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN ���¿��ٸ�
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }
        ////////////////////////////ConnectFunction ���� ����////////////////////////////////////


        ////////////////////////////StandByLampProc ���� ����////////////////////////////////////
        /// <summary>
        /// StandBy ���� ó��
        /// </summary>
        private void StandByLampProc(bool modeStatus)
        {
            if (_model == null) return;

            if (modeStatus) // STBY_MODE
            {
                switch (_model.ComStatus)
                {
                    case 3: // StandBy_3
                            // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                        if (!_model.Overload &&
                            ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                             (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                             (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                        {
                            if (_model.ResetButton)
                            {
                                _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            }
                        }
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                        else if (_model.RunLamp && _model.StandByLamp)
                        {
                            if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1) ||
                                (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1) ||
                                (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1))
                            {
                                _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            }
                        }
                        // AFTER POWER RECOVERY
                        else if ((_deviceId == "1" &&
                                  ((_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0 && _model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1) ||
                                   (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0 && _model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1))) ||
                                 (_deviceId == "2" &&
                                  ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0 && _model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1) ||
                                   (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0 && _model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1))) ||
                                 (_deviceId == "3" &&
                                  ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0 && _model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1) ||
                                   (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0 && _model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1))))
                        {
                            _model.StandByLamp = false;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        else if (((_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data1[6] == 0) ||
                                  (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && _model.RX_Data2[6] == 0) ||
                                  (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1 && _model.RX_Data3[6] == 0)) &&
                                 !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                        else if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1) ||
                                 (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1) ||
                                 (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1))
                        {
                            _model.StandByLamp = false;
                        }
                        // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                        else if ((_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0) ||
                                 (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0) ||
                                 (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0))
                        {
                            _model.StandByLamp = false;
                        }
                        break;

                    case 5: // StandBy_3_1RUN
                        HandleStandBy_3_1RUN();
                        break;

                    case 2: // StandBy_2
                    case 1: // StandBy_3to2
                    case 6: // StandBy_3to2_1RUN
                        HandleStandBy_2_or_3to2();
                        break;
                }
            }
            else // MANUAL_MODE
            {
                _model.StandByLamp = false;
            }

            // StandBy ��ȣ ���
            _model.IsStandby = _model.StandByLamp;
        }

        /// <summary>
        /// StandBy_3_1RUN ���� ó�� (�ڵ� ����ȭ�� ���� �и� �޼���)
        /// </summary>
        private void HandleStandBy_3_1RUN()
        {
            // ��ġ ID�� �б� ó��
            if (_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0)
            {
                if (_deviceId == "2")
                {
                    if (_model.RX_Data3[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    if (_model.RX_Data3[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "3")
                {
                    if (_model.Overload && _model.RX_Data2[2] == 1)
                    {
                        // ī���� ����
                    }
                    else if (!_model.Overload && (_model.RX_Data2[2] == 1 || _model.RX_Data2[6] == 0))
                    {
                        if (_model.RX_Data2[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data2[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data1[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0)
            {
                if (_deviceId == "3")
                {
                    if (_model.RX_Data1[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    if (_model.RX_Data1[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "1")
                {
                    if (_model.Overload && _model.RX_Data3[2] == 1)
                    {
                        // ī���� ����
                    }
                    else if (!_model.Overload && (_model.RX_Data3[2] == 1 || _model.RX_Data3[6] == 0))
                    {
                        if (_model.RX_Data3[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data3[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data2[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0)
            {
                if (_deviceId == "1")
                {
                    if (_model.RX_Data2[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    else if (_model.RX_Data2[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "2")
                {
                    if (_model.Overload && _model.RX_Data1[2] == 1)
                    {
                        // ī���� ����
                    }
                    else if (!_model.Overload && (_model.RX_Data1[2] == 1 || _model.RX_Data1[6] == 0))
                    {
                        if (_model.RX_Data1[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data1[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data3[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                                        (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                                        (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1) ||
                        (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1) ||
                        (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1))
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }

                // ��ġ ID�� �߰� ó��
                HandleDeviceSpecificConditions();
            }
        }

        /// <summary>
        /// ��ġ ID�� ���� Ư�� ���� ó��
        /// </summary>
        private void HandleDeviceSpecificConditions()
        {
            if (_deviceId == "1" && _model.StandByLamp)
            {
                if (_model.RX_Data2[2] == 0 && _model.RX_Data3[2] == 0)
                {
                    if (_model.RX_Data2[1] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[2] == 1 && _model.RX_Data3[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_deviceId == "2" && _model.StandByLamp)
            {
                if (_model.RX_Data3[2] == 0 && _model.RX_Data1[2] == 0)
                {
                    if (_model.RX_Data3[1] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[2] == 1 && _model.RX_Data1[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_deviceId == "3" && _model.StandByLamp)
            {
                if (_model.RX_Data1[2] == 0 && _model.RX_Data2[2] == 0)
                {
                    if (_model.RX_Data1[1] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[2] == 1 && _model.RX_Data2[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
            }
        }

        /// <summary>
        /// StandBy_2 �Ǵ� StandBy_3to2 ���� ó��
        /// </summary>
        private void HandleStandBy_2_or_3to2()
        {
            // ��ġ ID�� ���� ���� ó��
            if (_model.Error_Flag1 == false && _deviceId != "1")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.Error_Flag2 == false && _deviceId != "2")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.Error_Flag3 == false && _deviceId != "3")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
        }
        ////////////////////////////StandByLampProc ���� ����////////////////////////////////////
        #endregion

        #region �ܺ� �������̽� �޼���
        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStartButton()
        {
            if (_model == null) return;

            if (!_model.Overload)
            {
                if (!_model.RunStatus)
                {
                    _model.RunStatus = true;
                    _model.ResetButton = false;

                    // ���� ���� �� ���� ����
                    ExecutePSTARLogic();
                }
            }
        }

        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStopButton()
        {
            if (_model == null) return;

            _model.RunStatus = false;
            _model.ResetButton = true;

            // ���� ���� �� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// ��� ��ư ���� ó��
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            _model.IsManualMode = !_model.IsManualMode; // Manual ���� toggle

            // ���� ���� �� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// ��Ʈ ��ư ���� ó��
        /// </summary>
        public void PressHeatButton()
        {
            if (_model == null) return;

            _model.HeatStatus = !_model.HeatStatus;
            _model.IsHeatOn = !_model.IsHeatOn; //Heat ���� toggle

            // ���� ���� �� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// ������ ���� ����
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            if (_model == null) return;

            _model.Overload_I = isOverload;

            // ���� ���� �� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            if (_model == null) return;

            _model.Lowpress_I = isLowPressure;

            // ���� ���� �� ���� ����
            ExecutePSTARLogic();
        }

        /// <summary>
        /// ���� ���� �˸�
        /// </summary>
        private void NotifyStateChanged()
        {
            if (_model == null) return;

            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                DeviceId = _deviceId,
                IsRunning = _model.RunStatus,
                IsStandByMode = _model.ModeStatus,
                IsHeating = _model.HeatStatus,
                IsStandByLamp = _model.StandByLamp
            });
        }

        /// <summary>
        /// ���ҽ� ����
        /// </summary>
        public void Dispose()
        {
            _transmitTimer?.Stop();
            _transmitTimer?.Dispose();

            // �� �̺�Ʈ ���� ����
            if (_model != null)
            {
                _model.StateChanged -= OnModelStateChanged;
            }
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