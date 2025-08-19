using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private PSTARDevicePanelModel _deviceModel;

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
        private PSTPumpModel _pumpModel;

        // 생성자에서 장치 ID를 받아 초기화 Q) 장치타입은 왜 안받지
        public PSTARDevicePanelViewModel(string deviceId = null)
        {
            DeviceId = deviceId;
            _logService = DeviceLogService.Instance;
            _canService = CANCommunicationService.Instance;

            if (!string.IsNullOrEmpty(deviceId))
            {
                // 장치 모델 생성
                DeviceModel = new PSTARDevicePanelModel(deviceId, "Unknown");

                // PSTAR 펌프 모델 생성 및 이벤트 연결
                _pumpModel = new PSTPumpModel(deviceId);
                _pumpModel.CANDataTransmitted += OnPumpCANDataTransmitted;
                _pumpModel.DeviceStateChanged += OnDeviceStateChanged;

                // 중요: 펌프 모델에 장치 모델 설정
                _pumpModel.SetModel(DeviceModel);
            }

            // CAN 통신 이벤트 구독
            _canService.DataReceived += OnCANDataReceived;
            _canService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        /// <summary>
        /// 장치 모델을 설정합니다.
        /// </summary>
        /// <param name="deviceModel">장치 모델</param>
        public void SetDeviceModel(PSTARDevicePanelModel deviceModel)
        {
            DeviceModel = deviceModel;
            DeviceId = deviceModel?.DeviceId;

            // 장치가 설정되었음을 로그에 기록
            if (deviceModel != null)
            {
                _logService.LogDeviceModelSet(deviceModel.DeviceId, deviceModel.DeviceModel);

                // 펌프 모델에도 새 장치 모델 설정
                if (_pumpModel != null)
                {
                    _pumpModel.SetModel(deviceModel);
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

            // 속성 업데이트 (자동으로 PSTPumpModel과 연동)
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
            if (_pumpModel != null)
            {
                _pumpModel.ProcessReceivedCANFrame(e.Frame);
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
                if (_pumpModel != null && !IsCANTransmissionActive)
                {
                    StartCANTransmission();
                }
            }
            else if (status.Contains("연결 해제") || status.Contains("실패"))
            {
                if (_pumpModel != null && IsCANTransmissionActive)
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
            if (!IsCANTransmissionActive && _pumpModel != null)
            {
                IsCANTransmissionActive = true;
                _pumpModel.StartSimulation();

                _logService.AddLog($"ID {DeviceId}", "CAN 데이터 전송 시작");
            }
        }

        /// <summary>
        /// CAN 전송 중지
        /// </summary>
        public void StopCANTransmission()
        {
            if (IsCANTransmissionActive && _pumpModel != null)
            {
                IsCANTransmissionActive = false;
                _pumpModel.StopSimulation();

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
                if (_pumpModel != null)
                {
                    _pumpModel.PressHeatButton();
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
                    $"장치 {DeviceId}를 수동 모드로 전환하시겠습니까?",
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
                if (_pumpModel != null)
                {
                    _pumpModel.PressModeButton();
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
                if (_pumpModel != null)
                {
                    _pumpModel.PressStartButton();
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
                if (_pumpModel != null)
                {
                    _pumpModel.PressStopButton();
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

            // PSTAR 펌프 모델 저압 상태 활성화
            if (_pumpModel != null)
            {
                _pumpModel.SetLowPressure(true);
            }
        }

        // LP Test 종료 (마우스 업 또는 마우스 벗어날 때)
        [RelayCommand]
        public void EndLPTest()
        {
            IsLPTestActive = false;
            _logService.LogLPTestEnd(DeviceId);

            // PSTAR 펌프 모델 저압 상태 비활성화
            if (_pumpModel != null)
            {
                _pumpModel.SetLowPressure(false);
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
            if (_pumpModel != null)
            {
                _pumpModel.CANDataTransmitted -= OnPumpCANDataTransmitted;
                _pumpModel.DeviceStateChanged -= OnDeviceStateChanged;
                _pumpModel.Dispose();
            }

            _canService.DataReceived -= OnCANDataReceived;
            _canService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        #endregion
    }
}