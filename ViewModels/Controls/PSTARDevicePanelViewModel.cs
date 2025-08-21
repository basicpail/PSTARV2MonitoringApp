using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Services;
using PSTARV2MonitoringApp.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PSTARV2MonitoringApp.ViewModels.Controls
{
    public partial class PSTARDevicePanelViewModel : ObservableObject
    {
        // 이 ViewModel이 담당하는 특정 장치의 ID
        [ObservableProperty]
        private string _deviceId;

        // 이 ViewModel이 관리하는 장치 모델 (단일 장치)
        [ObservableProperty]
        private PSTARDeviceModel _deviceModel;

        // LP Test 상태
        [ObservableProperty]
        private bool _isLPTestActive;

        // CAN 전송 활성화 상태
        [ObservableProperty]
        private bool _isCANTransmissionActive = false;

        // 로그 서비스 참조
        private readonly DeviceLogService _logService;

        // CAN 통신 서비스 참조
        private readonly CANCommunicationService _canService;

        // PSTAR 펌프 모델 (실제 장치와 동일한 동작)
        private PSTARDeviceService _deviceService;

        // 생성자에서 장치 ID를 받아 초기화 Q) 장치타입은 왜 안받지
        public PSTARDevicePanelViewModel(string deviceId = null)
        {
            DeviceId = deviceId;
            _logService = DeviceLogService.Instance;
            _canService = CANCommunicationService.Instance;

            if (!string.IsNullOrEmpty(deviceId))
            {
                // 장치 모델 생성
                //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{DeviceId}] From PanelViewModel 생성자");
                DeviceModel = new PSTARDeviceModel(deviceId, "Unknown");

                // PSTAR 모델 생성 및 이벤트 연결
                _deviceService = new PSTARDeviceService(deviceId);
                _deviceService.CANDataTransmitted += OnPumpCANDataTransmitted; //DeviceService에서 TransmitCANData 호출 시 이벤트 발생
                _deviceService.DeviceStateChanged += OnDeviceStateChanged;

                // 중요: 펌프 모델에 장치 모델 설정
                _deviceService.SetModel(DeviceModel);
            }

            // CAN 통신 이벤트 구독
            _canService.DataReceived += OnCANDataReceived;
            _canService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        /// <summary>
        /// 장치 모델을 설정합니다.
        /// </summary>
        /// <param name="deviceModel">장치 모델</param>
        public void SetDeviceModel(PSTARDeviceModel deviceModel)
        {
            DeviceModel = deviceModel;
            DeviceId = deviceModel?.DeviceId;

            // 장치가 설정되었음을 로그에 기록
            if (deviceModel != null)
            {
                _logService.LogDeviceModelSet(deviceModel.DeviceId, deviceModel.DeviceModel);

                // 펌프 모델에도 새 장치 모델 설정
                if (_deviceService != null)
                {
                    _deviceService.SetModel(deviceModel);
                }
            }
        }

        /// <summary>
        /// DeviceStatusCardModel 정보로 장치 상태를 업데이트합니다.
        /// </summary>
        /// <param name="cardModel">DeviceStatusCardModel 객체</param>
        public void UpdateDeviceFromCardModel(DeviceStatusCardModel cardModel)
        {
            if (cardModel == null || DeviceModel == null) return;

            // 장치 ID가 일치하는 경우에만 업데이트
            if (DeviceModel.DeviceId == cardModel.DeviceId)
            {
                DeviceModel.UpdateFromDeviceStatusCard(cardModel);
            }
        }

        /// <summary>
        /// 장치 상태를 직접 업데이트합니다.
        /// </summary>
        public void UpdateDeviceState(bool isSourceOn = false, bool isAbnormal = false,
            bool isRunning = false, bool isStopped = false, bool isHeating = false,
            bool isCommFailure = false, bool isLowPressure = false, bool isStandby = false)
        {
            if (DeviceModel == null) return;

            // 속성 업데이트
            DeviceModel.IsSourceOn = isSourceOn;
            DeviceModel.IsAbnormal = isAbnormal;
            DeviceModel.IsRunning = isRunning;
            DeviceModel.IsStopped = isStopped;
            DeviceModel.IsHeating = isHeating;
            DeviceModel.IsCommFailure = isCommFailure;
            DeviceModel.IsLowPressure = isLowPressure;
            DeviceModel.IsStandby = isStandby;
        }

        /// <summary>
        /// CAN 데이터 수신 처리
        /// </summary>
        private void OnCANDataReceived(object sender, CANDataReceivedEventArgs e)
        {
            if (_deviceService != null)
            {
                _deviceService.ProcessReceivedCANFrame(e.Frame);
            }
        }

        /// <summary>
        /// 펌프 모델 CAN 데이터 전송 이벤트 처리
        /// </summary>
        private async void OnPumpCANDataTransmitted(object sender, CANTransmitEventArgs e)
        {
            if (IsCANTransmissionActive && _canService.IsConnected)
            {
                // CAN 서비스를 통해 데이터 전송
                //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{DeviceId}] OnPumpCANDataTransmitted 호출됨 테스트 프레임 전송 호출 From PanelViewModel - 발생 소스: {sender.GetType().Name}");

                await _canService.SendAsync(e.Frame);
            }
        }

        /// <summary>
        /// 장치 상태 변경 이벤트 처리
        /// </summary>
        private void OnDeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            // UI 스레드에서 업데이트 (DeviceModel은 자동으로 업데이트됨)
            Application.Current.Dispatcher.Invoke(() => {
                OnPropertyChanged(nameof(DeviceModel));
            });
        }

        /// <summary>
        /// 연결 상태 변경 이벤트 처리
        /// </summary>
        private void OnConnectionStatusChanged(object sender, string status)
        {
            if (status.Contains("연결됨"))
            {
                if (_deviceService != null && !IsCANTransmissionActive)
                {
                    StartCANTransmission();
                }
            }
            else if (status.Contains("연결 해제") || status.Contains("실패"))
            {
                if (_deviceService != null && IsCANTransmissionActive)
                {
                    StopCANTransmission();
                }
            }
        }

        /// <summary>
        /// CAN 전송 시작
        /// </summary>
        public void StartCANTransmission()
        {
            if (!IsCANTransmissionActive && _deviceService != null)
            {
                IsCANTransmissionActive = true;
                _deviceService.StartSimulation();

                _logService.AddLog($"ID {DeviceId}", "CAN 데이터 전송 시작");
            }
        }

        /// <summary>
        /// CAN 전송 중지
        /// </summary>
        public void StopCANTransmission()
        {
            if (IsCANTransmissionActive && _deviceService != null)
            {
                IsCANTransmissionActive = false;
                _deviceService.StopSimulation();

                _logService.AddLog($"ID {DeviceId}", "CAN 데이터 전송 중지");
            }
        }

        // 다이얼로그 호스트를 가져오는 헬퍼 메서드
        private ContentPresenter GetDialogHost()
        {
            return Application.Current.MainWindow?.FindName("RootContentDialog") as ContentPresenter;
        }

        #region 장치 제어 명령

        [RelayCommand]
        public async Task ToggleHeat()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"HEAT 기능 제어 - {DeviceId}",
                Content = DeviceModel.IsHeating ?
                    $"장치 {DeviceId}의 HEAT 기능을 비활성화하시겠습니까?" :
                    $"장치 {DeviceId}의 HEAT 기능을 활성화하시겠습니까?",
                PrimaryButtonText = "확인",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 펌프 모델을 통해 히팅 버튼 제어
                if (_deviceService != null)
                {
                    _deviceService.PressHeatButton();
                }

                // 로그 기록
                if (DeviceModel.IsHeating)
                {

                    _logService.LogHeatOn(DeviceId);
                }
                else
                {
                    _logService.LogHeatOff(DeviceId);
                }
            }
        }

        [RelayCommand]
        public async Task ToggleMode()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"모드 전환 - {DeviceId}",
                Content = DeviceModel.IsManualMode ?
                    $"장치 {DeviceId}를 자동 모드로 전환하시겠습니까?" :
                    $"장치 {DeviceId}를 StandBy 모드로 전환하시겠습니까?",
                PrimaryButtonText = "확인",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 펌프 모델을 통해 모드 버튼 제어
                if (_deviceService != null)
                {
                    _deviceService.PressModeButton();
                }

                // 로그 기록
                if (DeviceModel.IsManualMode)
                {
                    _logService.LogManualMode(DeviceId);
                }
                else
                {
                    _logService.LogAutoMode(DeviceId);
                }
            }
        }

        [RelayCommand]
        public async Task Start()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 이미 실행 중인지 확인
            if (DeviceModel.IsRunning)
            {
                var alreadyRunningDialog = new ContentDialog
                {
                    Title = "알림",
                    Content = $"장치 {DeviceId}가 이미 실행 중입니다.",
                    CloseButtonText = "확인"
                };

                // DialogHost 설정
                alreadyRunningDialog.DialogHost = GetDialogHost();

                await alreadyRunningDialog.ShowAsync();
                return;
            }

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"장치 시작 - {DeviceId}",
                Content = $"장치 {DeviceId}를 시작하시겠습니까?",
                PrimaryButtonText = "시작",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 펌프 모델을 통해 시작 버튼 제어
                if (_deviceService != null)
                {
                    _deviceService.PressStartButton();
                }

                _logService.LogDeviceStart(DeviceId);
            }
        }

        [RelayCommand]
        public async Task Stop()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 이미 정지 상태인지 확인
            if (!DeviceModel.IsRunning && DeviceModel.IsStopped)
            {
                var alreadyStoppedDialog = new ContentDialog
                {
                    Title = "알림",
                    Content = $"장치 {DeviceId}가 이미 정지 상태입니다.",
                    CloseButtonText = "확인"
                };

                // DialogHost 설정
                alreadyStoppedDialog.DialogHost = GetDialogHost();

                await alreadyStoppedDialog.ShowAsync();
                return;
            }

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"장치 정지 - {DeviceId}",
                Content = $"장치 {DeviceId}를 정지하시겠습니까?",
                PrimaryButtonText = "정지",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 펌프 모델을 통해 정지 버튼 제어
                if (_deviceService != null)
                {
                    _deviceService.PressStopButton();
                }

                _logService.LogDeviceStopReset(DeviceId);
            }
        }

        [RelayCommand]
        public async Task Reset()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"장치 초기화 - {DeviceId}",
                Content = $"장치 {DeviceId}의 모든 상태를 초기화하시겠습니까?",
                PrimaryButtonText = "초기화",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 장치 상태 초기화
                //이미 생성자에서 InitializeDefaultValues 호출 되는거 아닌가
                //DeviceModel.InitializeDefaultValues();

                _logService.LogDeviceReset(DeviceId);
            }
        }

        // LP Test 시작 (마우스 다운 시)
        [RelayCommand]
        public void StartLPTest()
        {
            IsLPTestActive = true;
            _logService.LogLPTestStart(DeviceId);
        }

        // LP Test 종료 (마우스 업 또는 마우스 벗어날 때)
        [RelayCommand]
        public void EndLPTest()
        {
            IsLPTestActive = false;
            _logService.LogLPTestEnd(DeviceId);
        }


        // 이상 발생 시뮬레이션
        [RelayCommand]
        public async Task SimulateAbnormal()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 이상 발생 대화상자 표시
            var abnormalDialog = new Views.Dialogs.AbnormalSimulationDialog(DeviceId);

            var dialog = new ContentDialog
            {
                Title = $"ID {DeviceId} 장치에 발생 시킬 이상 상황",
                Content = abnormalDialog,
                PrimaryButtonText = "발생",
                CloseButtonText = "취소"
            };

            dialog.DialogHost = GetDialogHost();
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 선택된 이상 유형 가져오기
                var abnormalType = abnormalDialog.GetSelectedAbnormalTypeEnum();
                string logMessage = string.Empty;

                switch (abnormalType)
                {
                    case AbnormalType.OVERLOAD:
                        // Overload 상태 설정
                        if (_deviceService != null)
                        {
                            _deviceService.SetOverload(true);
                            logMessage = "비정상 상태(Overload) 발생";

                            // PSTARDeviceModel에 직접 설정
                            DeviceModel.Overload = true;
                            DeviceModel.IsAbnormal = true;
                            DeviceModel.Overload_I = true;
                        }
                        break;

                    case AbnormalType.POWERFAIL:
                        //아직 구현 안됨
                        break;

                    

                    case AbnormalType.LowPressure:
                        // 저압 상태 설정
                        if (_deviceService != null)
                        {
                            Console.WriteLine("LOWPressure 발생");
                            //_deviceService.SetLowPressure(true);

                            // PSTARDeviceModel에 직접 설정
                            DeviceModel.TXLowpress = true;
                            //DeviceModel.IsLowPressure = true;
                            //DeviceModel.Lowpress_I = true;
                            //DeviceModel.TxLowpressInternal = true;

                            logMessage = "저압 상태 발생";
                        }
                        break;

                    case AbnormalType.CommFailure:
                        // 통신 오류 상태 설정
                        DeviceModel.IsCommFailure = true;

                        // 통신 오류 관련 플래그 설정
                        switch (DeviceId)
                        {
                            case "1":
                                DeviceModel.Error_Flag1 = true;
                                break;
                            case "2":
                                DeviceModel.Error_Flag2 = true;
                                break;
                            case "3":
                                DeviceModel.Error_Flag3 = true;
                                break;
                        }

                        logMessage = "통신 오류 발생";
                        break;
                }

                // 로그 기록
                if (!string.IsNullOrEmpty(logMessage))
                {
                    _logService.AddLog($"ID {DeviceId}", logMessage);
                }

                // 상태 변경 후 로직 실행 강제 트리거
                if (_deviceService != null)
                {
                    _deviceService.ExecutePSTARLogic();
                }

                // 속성 변경 알림
                OnPropertyChanged(nameof(DeviceModel));
            }
        }
        private async Task ShowErrorDialog(string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = "오류",
                Content = message,
                CloseButtonText = "확인"
            };

            errorDialog.DialogHost = GetDialogHost();
            await errorDialog.ShowAsync();
        }

        /// <summary>
        /// 정리
        /// </summary>
        public void Dispose()
        {
            // CAN 전송 중지
            StopCANTransmission();

            // 이벤트 구독 해제
            if (_deviceService != null)
            {
                _deviceService.CANDataTransmitted -= OnPumpCANDataTransmitted;
                _deviceService.DeviceStateChanged -= OnDeviceStateChanged;
                _deviceService.Dispose();
            }

            _canService.DataReceived -= OnCANDataReceived;
            _canService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        #endregion

        // 추가 명령 정의 - 파일 내 적절한 위치에 아래 코드를 추가해주세요
        #region 추가 기능 명령

        // 상세 정보 표시
        [RelayCommand]
        public async Task ShowDeviceDetails()
        {
            if (DeviceModel == null)
            {
                await ShowErrorDialog("장치 모델이 설정되지 않았습니다.");
                return;
            }

            // 상세 정보 대화상자를 보여주기 위한 Views.Dialogs.DeviceDetailsDialog 호출
            var detailsDialog = new Views.Dialogs.DeviceDetailsDialog(DeviceModel);

            var dialog = new ContentDialog
            {
                Title = $"ID {DeviceId} 장치 상세 정보",
                Content = detailsDialog,
                CloseButtonText = "확인"
            };

            dialog.DialogHost = GetDialogHost();
            await dialog.ShowAsync();

            // 로그 기록
            _logService.AddLog($"ID {DeviceId}", "장치 상세 정보 조회");
        }
        

        // 장치 삭제 처리
        [RelayCommand]
        public async Task DeleteDevice()
        {
            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = $"장치 삭제 확인",
                Content = $"ID {DeviceId} 장치를 삭제하시겠습니까? 이 작업은 되돌릴 수 없습니다.",
                PrimaryButtonText = "삭제",
                CloseButtonText = "취소",
            };

            dialog.DialogHost = GetDialogHost();
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 로그 기록
                _logService.AddLog($"ID {DeviceId}", "장치 삭제 요청됨");

                // DeviceDeleted 이벤트 발생 - 상위 컴포넌트에서 실제 삭제 처리
                DeviceDeleted?.Invoke(this, new DeviceDeletedEventArgs { DeviceId = DeviceId });
            }
        }

        // 장치 삭제 이벤트
        public event EventHandler<DeviceDeletedEventArgs> DeviceDeleted;

        #endregion

    }
    // PSTARDevicePanelViewModel 클래스 외부에 아래 클래스를 추가합니다.
    public class DeviceDeletedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }

    }
}