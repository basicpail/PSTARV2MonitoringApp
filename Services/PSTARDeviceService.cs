using System;
using System.Timers;
using PSTARV2MonitoringApp.Models;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// ���� �߿��� ������ ������ PSTAR ���� ��
    /// </summary>
    public class PSTARDeviceService : IDisposable
    {
        #region ����
        // ��ġ ID�� CAN ID
        private readonly string _deviceId;
        private readonly uint _canId;

        // CAN ���� Ÿ�̸� (���� Ÿ�̸Ӵ� ����)
        private readonly Timer _transmitTimer;

        // �� ���۷��� (��ġ �� ����)
        private PSTARDeviceModel _model;
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

            // CAN ���� Ÿ�̸� (300ms �ֱ� - PSTARFW.c�� CanDelay_mS)
            _transmitTimer = new Timer(300);
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
            // �� ���°� ����Ǹ� ���� ���� ����
            ExecutePumpLogic();

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
            ExecutePumpLogic();
        }

        /// <summary>
        /// �ùķ��̼� ����
        /// </summary>
        public void StopSimulation()
        {
            _transmitTimer.Stop();
        }

        /// <summary>
        /// ���� ���� ���� (���� OnLogicTimerElapsed�� ����)
        /// </summary>
        private void ExecutePumpLogic()
        {
            if (_model == null) return;

            // PSTARFW.c�� ���� ���� ���� ����
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
            // KeyProc, OverloadProc, LowpressProc �� �Է� ó�� ���� ����
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

            // LowpressProc ���� ����
            _model.TxLowpressInternal = lowpressStatus;
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
            if (_model == null) return;

            if (!_model.Overload && _model.StandByLamp)
            {
                if (!_model.Request_Flag &&
                    (_model.RX_Data1[4] == 1 || _model.RX_Data2[4] == 1 || _model.RX_Data3[4] == 1))
                {
                    _model.Request_Flag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[4] == 0 && _model.RX_Data2[4] == 0 && _model.RX_Data3[4] == 0)
                {
                    _model.Request_Flag = false;
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
            if (_model == null) return;

            // StandByLampProc ���� ���� (�ſ� ������ ����)
            // ����ȭ�� ���� ����
            //if (modeStatus) // STBY_MODE
            //{
            //    _model.StandByLamp = true;
            //}
            //else // MANUAL_MODE
            //{
            //    _model.StandByLamp = false;
            //}
        }
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
                    if (!_model.ModeStatus) // MANUAL_MODE
                    {
                        _model.RunStatus = true;

                        // ���� ���� �� ���� ����
                        ExecutePumpLogic();
                    }
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
            ExecutePumpLogic();
        }

        /// <summary>
        /// ��� ��ư ���� ó��
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            //_model.ModeStatus = !_model.ModeStatus;
            _model.IsManualMode = !_model.IsManualMode; // Manual ���� toggle

            // ���� ���� �� ���� ����
            ExecutePumpLogic();
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
            ExecutePumpLogic();
        }

        /// <summary>
        /// ������ ���� ����
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            if (_model == null) return;

            _model.Overload_I = isOverload;

            // ���� ���� �� ���� ����
            ExecutePumpLogic();
        }

        /// <summary>
        /// ���� ���� ����
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            if (_model == null) return;

            _model.Lowpress_I = isLowPressure;

            // ���� ���� �� ���� ����
            ExecutePumpLogic();
        }

        /// <summary>
        /// ���� ���� �˸�
        /// </summary>
        private void NotifyStateChanged()
        {
            if (_model == null) return;

            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                //TODO���� ��ġ ���� ������ ���缭 �����ؾ��ѵ�
                //DeviceId = _deviceId,
                //IsRunning = _model.RunStatus,
                //IsStandByMode = _model.ModeStatus,
                //IsHeating = _model.HeatStatus,
                //IsStandByLamp = _model.StandByLamp
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