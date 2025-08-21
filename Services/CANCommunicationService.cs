using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// ���� CAN ����� ����ϴ� ����
    /// </summary>
    public class CANCommunicationService
    {
        private static CANCommunicationService _instance;
        public static CANCommunicationService Instance => _instance ??= new CANCommunicationService();

        private CancellationTokenSource _cancellationTokenSource;
        private CANSettings _settings;
        private object _canInterface; // ���� CAN �������̽� ��ü

        // CAN ������ ���� �̺�Ʈ
        public event EventHandler<CANDataReceivedEventArgs> DataReceived;
        public event EventHandler<string> ConnectionStatusChanged;
        public event EventHandler<string> ErrorOccurred;
        // ��� CAN ������ �̺�Ʈ (���� �۽� ������ ����)
        public event EventHandler<CANDataReceivedEventArgs> AllCANDataReceived;
        // �۽� �̺�Ʈ �߰�
        public event EventHandler<CANTransmitEventArgs> DataTransmitted;

        private readonly Queue<CANFrame> _transmittedFrames = new Queue<CANFrame>(10); // �ִ� 10�� ������ ����
        private readonly object _queueLock = new object();

        // �������� ť�� �߰��� �� �߻��ϴ� �̺�Ʈ
        private event EventHandler FrameAddedToQueue;

        public CANSettings Settings => _settings ??= new CANSettings();
        public bool IsConnected => _settings?.IsConnected ?? false;

        private CANCommunicationService()
        {
        }

        /// <summary>
        /// CAN ��� ����
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (IsConnected) return true;

            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                ConnectionStatusChanged?.Invoke(this, "���� ��...");
                
                // CAN �������̽� �ʱ�ȭ
                var success = await InitializeCANInterface();
                if (!success)
                {
                    ConnectionStatusChanged?.Invoke(this, "���� ����");
                    return false;
                }
                
                Settings.IsConnected = true;
                ConnectionStatusChanged?.Invoke(this, "�����");
                
                // ��׶��忡�� ������ ���� ����
                _ = Task.Run(async () => await ReceiveDataLoop(_cancellationTokenSource.Token));
                
                return true;
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"���� ����: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// CAN ��� ����
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsConnected) return;

            _cancellationTokenSource?.Cancel();
            
            try
            {
                // CAN �������̽� ����
                await CleanupCANInterface();
                
                Settings.IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, "���� ������");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"���� ���� ����: {ex.Message}");
            }
        }

        /// <summary>
        /// CAN �������̽� �ʱ�ȭ
        /// </summary>
        private async Task<bool> InitializeCANInterface()
        {
            try
            {
                switch (Settings.InterfaceType.ToUpper())
                {
                    case "PCAN":
                        return await InitializePCANInterface();
                    case "VECTOR":
                        return await InitializeVectorInterface();
                    case "SOCKETCAN":
                        return await InitializeSocketCANInterface();
                    default:
                        // �׽�Ʈ ��� (���� �ϵ���� ���� �׽�Ʈ)
                        return await InitializeTestInterface();
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"CAN �������̽� �ʱ�ȭ ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PCAN �������̽� �ʱ�ȭ
        /// </summary>
        private async Task<bool> InitializePCANInterface()
        {
            try
            {
                // PCAN ���̺귯�� ��� ����
                // Peak.Can.Basic ���̺귯���� �ʿ�
                
                // TPCANStatus result = PCANBasic.Initialize(
                //     TPCANHandle.PCAN_USBBUS1,
                //     TPCANBaudrate.PCAN_BAUD_500K,
                //     TPCANType.PCAN_TYPE_ISA,
                //     0,
                //     0);
                
                await Task.Delay(500); // �ʱ�ȭ �ùķ��̼�
                
                // ���� ���� ��:
                // return result == TPCANStatus.PCAN_ERROR_OK;
                
                Debug.WriteLine("PCAN �������̽� �ʱ�ȭ �Ϸ�");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PCAN �ʱ�ȭ ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vector �������̽� �ʱ�ȭ
        /// </summary>
        private async Task<bool> InitializeVectorInterface()
        {
            try
            {
                // Vector CANoe/CANalyzer ���̺귯�� ���
                await Task.Delay(500);
                Debug.WriteLine("Vector �������̽� �ʱ�ȭ �Ϸ�");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Vector �ʱ�ȭ ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// SocketCAN �������̽� �ʱ�ȭ (Linux��)
        /// </summary>
        private async Task<bool> InitializeSocketCANInterface()
        {
            try
            {
                // SocketCANSharp ���̺귯�� ���
                await Task.Delay(500);
                Debug.WriteLine("SocketCAN �������̽� �ʱ�ȭ �Ϸ�");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SocketCAN �ʱ�ȭ ����: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// �׽�Ʈ �������̽� �ʱ�ȭ (�ϵ���� ���� �׽�Ʈ)
        /// </summary>
        private async Task<bool> InitializeTestInterface()
        {
            await Task.Delay(100);
            Debug.WriteLine("�׽�Ʈ CAN �������̽� �ʱ�ȭ �Ϸ�");
            return true;
        }

        /// <summary>
        /// CAN �������̽� ����
        /// </summary>
        private async Task CleanupCANInterface()
        {
            try
            {
                switch (Settings.InterfaceType.ToUpper())
                {
                    case "PCAN":
                        // PCANBasic.Uninitialize(TPCANHandle.PCAN_USBBUS1);
                        break;
                    case "VECTOR":
                        // Vector ���� �ڵ�
                        break;
                    case "SOCKETCAN":
                        // SocketCAN ���� �ڵ�
                        break;
                }
                
                await Task.Delay(100);
                Debug.WriteLine("CAN �������̽� ���� �Ϸ�");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CAN �������̽� ���� ����: {ex.Message}");
            }
        }

        /// <summary>
        /// ������ ���� ����
        /// </summary>
        private async Task ReceiveDataLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var canFrame = await ReceiveCANFrame(cancellationToken);
                    
                    if (canFrame != null)
                    {
                        //Console.WriteLine($"���ŵ� CAN ������: ID=0x{canFrame.Id:X3}, Data={BitConverter.ToString(canFrame.Data).Replace("-", " ")}");
                        // ���ŵ� �����͸� �̺�Ʈ�� ����
                        DataReceived?.Invoke(this, new CANDataReceivedEventArgs(canFrame));
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CAN ���� ����: {ex.Message}");
                    ErrorOccurred?.Invoke(this, $"���� ����: {ex.Message}");
                    
                    // ���� �� ��� ��� �� ��õ�
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// CAN ������ ����
        /// </summary>
        private async Task<CANFrame> ReceiveCANFrame(CancellationToken cancellationToken)
        {
            try
            {
                switch (Settings.InterfaceType.ToUpper())
                {
                    case "PCAN":
                        return await ReceivePCANFrame(cancellationToken);
                    case "VECTOR":
                        return await ReceiveVectorFrame(cancellationToken);
                    case "SOCKETCAN":
                        return await ReceiveSocketCANFrame(cancellationToken);
                    default:
                        return await ReceiveTestFrame(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CAN ������ ���� ����: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// PCAN ������ ����
        /// </summary>
        private async Task<CANFrame> ReceivePCANFrame(CancellationToken cancellationToken)
        {
            // ���� PCAN ����
            /*
            TPCANMsg canMsg;
            TPCANTimestamp timestamp;
            
            TPCANStatus result = PCANBasic.Read(TPCANHandle.PCAN_USBBUS1, out canMsg, out timestamp);
            
            if (result == TPCANStatus.PCAN_ERROR_OK)
            {
                return new CANFrame
                {
                    Id = canMsg.ID,
                    Data = canMsg.DATA.Take(canMsg.LEN).ToArray(),
                    Timestamp = DateTime.Now,
                    IsExtended = (canMsg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0
                };
            }
            */
            
            await Task.Delay(100, cancellationToken);
            return null;
        }

        /// <summary>
        /// Vector ������ ����
        /// </summary>
        private async Task<CANFrame> ReceiveVectorFrame(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return null;
        }

        /// <summary>
        /// SocketCAN ������ ����
        /// </summary>
        private async Task<CANFrame> ReceiveSocketCANFrame(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return null;
        }

        /// <summary>
        /// �׽�Ʈ ������ ���� (�ùķ��̼�)
        /// </summary>
        private async Task<CANFrame> ReceiveTestFrame(CancellationToken cancellationToken)
        {
            CANFrame frame = null;

            try
            {
                // ť�� �������� ������ ��� ó��
                lock (_queueLock)
                {
                    if (_transmittedFrames.Count > 0)
                    {
                        frame = _transmittedFrames.Dequeue();
                        Console.WriteLine($"{DateTime.Now} �׽�Ʈ ������ ����(��� ó��): ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        return frame;
                    }
                }

                // ť�� ��������� �������� ���� ������ ���
                using var waitEvent = new SemaphoreSlim(0, 1);

                // �޽��� �߰� �̺�Ʈ �ڵ鷯
                void OnFrameAdded(object sender, EventArgs e)
                {
                    waitEvent.Release();
                }

                // �ӽ� �̺�Ʈ ����
                FrameAddedToQueue += OnFrameAdded;

                try
                {
                    // �������� �߰��ǰų� ��� ��û�� ���� ������ ���
                    // 1�� Ÿ�Ӿƿ� �߰� (������ ������� �ʵ���)
                    await waitEvent.WaitAsync(1000, cancellationToken);

                    // ť ��Ȯ��
                    lock (_queueLock)
                    {
                        if (_transmittedFrames.Count > 0)
                        {
                            frame = _transmittedFrames.Dequeue();
                            Console.WriteLine($"{DateTime.Now} �׽�Ʈ ������ ����(��Ȯ��): ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        }
                    }
                }
                finally
                {
                    // �̺�Ʈ ���� ����
                    FrameAddedToQueue -= OnFrameAdded;
                }
            }
            catch (OperationCanceledException)
            {
                // �۾� ��� �� null ��ȯ
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReceiveTestFrame ����: {ex.Message}");
            }

            return frame;
        }

        /// <summary>
        /// CAN �޽��� ����
        /// </summary>
        public async Task<bool> SendAsync(CANFrame frame)
        {
            if (!IsConnected) return false;

            try
            {
                // �۽� �̺�Ʈ �߻� (�߰��� �κ�)
                DataTransmitted?.Invoke(this, new CANTransmitEventArgs(frame));

                // ���۵� �������� ť�� ���� (�׽�Ʈ ����)
                if (Settings.InterfaceType.ToUpper() == "TEST")
                {
                    lock (_queueLock)
                    {
                        Console.WriteLine($"{DateTime.Now} �׽�Ʈ ������ ����: ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        // ť ũ�� ����
                        if (_transmittedFrames.Count >= 10)
                            _transmittedFrames.Dequeue();

                        // �� ������ �߰�
                        _transmittedFrames.Enqueue(new CANFrame
                        {
                            Id = frame.Id,
                            Data = frame.Data.ToArray(), // ������ ����
                            Timestamp = DateTime.Now,
                            IsExtended = frame.IsExtended
                        });

                        // ť�� �������� �߰��Ǿ����� �˸�
                        FrameAddedToQueue?.Invoke(this, EventArgs.Empty);
                    }
                }

                // ��� CAN ������ �̺�Ʈ �߻� (�߰��� �κ�)
                //AllCANDataReceived?.Invoke(this, new CANDataReceivedEventArgs(frame));
                
                switch (Settings.InterfaceType.ToUpper())
                {
                    case "PCAN":
                        return await SendPCANFrame(frame);
                    case "VECTOR":
                        return await SendVectorFrame(frame);
                    case "SOCKETCAN":
                        return await SendSocketCANFrame(frame);
                    default:
                        return await SendTestFrame(frame);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"���� ����: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendPCANFrame(CANFrame frame)
        {
            // ���� PCAN ���� ����
            await Task.Delay(10);
            return true;
        }

        private async Task<bool> SendVectorFrame(CANFrame frame)
        {
            await Task.Delay(10);
            return true;
        }

        private async Task<bool> SendSocketCANFrame(CANFrame frame)
        {
            await Task.Delay(10);
            return true;
        }

        private async Task<bool> SendTestFrame(CANFrame frame)
        {
            await Task.Delay(10);
            Debug.WriteLine($"�׽�Ʈ CAN ����: ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
            return true;
        }
    }

    /// <summary>
    /// CAN ������ ���� �̺�Ʈ �μ�
    /// </summary>
    public class CANTransmitEventArgs : EventArgs
    {
        public CANFrame Frame { get; }

        public CANTransmitEventArgs(CANFrame frame)
        {
            Frame = frame;
        }
    }

    /// <summary>
    /// CAN ������ ���� �̺�Ʈ �μ�
    /// </summary>
    public class CANDataReceivedEventArgs : EventArgs
    {
        public CANFrame Frame { get; }

        public CANDataReceivedEventArgs(CANFrame frame)
        {
            Frame = frame;
        }
    }
}