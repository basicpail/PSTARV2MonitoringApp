using System;
using System.Diagnostics.Eventing.Reader;
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
        private readonly uint CAN_ID;
        private readonly int _canTransmitInterval = 1000; // CAN ���� �ֱ� (ms)
        private readonly int _deviceLoopInterval; // TMR0 �� 100ms �ֱ��, Logic ���൵ �� �ֱ�� ����
        private int _count100_mS = 0;
        private DateTime _startButtonPressedTime = DateTime.MinValue;
        private DateTime _stopButtonPressedTime = DateTime.MinValue;
        private DateTime _modeButtonPressedTime = DateTime.MinValue;
        private DateTime _heatButtonPressedTime = DateTime.MinValue;

        // CAN ���� Ÿ�̸� (���� Ÿ�̸Ӵ� ����)
        private readonly Timer _transmitTimer;

        // PSTAR ��ġ Ÿ�̸�
        private readonly Timer _deviceTimer;

        // �� ���۷��� (��ġ �� ����)
        private PSTARDeviceModel _model;

        // PSTARFW ���� �ʿ��� �͸� �߰�
        private int _buildUpTime = 5;   // BuildUp �ð� (��)
        private int _parallelTime = 10; // Parallel �ð� (��)
        private int _seqTime_mS = 10;   // Sequential Time (��)
        private bool _requestFlag = false;  // ���� ��û �÷���
        
        //�� �÷��׸� Ư���ϰ� ���� �ݴ�� ����
        private bool Com_Error = false;
        private bool Com_Normal = true;
        #endregion

        #region �̺�Ʈ
        // CAN ������ ���� �̺�Ʈ
        public event EventHandler<CANTransmitEventArgs> CANDataTransmitted;

        // ���� ���� �̺�Ʈ
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        #endregion



        #region Index
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

        public static class COMStatusIndices
        {
            public const int NoConnection = 0;
            public const int StandBy_3to2 = 1;
            public const int StandBy_2 = 2;
            public const int StandBy_3 = 3;
            public const int Manual = 4;
            public const int StandBy_3_1RUN = 5;
            public const int StandBy_3to2_1RUN = 6;
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
                case "1": _canId = 0x100; CAN_ID = 1; break;
                case "2": _canId = 0x200; CAN_ID = 2; break;
                case "3": _canId = 0x300; CAN_ID = 3; break;
                default: _canId = 0x100; CAN_ID = 1; break;
            }

            // CAN ���� Ÿ�̸�
            //_transmitTimer = new Timer(_model.CANTransmitInterval); // ���� ���� �� ���, 
            _transmitTimer = new Timer(_canTransmitInterval);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;

            //PSTAR Ÿ�̸�
            //�̻��Ȳ �߻� �κп��� �Լ������ϰ� ���͹� ����
            _deviceTimer = new Timer();


            StartSimulation(); //ī�� ���� �� �� �ٷ� ���� �ǵ��� ����
            //RunStopCont();
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

            _deviceTimer.Interval = _deviceLoopInterval;
            _deviceTimer.Elapsed += OnDeviceTimerElapsed;
            _deviceTimer.AutoReset = true;
            _deviceTimer.Start();
        }

        

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StopSimulation()
        {
            _transmitTimer.Stop();
            _deviceTimer.Stop();
        }


        /// <summary>
        /// ��ġ Ÿ�̸� �̺�Ʈ �ڵ鷯 - �߿����� TMR0_DefaultInterruptHandler�� �ش�
        /// </summary>
        private void OnDeviceTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if(_model == null) return;

            ResetButtonStates();

            // 100ms ������ �����ϴ� ī����
            _count100_mS++;

            // ������ Ÿ�� ī��Ʈ
            _model.CountSeqTime_mS++;

            // ��Ÿ �и��� ���� ī���� ����
            _model.CountComFailLamp_mS++;
            _model.CountStandByCheck_mS++;

            // StandBy_2_Flag ���¿� ���� ī��Ʈ
            if (_model.StandBy_2_Flag)
                _model.CountStandBy_2_mS++;
            else
                _model.CountStandBy_2_mS = 0;

            // 1�� ����(10 * 100ms)�� ����Ǵ� �ڵ�
            if (_count100_mS == 9)
            {
                // ���� Ÿ�� ī��Ʈ
                if (_model.HeatStatus)
                    _model.CountHeatingOnTime_S++;

                // ���� Ÿ�� ī��Ʈ
                if (_model.CountParaStart)
                    _model.CountParallelTime_S++;

                // ����� Ÿ�� ī��Ʈ
                if (_model.CountBuildUpStart)
                    _model.CountBuildUpTime_S++;

                // ���� ��ư ī��Ʈ
                if (!_model.STOP_PB_I)
                    _model.CountResetButton_S++;
                else
                    _model.CountResetButton_S = 0;

                // ��� ��� ī��Ʈ
                _model.CountComFault1_S++;
                if (_model.CountComFault1_S >= 100)
                    _model.CountComFault1_S = 100;

                _model.CountComFault2_S++;
                if (_model.CountComFault2_S >= 100)
                    _model.CountComFault2_S = 100;

                _model.CountComFault3_S++;
                if (_model.CountComFault3_S >= 100)
                    _model.CountComFault3_S = 100;

                // �ʱ�ȭ ī��Ʈ
                _model.CountComInit++;
                if (_model.CountComInit >= 100)
                    _model.CountComInit = 100;

                // ������ ���¿��� ���� ��û ī��Ʈ
                if (_model.Overload)
                    _model.CountRunReq_S++;

                // ������ �÷��׿� ���� ī��Ʈ
                if (_model.CountOverload_Flag)
                    _model.CountOverload_S++;
                else
                    _model.CountOverload_S = 0;

                
            }
            else if (_count100_mS == 10)
            {
                _count100_mS = 0;
            }

            ExecutePSTARLogic();

        }

        /// <summary>
        /// ��ư ���� �ڵ� ���� - ��ư�� ���� �� ������ �ð��� ������ �ڵ����� ����
        /// </summary>
        private void ResetButtonStates()
        {
            // ���� ��ư ó��
            if (_model.START_PB_I && DateTime.Now - _startButtonPressedTime > TimeSpan.FromMilliseconds(300))
            {
                _model.START_PB_I = !_model.START_PB_I;
            }

            // ���� ��ư ó��
            if (_model.STOP_PB_I && DateTime.Now - _stopButtonPressedTime > TimeSpan.FromMilliseconds(300))
            {
                _model.STOP_PB_I = !_model.STOP_PB_I;
            }

            // ��� ��ư ó��
            if (_model.MODE_PB_I && DateTime.Now - _modeButtonPressedTime > TimeSpan.FromMilliseconds(300))
            {
                _model.MODE_PB_I = !_model.MODE_PB_I;
            }

            // ��Ʈ ��ư ó��
            if (_model.HEAT_PB_I && DateTime.Now - _heatButtonPressedTime > TimeSpan.FromMilliseconds(300))
            {
                _model.HEAT_PB_I = !_model.HEAT_PB_I;
            }
        }

        /// <summary>
        /// ���� ���� (���� OnLogicTimerElapsed�� ����)
        /// </summary>
        public void ExecutePSTARLogic()
        {
            if (_model == null) return;

            // PSTARFW.c�� ���� ���� ���� ����
            HandlePowerRecovery();
            //UpdatePressureStatus();
            //ProcessInputs();
            RunStopProc(_model.RunStatus);
            RunInput();
            HeatCont(_model.HeatStatus);
            HeatProc(_model.HeatStatus);
            ModeProc(_model.ModeStatus);
            KeyProc();
            OverloadProc(_model.Overload_I);
            LowpressProc(_model.Lowpress_I);
            ComFailErrorFlag();
            //���⿡ CanRevMsg
            ReceiveRunReq(); 
            SendRunReq();

            ConnectFunction();
            ConnectProc(_model.ComStatus);

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

            uint id = frame.Id;
            byte[] data = frame.Data;

            // �� ��ġ�� �ٸ� ��ġ���� �����͸� ����
            if (id == 0x100) // 1�� ��ġ���� �� �޽���
            {
                // ��� ���� ī���� ����
                if (CAN_ID != 1) // �ڽ��� ���� �޽����� ����
                {
                    _model.RX_Data1 = data;
                    _model.CountComFault1_S = 0;
                    Console.WriteLine($"ID=0x{id:X3}, ��ġ1 ������: {BitConverter.ToString(data).Replace("-", " ")}");
                }
            }
            else if (id == 0x200) // 2�� ��ġ���� �� �޽���
            {
                if (CAN_ID != 2) // �ڽ��� ���� �޽����� ����
                {
                    _model.RX_Data2 = data;
                    _model.CountComFault2_S = 0;
                    Console.WriteLine($"ID=0x{id:X3}, ��ġ2 ������: {BitConverter.ToString(data).Replace("-", " ")}");
                }
            }
            else if (id == 0x300) // 3�� ��ġ���� �� �޽���
            {
                if (CAN_ID != 3) // �ڽ��� ���� �޽����� ����
                {
                    _model.RX_Data3 = data;
                    _model.CountComFault3_S = 0;
                    Console.WriteLine($"ID=0x{id:X3}, ��ġ3 ������: {BitConverter.ToString(data).Replace("-", " ")}");
                }
            }
            //Console.WriteLine($"���� ������: ID=0x{canFrame.Id:X3}, Data={BitConverter.ToString(canFrame.Data).Replace("-", " ")}");

            // �𵨿� ������ ó�� ����
            //_model.ProcessReceivedCANFrame(frame);
        }

        /// <summary>
        /// CAN ������ ����
        /// </summary>
        private void TransmitCANData()
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{_deviceId}] TransmitCANData ȣ���");

            if (_model == null) return;

            // �𵨿��� ������ �о CAN ������ ����
            byte[] data = new byte[8];
            data[CANDataIndices.STBY_START] = (byte)(_model.STBY_Start ? 1 : 0);
            data[CANDataIndices.RUN_LAMP] = (byte)(_model.RUN_LAMP ? 1 : 0);
            data[CANDataIndices.OVERLOAD] = (byte)(_model.Overload ? 1 : 0);
            data[CANDataIndices.MODE_STATUS] = (byte)(_model.ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[CANDataIndices.RUN_REQ] = (byte)(_model.RUN_req ? 1 : 0);
            data[CANDataIndices.RESET_BUTTON] = (byte)(_model.ResetButton ? 1 : 0);
            data[CANDataIndices.STANDBY_LAMP] = (byte)(_model.STAND_BY_LAMP ? 1 : 0);
            data[CANDataIndices.TX_LOWPRESS] = (byte)(_model.TXLowpress ? 1 : 0);

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

        #region PSTAR ��� ����

        // SeqTime_mS ���� �߰�

        /// <summary>
        /// ���� ���� ���� (POWER RECOVERY AFTER POWER FAIL)
        /// </summary>
        private void HandlePowerRecovery()
        {
            /*
             * //---------------------------RUN---------------------------
                if(RunEE == STOP)                          RunStopCont(STOP);
                else if(RunEE == RUN && OVERLOAD_I == OFF) InitFlag = 1;
             */
            if (_model == null) return;

            if (_model.InitFlag == true)
            {
                if (_seqTime_mS < 630) // 0~62s : UVR
                {
                    // Push the Reset Button -> STANDBY LAMP ON (Change MAIN Pump : MAIN -> STBY)
                    if (_model.ModeStatus == true &&
                        ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                         (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                         (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� ����
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(2 RUN) - MAIN 1 & MAIN2)
                    else if ((CAN_ID == 1 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1) ||
                             (CAN_ID == 2 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1) ||
                             (CAN_ID == 3 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� ����
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(1 RUN) - ST'BY)
                    else if (_model.ComStatus == COMStatusIndices.StandBy_3_1RUN && // StandBy_3_1RUN
                            ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM ���� �κ��� ����
                        _model.InitFlag = false;
                    }
                    // Sequential Time (MANUAL RUN / 2 STAND BY RUN & RUN / Power Recovery Before 1s (MAIN -> MAIN))
                    else
                    {
                        // ���� ���� �� ��ȣ ����
                        _model.STOP_LAMP = false;

                        // CountSeqTime_mS�� SeqTime_mS�� �����ϸ� RUN ���·� ����
                        //if (_model.CountSeqTime_mS * 1000 >= _seqTime_mS) // CountSeqTime_mS�� �� ����, _seqTime_mS�� �и��� ����
                        if (_model.CountSeqTime_mS >= _seqTime_mS) // CountSeqTime_mS�� �� ����, _seqTime_mS�� �и��� ����
                            {
                            _model.RunStatus = true;
                            _model.InitFlag = false;
                            _model.CountSeqTime_mS = 0;
                        }
                    }
                }
                else if (_seqTime_mS == 630) // 63s : UVP
                {
                    _model.RunStatus = false;
                    // EEPROM ���� �κ��� ����
                    _model.InitFlag = false;
                }
            }
            else if (_model.InitFlag == false)
            {
                _model.CountSeqTime_mS = 0;
            }

            // �ٸ� ��ġ�� ������ ���� Ȯ��
            if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data2[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data3[CANDataIndices.OVERLOAD] == 1)
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
            //_model.TXLowpress = _model.TxLowpressInternal;
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

        private void RunStopCont(bool command) //command�� RunEE (0 �� STOP)
        {
            if (_model == null) return;

            if (command) // RUN
            {
            }
            else // STOP
            {
                _model.STOP_LAMP = true;

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
                if(_model.FirstRunStatus == false) _model.FirstRunStatus = true; //�� ���̶� RUN�� ������ ���Ŀ��� STOP �޽��� ������ ����. ���� ���� ���� �̹� STOP ������ �� ���ʿ��� STOP �޽��� �ܺ�(�ι���/PLC)�� ���� ������/���ʿ� �α׸� ����� �� ���� ����

                _model.ResetButton = false;
                _model.STOP_LAMP = false;
            }
            else // STOP
            {
                _model.STOP_LAMP = true;
                _model.RUN_LAMP = false;

                // ���� �� ù ���� ���� �ʱ�ȭ
                //STOP_SIG�� �׳� ��� ON���� ���� �ʰ�, ������ �ð�(StopPulse) ���ȸ� ON ���� �ڵ����� OFF �Ͽ� ���� ���� ���� Ʈ���š��� ����
                //if (FirstRunStatus == 1 && CountStopPulse_mS < StopPulse) LampSigCont(STOP_SIG, ON);
                //else if (CountStopPulse_mS >= StopPulse)
                //{
                //    CountStopPulse_mS = StopPulse;
                //    LampSigCont(STOP_SIG, OFF);
                //}
            }
        }

        /// <summary>
        /// ���� �Է� ó��
        /// </summary>
        private void RunInput()
        {
            if (_model == null) return;

            if (_model.START_PB_I == true) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                _model.RUN_LAMP = true;
                _model.STOP_LAMP = false;
            }
        }

        /// <summary>
        /// Heat_EE �� ó��
        /// command�� HeatEE (0 �� HEAT OFF)
        /// </summary>
        private void HeatCont(bool command)
        {
            if (_model == null) return;

            if (command == false) // HEAT OFF
            {
                _model.HEAT_ON_LAMP = false;
                _model.HEATING_LAMP = false; //HEAT�� SIG ó�� ������Ѵ�.
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.HEAT_ON_LAMP = true;
            }
        }

        /// <summary>
        /// ���� ó��
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            if (_model == null) return;

            if (heatStatus == false) // HEAT OFF
            {
                _model.HEAT_ON_LAMP = false;
                _model.HEATING_LAMP = false;
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.HEAT_ON_LAMP = true;

                if (_model.RunStatus == false) // STOP
                {
                    if (_model.CountHeatingOnTime_S >= _model.HeatingOnTime)
                    {
                        _model.HEATING_LAMP = true;
                    }
                }
                else // RUN ���¿����� ���� ��ȣ ��Ȱ��ȭ
                {
                    _model.HEATING_LAMP = false;
                    _model.CountHeatingOnTime_S = 0;
                }
            }
        }

        /// <summary>
        /// ��� ó��
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            if(_model == null) return;

            if(modeStatus == false) // MANUAL_MODE
            {
                _model.MODE_MANUAL_LAMP = true;
                _model.MODE_STBY_LAMP = false;
            }
            else // STBY_MODE
            {
                _model.MODE_STBY_LAMP = true;
                _model.MODE_MANUAL_LAMP = false;
            }
        }

        /// <summary>
        /// ��ư ���� ó��
        /// </summary>
        private void KeyProc()
        {
            if(_model == null) return;

            //-------------------------RUN/STOP BUTTON-------------------------
            if (_model.Overload_I == false)
            {
                //-------------------------RUN-------------------------
                if (_model.RunStatus == false)
                {
                    //�� ���� ���� �ϰ� ����� ��ġ(arm/disarm) ���, ��� ������ �־ �ݺ� Ʈ���Ű� �� ��
                    if (_model.OldStartPB == true && _model.START_PB_I == true)
                    {
                        _model.OldStartPB = false;
                        _model.RunStatus = true;
                    }
                    else if(_model.OldStartPB == false && _model.START_PB_I == false)
                    {
                        _model.OldStartPB = true;
                    }

                    if(_model.RunRemote_I == true) _model.RunStatus = true;
                }
                //-------------------------STOP-------------------------
                if(_model.OldStopPB == true && _model.STOP_PB_I == true)
                {
                    _model.OldStopPB = false;
                    _model.RunStatus = false;
                }
                else if(_model.OldStopPB == false && _model.STOP_PB_I == false)
                {
                    if (_model.RX_Data1[CANDataIndices.STBY_START] == 1 || _model.RX_Data2[CANDataIndices.STBY_START] == 1 || _model.RX_Data3[CANDataIndices.STBY_START] == 1)
                    {
                        _model.ResetButton = true; // StandBy Start �˶� �߻� �� Reset ��ư Ȱ��ȭ
                    }
                    else
                    {
                        _model.ResetButton = false;
                    }
                    _model.OldStopPB = true;
                }
                if(_model.StopRemote_I == true) _model.RunStatus = false;
            }

            //-------------------------HEAT BUTTON-------------------------
            if(_model.OldHeatPB == true && _model.HEAT_PB_I == true)
            {
                _model.OldHeatPB = false;

                if(_model.HeatStatus == false) _model.HeatStatus = true;
                else if(_model.HeatStatus == true) _model.HeatStatus = false;
            }
            else if(_model.OldHeatPB == false && _model.MODE_PB_I == false)
            {
                _model.OldHeatPB = true;
            }
            //-------------------------MODE BUTTON-------------------------
            if(_model.OldModePB == true && _model.MODE_PB_I == true)
            {
                _model.OldModePB = false;

                if(_model.ModeStatus == false) _model.ModeStatus = true; // MANUAL -> STBY
                else if(_model.ModeStatus == true) _model.ModeStatus = false; // STBY -> MANUAL
            }
            else if(_model.OldModePB == false && _model.MODE_PB_I == false)
            {
                _model.OldModePB = true;
            }
        }

        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStartButton()
        {
            if (_model == null) return;

            _model.START_PB_I = true; // Start ��ư ���� ��ȣ
            _startButtonPressedTime = DateTime.Now;
        }

        /// <summary>
        /// ���� ��ư ���� ó��
        /// </summary>
        public void PressStopButton()
        {
            if (_model == null) return;

            _model.STOP_PB_I = true; // Stop ��ư ���� ��ȣ
            _stopButtonPressedTime = DateTime.Now;
        }

        /// <summary>
        /// ��� ��ư ���� ó��
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            _model.MODE_PB_I = true; // Mode ��ư ���� ��ȣ
            _modeButtonPressedTime = DateTime.Now;
        }

        /// <summary>
        /// ��Ʈ ��ư ���� ó��
        /// </summary>
        public void PressHeatButton()
        {
            if (_model == null) return;

            _model.HEAT_PB_I = true; // Heat ��ư ���� ��ȣ
            _heatButtonPressedTime = DateTime.Now;
        }

        /// <summary>
        /// ������ ó��
        /// </summary>
        private void OverloadProc(bool Overload_I)
        {
            if (_model == null) return;

            if (Overload_I == true)
            {
                if (_model.RunStatus) //RUN ���¿��� ������ �߻� �� ����
                {
                    _model.Stop_Overload = true;
                    _model.RunStatus = false;
                }
                _model.ABN_LAMP = true;
                _model.Overload = true;
                _model.CountParallelTime_S = 0;

                _model.ResetButton = false;

                if (_model.STAND_BY_LAMP)
                {
                    _model.STAND_BY_LAMP = false; // Occur Overload -> StandBy x
                    _model.STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                _model.ABN_LAMP = false;
                _model.Overload = false;
                _model.CountRunReq_S = 0;

                _model.STBY_Overload = false;
                _model.Stop_Overload = false; //���� �ڵ忡�� false�� �Ҵ�
            }
        }

        #region LowpressProc ����
        /// <summary>
        /// ���� ó��
        /// </summary>
        private void LowpressProc(bool Lowpress_I)
        {
            if (_model == null) return;

            if (Lowpress_I)
            {
                _model.LOW_PRESS_LAMP = true;
                _model.Lowpress = true;

                _model.TXLowpress = true; // ���� ��ȣ
            }
            else if (_model.ComStatus == COMStatusIndices.StandBy_2 &&
                (_model.RX_Data1[CANDataIndices.TX_LOWPRESS] == 1 || _model.RX_Data2[CANDataIndices.TX_LOWPRESS] == 1 || _model.RX_Data3[CANDataIndices.TX_LOWPRESS] == 1))
                // 2�밡 StandBy�� �� �ٸ� ��ġ���� ���� ��ȣ ���� ��
            {
                _model.LOW_PRESS_LAMP = true;
                _model.Lowpress = true;
                _model.TXLowpress = true;
            }
            else if (Lowpress_I == false || 
                (_model.ComStatus == COMStatusIndices.StandBy_2 && 
                (_model.RX_Data1[CANDataIndices.TX_LOWPRESS] == 0 || _model.RX_Data2[CANDataIndices.TX_LOWPRESS] == 0 || _model.RX_Data3[CANDataIndices.TX_LOWPRESS] == 0 )))
                // ���� ��ȣ�� ���� 2�밡 StandBy�� �� �ٸ� ��ġ���� ���� ��ȣ�� ���� ��
            {
                _model.LOW_PRESS_LAMP = false;
                _model.Lowpress = false;
                _model.TXLowpress = false;

                _model.CountBuildUpTime_S = 0;
                _model.CountBuildUpStart = false;
            }
            //--------------------------BUILD UP TIME SETTING--------------------------
            // RUN -> LowPressure
            if(_model.OldRunStatus == true && _model.RunStatus == true) //RUN ���¿��� ���� �߻� �� (�⵿ �ܰ�� ���� ���� �ܰ踦 �����ϱ� ���� ��.)
            {
                if(_model.OldLowpress == false && _model.Lowpress == true) //���� ��ȣ�� ó�� ������ ��
                {
                    _model.CountBuildUpTime = 3; // 3�� (RUN -> LowPressure)
                }
            }
            // STOP -> LowPressure -> RUN
            if(_model.OldLowpress == true && _model.Lowpress == true) //���� ���¿���
            {
                if(_model.OldRunStatus == false && _model.RunStatus == true) //STOP -> RUN ���·� ���� ��
                {
                    if(_model.STAND_BY_LAMP == false) //StandBy�� �ƴ� ��
                    {
                        _model.CountBuildUpTime = _model.BuildUpTime; // ������
                    }
                    else if(_model.STAND_BY_LAMP == true) //StandBy ����� �� (�̶��� �� 3�� ��?)
                    {
                        _model.CountBuildUpTime = 3; // 3��
                    }
                }
            }
            if(_model.Lowpress == true && _model.RUN_LAMP == true && //���� ���¿��� RUN ������ ���� �ְ�
                ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) || //�ٸ� ��ġ���� StandBy ������ ��
                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
            {
                _model.CountBuildUpStart = true; //BuildUp ī��Ʈ ����
            }
            else
            {
                _model.CountBuildUpStart = false;
            }
            //--------------------------PARALLEL TIME COUNT START (AFTER BUILD UP TIME OVER)--------------------------
            if(_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true && _model.Lowpress == true) //RUN & StandBy & ���� ���¿���
                //���� ���Ĺ��� ������ ��Ȳ, build up �Ŀ��� ���� �����̸� ���Ĺ��� ������ �Բ� �⵿�ؼ� ���ɿ��� ����.
            {
                if(_model.CountBuildUpTime_S >= _model.CountBuildUpTime) //BuildUp �ð��� ������
                {
                    _model.CountParaStart = false; //???���� ���Ĺ��� �����ϱ� ������� Ÿ�̸Ӹ� ����. ���Ĺ��� ������ ���� ī��Ʈ�� �ع����� ���ΰ� �Բ� ���Ĺ��̵� ������ ���� ���� ������ ���� ����
                    _model.CountParallelTime_S = 0;
                    _model.CountBuildUpStart = false; //BuildUp ī��Ʈ ����
                    _model.CountBuildUpTime_S = 0; //BuildUp �ð� �ʱ�ȭ
                }
            }
            else if(_model.Lowpress == true && //���� ����, ���� ���� ������ ��Ȳ
                ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) || //�ٸ� ��ġ���� StandBy ������ ��
                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
            {
                if(_model.CountBuildUpTime_S >= _model.CountBuildUpTime) //BuildUp �ð��� ������
                {
                    _model.CountParaStart = true; //Parallel �ð� ī��Ʈ ����
                    _model.CountBuildUpStart = false; //BuildUp ī��Ʈ ����
                    _model.CountBuildUpTime_S = 0; //BuildUp �ð� �ʱ�ȭ
                }
            }
            //--------------------------PARALLEL TIME COUNT OVER--------------------------
            if(_model.CountParaStart == true) //Parallel �ð� ī��Ʈ ���� ���¿���
            {
                if(_model.CountParallelTime_S >= _model.ParallelTime) //Parallel �ð��� ������
                {
                    _model.RunStatus = false; //���� ����
                    _model.CountParaStart = false; //Parallel �ð� ī��Ʈ ����
                    _model.CountParallelTime_S = 0; //Parallel �ð� �ʱ�ȭ
                }
            }

            _model.OldLowpress = _model.Lowpress;
            _model.OldRunStatus = _model.RunStatus;
        }
        #endregion

        #region ComFailErrorFlag ����
        /// <summary>
        /// ��� ���� �÷��� ó��
        /// </summary>
        private void ComFailErrorFlag()
        {
            if (_model == null) return;

            //--------------------------COM FAIL - CAN ID 1--------------------------
            //Error_Flag : False�� Com_Normal, True�� Com_Error
            //Error_Flag : False�� Com_Error, True�� Com_Normal
            if (_model.CountComFault1_S > _model.ComFault_S && _model.Error_Flag1 == Com_Normal && CAN_ID != 1)
            //ID1�κ��� ���� �ð�(���� 1��) �̻� �������� �� ���� ���Ѵ�. �ڱ� �ڽſ� ���� ������ ���� ����
            {
                _model.Error_Flag1 = Com_Error;

                if(_model.STAND_BY_LAMP == true && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1)
                //StandBy ��忡�� ��� ���� �߻� �� �ڵ� ��ȯ
                //���� ���Ĺ��� �����̰� ���������� �� ID1�� RUN ���¿��µ�, ID1�� ����� ����� -> ID1�� ���峵�ٰ� ���� ���� RUN���� �ڵ� ��ȯ
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data1 = new byte[8]; //���� ������ �ʱ�ȭ. ���� ���� ������ 0���� ��� �ʱ�ȭ.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S) //? �̻��� �����̴�
                //�ʱ�ȭ ����(ComInit_S��)�� �������� ������ ���� ������ ���� �ʵ��� �ϴ� ���� ����.
            {
                //�ٽ� ���������� ���ŵǰ� �ִٰ� �Ǵ��ϰ� Error_Flag�� False�� �ٲ�
                if (_model.CountComFault1_S <= _model.ComFault_S && _model.Error_Flag1 == Com_Error && CAN_ID != 1) _model.Error_Flag1 = Com_Normal; //Com_Normal
            }

            //ID1�� MANUAL ���(0) ���, ���Ĺ���/���� ��ȯ�� �Ұ����ϴٰ� ���� ��ǻ� ���� �Ұ�(Com_Error)�� ���
            if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 1) _model.Error_Flag1 = Com_Error; //MANUAL_MODE -> Com_Error
            //ID1�� STBY ���(1) ��� ��ȯ �����̹Ƿ� Com_Normal�� ����
            else if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 1) _model.Error_Flag1 = Com_Normal; //STBY_MODE -> Com_Normal

            //--------------------------COM FAIL - CAN ID 2--------------------------
            if (_model.CountComFault2_S > _model.ComFault_S && _model.Error_Flag2 == Com_Normal && CAN_ID != 2)
            {
                _model.Error_Flag2 = Com_Error; //Com_Error

                if (_model.STAND_BY_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data2 = new byte[8]; //���� ������ �ʱ�ȭ. ���� ���� ������ 0���� ��� �ʱ�ȭ.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S) 
            {
                if (_model.CountComFault2_S <= _model.ComFault_S && _model.Error_Flag2 == Com_Error && CAN_ID != 2) _model.Error_Flag2 = Com_Normal; //Com_Normal
            }
            if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 2) _model.Error_Flag2 = Com_Error; //MANUAL_MODE -> Com_Error
            //ID1�� STBY ���(1) ��� ��ȯ �����̹Ƿ� Com_Normal�� ����
            else if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 2) _model.Error_Flag2 = Com_Normal; //STBY_MODE -> Com_Normal

            //--------------------------COM FAIL - CAN ID 3--------------------------
            if (_model.CountComFault3_S > _model.ComFault_S && _model.Error_Flag3 == Com_Normal && CAN_ID != 3)
            {
                _model.Error_Flag3 = Com_Error; //Com_Error

                if (_model.STAND_BY_LAMP == true && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data3 = new byte[8]; //���� ������ �ʱ�ȭ. ���� ���� ������ 0���� ��� �ʱ�ȭ.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S)
            {
                if (_model.CountComFault3_S <= _model.ComFault_S && _model.Error_Flag3 == Com_Error && CAN_ID != 3) _model.Error_Flag3 = Com_Normal; //Com_Normal
            }
            if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 3) _model.Error_Flag3 = Com_Error; //MANUAL_MODE -> Com_Error
            //ID1�� STBY ���(1) ��� ��ȯ �����̹Ƿ� Com_Normal�� ����
            else if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 3) _model.Error_Flag3 = Com_Normal; //STBY_MODE -> Com_Normal
        }

        #endregion

        /// <summary>
        /// CAN�޽��� ���� ó��
        /// </summary>
        private void CanRevMsg()
        {
            _model.CountComFault1_S = 0;
            _model.CountComFault2_S = 0;
            _model.CountComFault3_S = 0;
        }

        #region StandByLampProc ����
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
                    case 3: // StandBy_3(3�� ���� ����, ���� 2�� RUN) ��Ȳ���� ���� ���� ���� ���Ĺ��������� ���ϴ� ��Ģ
                        // ��� �� ���� ���� �� ����
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                        if (_model.Overload == false &&
                            ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                        {
                            if (_model.ResetButton == true)
                            {
                                //�ٸ� ��ġ �� RUN�̸鼭 STBY ������ ������ �ִٸ�, ���� ��� �����̶�� �� �� �ְ�, ���� ��ư ���� �������� ���� ���Ĺ��̷� ��ȯ
                                _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            }
                        }
                        // ��� �� ���Ĺ��� ���� �� ����
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                        else if (_model.RUN_LAMP && _model.STAND_BY_LAMP) //���� STBY��, RUN �� (���� ������ ��Ȳ)
                            //�� �� �ٸ� ��ġ�� STOP ���¿��� Reset ��ư�� �����ٸ�, STBY ������ ���� ���� �������� ��ȯ�ȴ�.
                        {
                            if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1) ||
                                (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1) ||
                                (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1))
                            {
                                _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            }
                        }
                        // AFTER POWER RECOVERY
                        // ���� ���� ���� �ʱ� ����
                        // ���� ������ �� ������ �̹� ������(RUN & STBY ���� OFF)�� + �����Ĺ���(STOP & STBY ���� ON)���� ���� ¦�� �̷� ���� �����̸�, ���� STBY ������ ��(OFF). �̹� ������ ����-���Ĺ��� �ֿ��� ȥ���� ���� �ʵ���?
                        // ���� ���� ���, �̹� ���� ����+���Ĺ��̷� �ڸ�������� ��(�� ��°)�� STBY ���� OFF�� ���� ȥ�� ����.
                        else if ((CAN_ID == 1 &&
                                  ((_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1))) ||
                                 (CAN_ID == 2 &&
                                  ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))) ||
                                 (CAN_ID == 3 &&
                                  ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))))
                        {
                            _model.STAND_BY_LAMP = false;
                        }

                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        // �ٸ� ��ġ �� STBY ���� RUN ���̸鼭 STBY ������ ���� ��ġ�� �ִٸ� �� ��ġ�� �����̴�.
                        // ���� ���� �����̰� �����ε� ���� �ʱ�ȭ �÷��׵� �ƴϸ�, ���� ���Ĺ��̷� ��ȯ. ���� ������ ���� ���Ĺ��̷� ����.
                        // ���� ���ǹ��� ���� ���ϰ� ����� �����ϱ�, ����-���Ĺ��� ���� ���� ���ٴ� ��?
                        else if (((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) ||
                                  (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0) ||
                                  (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)) &&
                                 _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                        // ������ ���� ����(STOP & �����ε� ����) �ϸ�, �� STBY ������ ����. ������ �������� ���Ĺ��̵� ���������� �ϴ� ��?
                        else if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1) ||
                                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1) ||
                                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1))
                        {
                            _model.STAND_BY_LAMP = false;
                        }
                        // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                        // ������ ���� ���� ��ȯ�Ǿ ���� ���̸� �� STBY ������ ����. ���� ������ ���� ���Ĺ��� ���� ��Ȱ��ȭ.
                        else if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0) ||
                                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 0) ||
                                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 0))
                        {
                            _model.STAND_BY_LAMP = false;
                        }
                        break;

                    case 5: // StandBy_3_1RUN
                        HandleStandBy_3_1RUN();
                        break;

                    case 2: // StandBy_2
                    case 1: // StandBy_3to2
                    case 6: // StandBy_3to2_1RUN
                        HandleStandBy_2_StandBy_3to2_StandBy_3to2_1RUN();
                        break;
                }
            }
            else // MANUAL_MODE
            {
                _model.STAND_BY_LAMP = false;
            }

            // StandBy ��ȣ ���
            // ������ġ������ ����
        }

        /// <summary>
        /// StandBy_3_1RUN ���� ó�� (3�� ���� ����, 1�븸 Run)
        /// 3�� �����ε� 1�븸 RUN ���� �� standby ��Ȱ�� �������� ���� ���ϰ�, �ߺ� STBY �����̳� ���۵��� �����ϴ� ����
        /// </summary>
        private void HandleStandBy_3_1RUN()
        {
            // ��ġ ID�� �б� ó��
            // 1���� ������ ���
            if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) //�� ���� �����ϸ� 1���� ����
            {
                if (CAN_ID == 2)
                    //1���� ������ ��� ��� 1������ 2��, 2������ 3������ ���� ������ ����. 3���� �����ε峪 ���� ������ ������.
                    //�� �밡 ���ÿ� STBY�� �Ѵ� ���� ��Ȳ�� ���� ���� ��ġ
                {
                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)
                    // ����(1) + �ٸ� ����(3)�� STBY�� �ƴ� ���� STBY�� �ƹ��� ����
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) //�̹� �ٸ� ��ġ�� ���Ĺ��� ��
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 3)
                {
                    // �����ε尡 ���� ������ ������ �̷��, ���� �ð�(CountStandByCheck_mS) ���� �� ������
                    if (_model.Overload == true && _model.RX_Data2[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // ī���� ����
                    }
                    //���� �����ε� �ƴϰ� �ٸ� ��ġ(2��)�� �����ε��̰ų�, �ٸ� ��ġ�� STBY ������ ���� ���¶��
                    else if (_model.Overload == false && (_model.RX_Data2[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >=5 )
                    {
                        if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                //���Ĺ��� ��� �ְ� �� ���°� �����ϸ� ���� ���Ĺ����ϰڴ�.
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }
                    //2�� ��ġ�� ���Ĺ��� ���̸� ���� ���Ĺ��� ����
                    if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false; // ������ MANUAL�̸� ���� STBY ����
                }
            }
            //2���� ������ ���
            else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
            {
                if (CAN_ID == 3)
                {
                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 1)
                {
                    if (_model.Overload == true && _model.RX_Data3[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // ī���� ����
                    }
                    else if (_model.Overload == false && (_model.RX_Data3[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >= 5)
                    {
                        if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }

                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            //3���� ������ ���
            else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
            {
                if (CAN_ID == 1)
                {
                    if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    else if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 2)
                {
                    if (_model.Overload == true && _model.RX_Data1[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // ī���� ����
                    }
                    else if (_model.Overload == false && (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >= 5)
                    {
                        if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }

                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            // �ƹ��� ������ �ƴ� ��� (3�� ��� STOP �����̰ų�, 2�� �̻��� RUN ���̰ų�, RUN ���� ��ġ�� ��� STBY ���� ON �����̰ų� ���)
            //StandBy_3_1RUN ��Ȳ���� 1�뵵 ������ �ƴ� ����, 3�� ��� STOP �����̰ų�, 2�� �̻��� RUN ���̰ų�, RUN ���� ��ġ�� ��� STBY ���� ON �����̰ų� ���
            //StandBy_3_1RUN �б� �ȿ��� ���� ���� �ĺ� ���̽��� �ش����� ���� �� ������ �κ�, ȥ�� �������� ���ΰ� ���Ĺ��̸� �����ϴ� ������ �����Ѵ�.
            else
            {
                _model.CountStandByCheck_mS = 0; // ī���� ����

                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                //��� �� ���� ���� ���� �� ���� ��Ģ
                // RUN�̸鼭 STBY ������ ���� ��ġ�� �ִٸ� �װ� ���Ĺ��̰� �پ ���� ���� ���� ��Ȳ (��� �� ���� ����)
                //�̶� ���� Reset�� ������ ���� STBY�� �ٲ㼭 ������ ����
                if (_model.Overload == false && ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                                        (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                                        (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                {
                    if (_model.ResetButton == true)
                    {
                        _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)

                        //if (_model.StandBy_3_1RUN_Flag == true)
                        //{
                        //    _model.StandBy_3_1RUN_Flag = false;
                        //}
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                // ���� STBY�� �پ RUN ���� ��Ȳ ó��
                else if (_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true)
                {
                    if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1) ||
                        (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1) ||
                        (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1))
                    {
                        _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        //if (_model.StandBy_3_1RUN_Flag == true)
                        //{
                        //    _model.StandBy_3_1RUN_Flag = false;
                        //}
                    }
                }

                // ��ġ ID�� �߰� ó��
                HandleDeviceSpecificConditions();
            }
        }

        /// <summary>
        /// ��ġ ID�� ���� Ư�� ���� ó��
        /// ���� STBY ���� ���� ������ ��, �ٸ� ��ġ���� ���¸� ���� ���� STBY ������ ���� ���ǵ�, �ߺ� STBY/����/������ ����
        /// </summary>
        private void HandleDeviceSpecificConditions()
        {
            if (CAN_ID == 1 && _model.STAND_BY_LAMP)
            {
                //���� ��� �����ε尡 ���� = ��������.�̶�
                //���(��: 2��)�� ���� ��� ����(STOP &STBY ���)**�� �̹� �ڸ� ��Ұ� ���� RUN�� �ƴ� �� STBY�� �ʿ� ����(�ߺ� ��� ����)
                //�Ǵ� ��밡 MANUAL�� RUN �ڵ� ���� �Ұ��� �� STBY�� ����(�ڵ� ��� ���ġ ����)
                if (_model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if(_model.CountOverload_S > 1)
                {
                    if(_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    //�����ϰ� ��ӵǸ� �÷����� ���� ����.�׷��� ������ �� ���������� ��� STBY�� ������ ����
                    //������ STOP &Overload OFF & STBY ���� �����߰�, ���� RUN �ƴ� �� �� STBY OFF
                    //�Ǵ� ���� �� Overload ON�ε� ���� RUN �ƴ� �� �� STBY OFF
                    //ȥ���⿡ ���ߺ� ���/ ��¥ ��⡱�� ���� �ý����� �� �򰥸��� ����� �� ����.
                    else if (_model.RX_Data2[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false) //  Ư���ϰ� 2���� 3���� ���ϳ�
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
            else if (CAN_ID == 2 && _model.STAND_BY_LAMP == true)
            {
                if (_model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if (_model.CountOverload_S > 1)
                {
                    if( _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false) //  Ư���ϰ� 1���� 3���� ���ϳ�
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
            else if (CAN_ID == 3 && _model.STAND_BY_LAMP == true)
            {
                if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if (_model.CountOverload_S > 1)
                {
                    if( _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
        }

        /// <summary>
        /// StandBy_2(2�븸 ���� �) �Ǵ� StandBy_3to2 �Ǵ� StandBy_3to2_1RUN ���� ó��
        /// </summary>
        private void HandleStandBy_2_StandBy_3to2_StandBy_3to2_1RUN()
        {
            _model.StandBy_2_Flag = true;
            // ��ġID�� ���� ���� ó��
            if (_model.CountStandBy_2_mS >= 5)
            {
                if (_model.Error_Flag1 == Com_Normal)
                {
                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                    if (_model.Overload == false && (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))
                    {
                        if (_model.ResetButton == true)
                        {
                            _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }
                        }
                    }

                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                    else if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RUN_LAMP == true && _model.StandByLamp == true)
                    {
                        if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1)
                        {
                            _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }

                        }
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                    else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.InitFlag == false && _model.STOP_LAMP == true)
                    {
                        _model.STAND_BY_LAMP = true;
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                    else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                    else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                else if (_model.Error_Flag2 == Com_Normal)
                {
                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                    if (_model.Overload == false && (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1))
                    {
                        if (_model.ResetButton == true)
                        {
                            _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }
                        }
                    }
                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                    // Error_Flag1 ���� ���� �� �ٸ���
                    else if (_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true)
                    {
                        if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1)
                        {
                            _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }
                        }
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.InitFlag == false && _model.STOP_LAMP == true)
                    {
                        _model.STAND_BY_LAMP = true;
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                    else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 0)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (_model.Error_Flag3 == Com_Normal)
                {
                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                    if (_model.Overload == false && (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1))
                    {
                        if (_model.ResetButton == true)
                        {
                            _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }
                        }
                    }
                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                    else if (_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true)
                    {
                        if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1)
                        {
                            _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            if (_model.StandBy_3_1RUN_Flag == true)
                            {
                                _model.StandBy_3_1RUN_Flag = false;
                            }
                        }
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.InitFlag == false && _model.STOP_LAMP == true)
                    {
                        _model.STAND_BY_LAMP = true;
                    }
                    // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                    else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 0)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Run ��û ���� ó�� (���� Main ���� �� ��)
        /// </summary>
        private void SendRunReq()
        {
            if(_model == null) return;

            //ModeStatus == true -> STBY_MODE
            if (_model.ModeStatus == true && _model.ComStatus != COMStatusIndices.NoConnection)
            {
                // ���� ���¿��� ���� ���� ��� Run ��û ����
                if (_model.Lowpress && _model.RUN_LAMP == true &&
                    _model.CountBuildUpTime_S == _model.CountBuildUpTime &&
                    _model.RUN_req == false)
                {
                    _model.RUN_req = true;
                }
                // ������ ���¿��� Run ��û ����
                else if (_model.Stop_Overload == true && _model.Overload == true &&
                        _model.STBY_Overload == false &&
                        _model.CountRunReq_S == _model.RunReq_S &&
                        _model.RUN_req == false)
                {
                    _model.RUN_req = true;
                }
                // ���� ���·� ���� �� Run ��û ���
                else if (_model.Overload == false && _model.Lowpress == false && _model.RUN_LAMP == false &&
                        (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1) &&
                        _model.RUN_req == true)
                {
                    _model.RUN_req = false;
                }
                // ���� ���¿��� �ٸ� ������ ���� ���� ��� Run ��û ���
                else if (_model.Lowpress &&
                        (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1) &&
                        _model.RUN_LAMP == false && _model.RUN_req == true)
                {
                    _model.RUN_req = false;
                }
                // ������ ���°� ���ӵ� ��� Run ��û ���
                else if (_model.Overload == true && _model.CountRunReq_S > _model.RunReq_S && _model.RUN_req == true)
                {
                    _model.RUN_req = false;
                }
            }
        }

        /// <summary>
        /// Run ��û ���� ó�� (���� STBY ���� �� ��)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (_model == null) return;

            if (_model.Overload == false && _model.STAND_BY_LAMP == true)
            {
                // Run ��û�� ������ ����
                if (_model.Request_Flag == false &&
                    (_model.RX_Data1[CANDataIndices.RUN_REQ] == 1 || _model.RX_Data2[CANDataIndices.RUN_REQ] == 1 || _model.RX_Data3[CANDataIndices.RUN_REQ] == 1))
                {
                    _requestFlag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[CANDataIndices.RUN_REQ] == 0 && _model.RX_Data2[CANDataIndices.RUN_REQ] == 0 && _model.RX_Data3[CANDataIndices.RUN_REQ] == 0)
                {
                    _requestFlag = false;
                }
            }
        }


        /// <summary>
        /// StandBy ���� �˶� ó��
        /// </summary>
        private void StbyStartAlarm()
        {
            if (_model == null) return;
            // ���� ��ġ������ �������� ����
        }

        /// <summary>
        /// ���, ���� ���¿� ���� ���� ���� ó��
        /// </summary>
        /// <param name="comStatus"></param>
        private void ConnectProc(int comStatus)
        {
            if(comStatus == COMStatusIndices.NoConnection)
            {
                _model.COMM_FAULT_LAMP = true;
                _model.ComFailLamp_Flag = false;

                _model.CountStandByCheck_mS = 0;

                _model.StandBy_2_Flag = false;
            }
            //3��->2��� �ٲ�� ���� ��Ȳ
            else if(comStatus == COMStatusIndices.StandBy_3to2 || comStatus == COMStatusIndices.StandBy_3to2_1RUN)
            {
                //���� ������
                if(_model.CountComFailLamp_mS < 5)
                {
                    _model.COMM_FAULT_LAMP = true;
                }
                else if(5 <= _model.CountComFailLamp_mS && _model.CountComFailLamp_mS < 10)
                {
                    _model.COMM_FAULT_LAMP = false;
                }
                else if(_model.CountComFailLamp_mS >= 10)
                {
                    _model.CountComFailLamp_mS = 0;
                }
                _model.ComStatus_Flag = true; // �޸� ���¸� StandBy_3to2�� ����?

                _model.CountStandByCheck_mS = 0;

                if(_model.STOP_PB_I == false && _model.CountResetButton_S >= 2)
                {
                    _model.ComFailLamp_Flag = true;
                    _model.ComStatus = COMStatusIndices.StandBy_2; // ComStatus�� StandBy_2�� ���� ����
                }
            }
            else if(comStatus == COMStatusIndices.StandBy_2) //2 StandBy
            {
                _model.COMM_FAULT_LAMP = false;
                _model.ComFailLamp_Flag = false;
                _model.CountStandByCheck_mS = 0;
            }
            else if(comStatus == COMStatusIndices.StandBy_3) // 3 StandBy 2Run
            {
                _model.COMM_FAULT_LAMP = false;
                _model.ComFailLamp_Flag = false;
                _model.CountStandByCheck_mS = 0;

                _model.StandBy_2_Flag = false;
            }
            else if(comStatus == COMStatusIndices.Manual) //ManualMode -> NoConnection
            {
                _model.COMM_FAULT_LAMP = false;
                _model.ComFailLamp_Flag = false;

                _model.CountStandByCheck_mS = 0;

                _model.StandBy_2_Flag = false;
            }
            else if(comStatus == COMStatusIndices.StandBy_3_1RUN) // 3 StandBy 1Run
            {
                _model.COMM_FAULT_LAMP = false;
                _model.ComFailLamp_Flag = false;

                _model.ComStatus_Flag = true; // �޸� ���¸� StandBy_3_1RUN�� ����?
                _model.StandBy_2_Flag = false;
            }

            //ComFailLamp_Flag�� true�̸� ����� �������� ��Ȳ�ΰǰ�?
            //���� Ȯ�� ���� �� �ֱ� ���ȸ� ���� ���� ������ ����?
            if(_model.ComFailLamp_Flag == true)
            {
                _model.COMM_FAULT_LAMP = false;
            }
        }

        #region ConnectFunction ����
        ////////////////////////////ConnectFunction ���� ����////////////////////////////////////
        /// <summary>
        /// ���� ���� �Լ�
        /// ComStatus �� �������ش�.
        /// ConnectionFunction -> ����
        /// ConnectProc -> ����
        /// </summary>
        private void ConnectFunction()
        {
            if (_model == null) return;

            // �� �Լ��� �ٸ� CAN ��� ���¿� ���� ���� ��ġ�� ���� ���¸� ����
            // ���� ����
            // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
            // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

            switch (CAN_ID)
            {
                case 1:
                    HandleConnectDevice1();
                    break;
                case 2:
                    HandleConnectDevice2();
                    break;
                case 3:
                    HandleConnectDevice3();
                    break;
            }
        }

        /// <summary>
        /// ��ġ ID 1�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice1()
        {
            // MANUAL ����̸� NO CONNECT
            if (_model.ModeStatus == false)
            {
                _model.ComStatus = COMStatusIndices.Manual; // Manual
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag2 == Com_Error && _model.Error_Flag3 == Com_Error)
            {
                _model.ComStatus = COMStatusIndices.NoConnection; // NoConnection
            }

            // 3�� ���� (StandBy_3)
            if (_model.Error_Flag2 == Com_Normal && _model.Error_Flag3 == Com_Normal)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                // 2 RUN or 1 RUN
                if (_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if(_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if(_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if(_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }

                // 2 STAND BY
                else if((_model.Error_Flag2 == Com_Normal && _model.Error_Flag3 == Com_Error) || (_model.Error_Flag2 == Com_Error && _model.Error_Flag3 == Com_Normal))
                {
                    if (_model.ComStatus == COMStatusIndices.StandBy_3) // ������ StandBy_3�̾��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2; // StandBy_3to2
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_2) // �̹� StandBy_2 ���¶��
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_2; // ����
                    }
                    else if (_model.ComStatus == COMStatusIndices.NoConnection || _model.ComStatus == COMStatusIndices.Manual) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                    {
                        //_model.ComStatus = _model.ComStatus_Flag ? COMStatusIndices.StandBy_3to2 : COMStatusIndices.StandBy_2; // �޸� ���¿� ���� ����
                        if(_model.ComStatus_Flag == true) _model.ComStatus = COMStatusIndices.StandBy_3to2;
                        else if(_model.ComStatus_Flag == false) _model.ComStatus = COMStatusIndices.StandBy_2;
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_3_1RUN) // StandBy_3_1RUN ���¿��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2_1RUN; // StandBy_3to2_1RUN
                        _model.StandBy_3_1RUN_Flag = true;
                    }
                }
            }
        }

        /// <summary>
        /// ��ġ ID 2�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice2()
        {
            // MANUAL ����̸� NO CONNECT
            if (_model.ModeStatus == false)
            {
                _model.ComStatus = COMStatusIndices.Manual; // Manual
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag1 == Com_Error && _model.Error_Flag3 == Com_Error)
            {
                _model.ComStatus = COMStatusIndices.NoConnection; // NoConnection
            }

            // 3�� ���� (StandBy_3)
            if (_model.Error_Flag1 == Com_Normal && _model.Error_Flag3 == Com_Normal)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                // 2 RUN or 1 RUN
                if (_model.RUN_LAMP == true && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == true && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == true && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }

                // 2 STAND BY
                else if ((_model.Error_Flag1 == Com_Normal && _model.Error_Flag3 == Com_Error) || (_model.Error_Flag1 == Com_Error && _model.Error_Flag3 == Com_Normal))
                {
                    if (_model.ComStatus == COMStatusIndices.StandBy_3) // ������ StandBy_3�̾��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2; // StandBy_3to2
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_2) // �̹� StandBy_2 ���¶��
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_2; // ����
                    }
                    else if (_model.ComStatus == COMStatusIndices.NoConnection || _model.ComStatus == COMStatusIndices.Manual) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                    {
                        //_model.ComStatus = _model.ComStatus_Flag ? COMStatusIndices.StandBy_3to2 : COMStatusIndices.StandBy_2; // �޸� ���¿� ���� ����
                        if (_model.ComStatus_Flag == true) _model.ComStatus = COMStatusIndices.StandBy_3to2;
                        else if (_model.ComStatus_Flag == false) _model.ComStatus = COMStatusIndices.StandBy_2;
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_3_1RUN) // StandBy_3_1RUN ���¿��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2_1RUN; // StandBy_3to2_1RUN
                        _model.StandBy_3_1RUN_Flag = true;
                    }
                }
            }
        }

        /// <summary>
        /// ��ġ ID 3�� ���� ���� ���� ó��
        /// </summary>
        private void HandleConnectDevice3()
        {
            // MANUAL ����̸� NO CONNECT
            if (_model.ModeStatus == false)
            {
                _model.ComStatus = COMStatusIndices.Manual; // Manual
            }

            // ���� ���� (NoConnection)
            if (_model.Error_Flag2 == Com_Error && _model.Error_Flag1 == Com_Error)
            {
                _model.ComStatus = COMStatusIndices.NoConnection; // NoConnection
            }

            // 3�� ���� (StandBy_3)
            if (_model.Error_Flag2 == Com_Normal && _model.Error_Flag1 == Com_Normal)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // ���� ���¿� ���� ComStatus ����
                // 2 RUN or 1 RUN
                if (_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3;
                }
                else if (_model.RUN_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }
                else if (_model.RUN_LAMP == false && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.ComStatus = COMStatusIndices.StandBy_3_1RUN;
                }

                // 2 STAND BY
                else if ((_model.Error_Flag2 == Com_Normal && _model.Error_Flag1 == Com_Error) || (_model.Error_Flag2 == Com_Error && _model.Error_Flag1 == Com_Normal))
                {
                    if (_model.ComStatus == COMStatusIndices.StandBy_3) // ������ StandBy_3�̾��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2; // StandBy_3to2
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_2) // �̹� StandBy_2 ���¶��
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_2; // ����
                    }
                    else if (_model.ComStatus == COMStatusIndices.NoConnection || _model.ComStatus == COMStatusIndices.Manual) // ���� ���� �Ǵ� Manual ���¿��ٸ�
                    {
                        //_model.ComStatus = _model.ComStatus_Flag ? COMStatusIndices.StandBy_3to2 : COMStatusIndices.StandBy_2; // �޸� ���¿� ���� ����
                        if (_model.ComStatus_Flag == true) _model.ComStatus = COMStatusIndices.StandBy_3to2;
                        else if (_model.ComStatus_Flag == false) _model.ComStatus = COMStatusIndices.StandBy_2;
                    }
                    else if (_model.ComStatus == COMStatusIndices.StandBy_3_1RUN) // StandBy_3_1RUN ���¿��ٸ�
                    {
                        _model.ComStatus = COMStatusIndices.StandBy_3to2_1RUN; // StandBy_3to2_1RUN
                        _model.StandBy_3_1RUN_Flag = true;
                    }
                }
            }
        }
        #endregion

        #endregion

        #region �ܺ� �������̽� �޼���
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
        /// desperate
        /// </summary>
        private void NotifyStateChanged()
        {
            //if (_model == null) return;

            //DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            //{
            //    DeviceId = _deviceId,
            //    IsRunning = _model.RunStatus,
            //    IsStandByMode = _model.ModeStatus,
            //    IsHeating = _model.HeatStatus,
            //    IsSTAND_BY_LAMP = _model.STAND_BY_LAMP,
            //    TXLowpress  = _model.TXLowpress
            //});
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
    /// desperate
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public bool IsRunning { get; set; }
        public bool IsStandByMode { get; set; }
        public bool IsHeating { get; set; }
        public bool IsSTAND_BY_LAMP { get; set; }
        public bool TXLowpress { get; set; }
    }
}