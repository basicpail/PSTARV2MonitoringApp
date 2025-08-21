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
    /// 실제 CAN 통신을 담당하는 서비스
    /// </summary>
    public class CANCommunicationService
    {
        private static CANCommunicationService _instance;
        public static CANCommunicationService Instance => _instance ??= new CANCommunicationService();

        private CancellationTokenSource _cancellationTokenSource;
        private CANSettings _settings;
        private object _canInterface; // 실제 CAN 인터페이스 객체

        // CAN 데이터 수신 이벤트
        public event EventHandler<CANDataReceivedEventArgs> DataReceived;
        public event EventHandler<string> ConnectionStatusChanged;
        public event EventHandler<string> ErrorOccurred;
        // 모든 CAN 데이터 이벤트 (로컬 송신 데이터 포함)
        public event EventHandler<CANDataReceivedEventArgs> AllCANDataReceived;
        // 송신 이벤트 추가
        public event EventHandler<CANTransmitEventArgs> DataTransmitted;

        private readonly Queue<CANFrame> _transmittedFrames = new Queue<CANFrame>(10); // 최대 10개 프레임 저장
        private readonly object _queueLock = new object();

        // 프레임이 큐에 추가될 때 발생하는 이벤트
        private event EventHandler FrameAddedToQueue;

        public CANSettings Settings => _settings ??= new CANSettings();
        public bool IsConnected => _settings?.IsConnected ?? false;

        private CANCommunicationService()
        {
        }

        /// <summary>
        /// CAN 통신 시작
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (IsConnected) return true;

            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                ConnectionStatusChanged?.Invoke(this, "연결 중...");
                
                // CAN 인터페이스 초기화
                var success = await InitializeCANInterface();
                if (!success)
                {
                    ConnectionStatusChanged?.Invoke(this, "연결 실패");
                    return false;
                }
                
                Settings.IsConnected = true;
                ConnectionStatusChanged?.Invoke(this, "연결됨");
                
                // 백그라운드에서 데이터 수신 시작
                _ = Task.Run(async () => await ReceiveDataLoop(_cancellationTokenSource.Token));
                
                return true;
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"연결 실패: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// CAN 통신 중지
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsConnected) return;

            _cancellationTokenSource?.Cancel();
            
            try
            {
                // CAN 인터페이스 정리
                await CleanupCANInterface();
                
                Settings.IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, "연결 해제됨");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"연결 해제 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// CAN 인터페이스 초기화
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
                        // 테스트 모드 (실제 하드웨어 없이 테스트)
                        return await InitializeTestInterface();
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"CAN 인터페이스 초기화 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PCAN 인터페이스 초기화
        /// </summary>
        private async Task<bool> InitializePCANInterface()
        {
            try
            {
                // PCAN 라이브러리 사용 예시
                // Peak.Can.Basic 라이브러리가 필요
                
                // TPCANStatus result = PCANBasic.Initialize(
                //     TPCANHandle.PCAN_USBBUS1,
                //     TPCANBaudrate.PCAN_BAUD_500K,
                //     TPCANType.PCAN_TYPE_ISA,
                //     0,
                //     0);
                
                await Task.Delay(500); // 초기화 시뮬레이션
                
                // 실제 구현 시:
                // return result == TPCANStatus.PCAN_ERROR_OK;
                
                Debug.WriteLine("PCAN 인터페이스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PCAN 초기화 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vector 인터페이스 초기화
        /// </summary>
        private async Task<bool> InitializeVectorInterface()
        {
            try
            {
                // Vector CANoe/CANalyzer 라이브러리 사용
                await Task.Delay(500);
                Debug.WriteLine("Vector 인터페이스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Vector 초기화 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// SocketCAN 인터페이스 초기화 (Linux용)
        /// </summary>
        private async Task<bool> InitializeSocketCANInterface()
        {
            try
            {
                // SocketCANSharp 라이브러리 사용
                await Task.Delay(500);
                Debug.WriteLine("SocketCAN 인터페이스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SocketCAN 초기화 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 테스트 인터페이스 초기화 (하드웨어 없이 테스트)
        /// </summary>
        private async Task<bool> InitializeTestInterface()
        {
            await Task.Delay(100);
            Debug.WriteLine("테스트 CAN 인터페이스 초기화 완료");
            return true;
        }

        /// <summary>
        /// CAN 인터페이스 정리
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
                        // Vector 정리 코드
                        break;
                    case "SOCKETCAN":
                        // SocketCAN 정리 코드
                        break;
                }
                
                await Task.Delay(100);
                Debug.WriteLine("CAN 인터페이스 정리 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CAN 인터페이스 정리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 데이터 수신 루프
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
                        //Console.WriteLine($"수신된 CAN 프레임: ID=0x{canFrame.Id:X3}, Data={BitConverter.ToString(canFrame.Data).Replace("-", " ")}");
                        // 수신된 데이터를 이벤트로 전달
                        DataReceived?.Invoke(this, new CANDataReceivedEventArgs(canFrame));
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CAN 수신 오류: {ex.Message}");
                    ErrorOccurred?.Invoke(this, $"수신 오류: {ex.Message}");
                    
                    // 오류 시 잠시 대기 후 재시도
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// CAN 프레임 수신
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
                Debug.WriteLine($"CAN 프레임 수신 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// PCAN 프레임 수신
        /// </summary>
        private async Task<CANFrame> ReceivePCANFrame(CancellationToken cancellationToken)
        {
            // 실제 PCAN 구현
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
        /// Vector 프레임 수신
        /// </summary>
        private async Task<CANFrame> ReceiveVectorFrame(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return null;
        }

        /// <summary>
        /// SocketCAN 프레임 수신
        /// </summary>
        private async Task<CANFrame> ReceiveSocketCANFrame(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return null;
        }

        /// <summary>
        /// 테스트 프레임 수신 (시뮬레이션)
        /// </summary>
        private async Task<CANFrame> ReceiveTestFrame(CancellationToken cancellationToken)
        {
            CANFrame frame = null;

            try
            {
                // 큐에 프레임이 있으면 즉시 처리
                lock (_queueLock)
                {
                    if (_transmittedFrames.Count > 0)
                    {
                        frame = _transmittedFrames.Dequeue();
                        Console.WriteLine($"{DateTime.Now} 테스트 프레임 수신(즉시 처리): ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        return frame;
                    }
                }

                // 큐가 비어있으면 프레임이 들어올 때까지 대기
                using var waitEvent = new SemaphoreSlim(0, 1);

                // 메시지 추가 이벤트 핸들러
                void OnFrameAdded(object sender, EventArgs e)
                {
                    waitEvent.Release();
                }

                // 임시 이벤트 구독
                FrameAddedToQueue += OnFrameAdded;

                try
                {
                    // 프레임이 추가되거나 취소 요청이 있을 때까지 대기
                    // 1초 타임아웃 추가 (무한정 대기하지 않도록)
                    await waitEvent.WaitAsync(1000, cancellationToken);

                    // 큐 재확인
                    lock (_queueLock)
                    {
                        if (_transmittedFrames.Count > 0)
                        {
                            frame = _transmittedFrames.Dequeue();
                            Console.WriteLine($"{DateTime.Now} 테스트 프레임 수신(재확인): ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        }
                    }
                }
                finally
                {
                    // 이벤트 구독 해제
                    FrameAddedToQueue -= OnFrameAdded;
                }
            }
            catch (OperationCanceledException)
            {
                // 작업 취소 시 null 반환
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReceiveTestFrame 오류: {ex.Message}");
            }

            return frame;
        }

        /// <summary>
        /// CAN 메시지 전송
        /// </summary>
        public async Task<bool> SendAsync(CANFrame frame)
        {
            if (!IsConnected) return false;

            try
            {
                // 송신 이벤트 발생 (추가된 부분)
                DataTransmitted?.Invoke(this, new CANTransmitEventArgs(frame));

                // 전송된 프레임을 큐에 저장 (테스트 모드용)
                if (Settings.InterfaceType.ToUpper() == "TEST")
                {
                    lock (_queueLock)
                    {
                        Console.WriteLine($"{DateTime.Now} 테스트 프레임 전송: ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                        // 큐 크기 제한
                        if (_transmittedFrames.Count >= 10)
                            _transmittedFrames.Dequeue();

                        // 새 프레임 추가
                        _transmittedFrames.Enqueue(new CANFrame
                        {
                            Id = frame.Id,
                            Data = frame.Data.ToArray(), // 데이터 복사
                            Timestamp = DateTime.Now,
                            IsExtended = frame.IsExtended
                        });

                        // 큐에 프레임이 추가되었음을 알림
                        FrameAddedToQueue?.Invoke(this, EventArgs.Empty);
                    }
                }

                // 모든 CAN 데이터 이벤트 발생 (추가된 부분)
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
                ErrorOccurred?.Invoke(this, $"전송 오류: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendPCANFrame(CANFrame frame)
        {
            // 실제 PCAN 전송 구현
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
            Debug.WriteLine($"테스트 CAN 전송: ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
            return true;
        }
    }

    /// <summary>
    /// CAN 데이터 전송 이벤트 인수
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
    /// CAN 데이터 수신 이벤트 인수
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