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
            // 큐에 저장된 프레임이 있으면 가져오기
            CANFrame frame = null;

            lock (_queueLock)
            {
                if (_transmittedFrames.Count > 0)
                {
                    frame = _transmittedFrames.Dequeue();
                    Debug.WriteLine($"테스트 프레임 수신: ID=0x{frame.Id:X3}, Data={frame.DataAsHex}");
                }
            }

            // 지연 시간 조정 (원활한 테스트를 위해 짧게 설정)
            await Task.Delay(1000, cancellationToken);

            return frame; // 큐에 프레임이 없으면 null 반환

            //await Task.Delay(1000, cancellationToken); // 1초마다 테스트 데이터 생성
            
            //var random = new Random();
            
            //// 실제 데이터 형식에 맞는 테스트 데이터 생성
            //var data = new byte[8];
            //data[0] = (byte)(random.Next(2)); // STBY_Start (0 or 1)
            //data[1] = (byte)(random.Next(2)); // RunLamp (0 or 1)
            //data[2] = (byte)(random.Next(2)); // Overload (0 or 1)
            //data[3] = (byte)(random.Next(2)); // ModeStatus (0 or 1)
            //data[4] = (byte)(random.Next(2)); // RUN_req (0 or 1)
            //data[5] = (byte)(random.Next(2)); // ResetButton (0 or 1)
            //data[6] = (byte)(random.Next(2)); // StandByLamp (0 or 1)
            //data[7] = (byte)(random.Next(2)); // TXLowpress (0 or 1)
            
            //return new CANFrame
            //{
            //    Id = Settings.DeviceBaseId + (uint)random.Next(3), // 0x100, 0x101, 0x102
            //    Data = data,
            //    Timestamp = DateTime.Now,
            //    IsExtended = false
            //};
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
                    }
                }

                // 모든 CAN 데이터 이벤트 발생 (추가된 부분)
                AllCANDataReceived?.Invoke(this, new CANDataReceivedEventArgs(frame));
                
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