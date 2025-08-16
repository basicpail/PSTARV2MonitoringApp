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

        // 생성자에서 장치 ID를 받아 초기화
        public PSTARDevicePanelViewModel(string deviceId = null)
        {
            DeviceId = deviceId;
            _logService = DeviceLogService.Instance;
            _canService = CANCommunicationService.Instance;

            if (!string.IsNullOrEmpty(deviceId))
            {
                DeviceModel = new PSTARDevicePanelModel(deviceId, "Unknown");

                // PSTAR 펌프 모델 생성 및 이벤트 연결
                _pumpModel = new PSTPumpModel(deviceId);
                _pumpModel.CANDataTransmitted += OnPumpCANDataTransmitted;
                _pumpModel.DeviceStateChanged += OnDeviceStateChanged;
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

            DeviceModel.IsSourceOn = isSourceOn;
            DeviceModel.IsAbnormal = isAbnormal;
            DeviceModel.IsRunning = isRunning;
            DeviceModel.IsStopped = isStopped;
            DeviceModel.IsHeating = isHeating;
            DeviceModel.IsCommFailure = isCommFailure;
            DeviceModel.IsLowPressure = isLowPressure;
            DeviceModel.IsStandby = isStandby;

            // PSTAR 펌프 모델도 업데이트
            if (_pumpModel != null)
            {
                _pumpModel.SetOverload(isAbnormal);
                _pumpModel.SetLowPressure(isLowPressure);

                if (isRunning && !_pumpModel.RunStatus)
                    _pumpModel.PressStartButton();
                else if (!isRunning && _pumpModel.RunStatus)
                    _pumpModel.PressStopButton();

                if (isHeating != _pumpModel.HeatStatus)
                    _pumpModel.PressHeatButton();

                if (isStandby != _pumpModel.ModeStatus)
                    _pumpModel.PressModeButton();
            }
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
        private async void OnPumpCANDataTransmitted(object sender, Services.CANTransmitEventArgs e)
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
            if (DeviceModel != null)
            {
                // 장치 모델 업데이트
                DeviceModel.IsRunning = e.IsRunning;
                DeviceModel.IsManualMode = !e.IsStandByMode;
                DeviceModel.IsHeating = e.IsHeating;
                DeviceModel.IsStandby = e.IsStandByLamp;
                DeviceModel.IsStopped = !e.IsRunning;

                // UI 스레드에서 업데이트
                Application.Current.Dispatcher.Invoke(() => {
                    OnPropertyChanged(nameof(DeviceModel));
                });
            }
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
                Title = $"가열 기능 제어 - {DeviceId}",
                Content = DeviceModel.IsHeating ?
                    $"장치 {DeviceId}의 가열 기능을 비활성화하시겠습니까?" :
                    $"장치 {DeviceId}의 가열 기능을 활성화하시겠습니까?",
                PrimaryButtonText = "확인",
                CloseButtonText = "취소"
            };

            // DialogHost 설정
            dialog.DialogHost = GetDialogHost();

            // 다이얼로그 표시
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 가열 상태 토글
                DeviceModel.IsHeating = !DeviceModel.IsHeating;

                // 가열이 활성화되면 ON 상태로 변경
                if (DeviceModel.IsHeating)
                {
                    DeviceModel.IsOn = true;
                    _logService.LogHeatOn(DeviceId);
                }
                else
                {
                    _logService.LogHeatOff(DeviceId);
                }

                // PSTAR 펌프 모델 업데이트
                if (_pumpModel != null)
                {
                    _pumpModel.PressHeatButton();
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
                // 모드 상태 토글
                DeviceModel.IsManualMode = !DeviceModel.IsManualMode;

                if (DeviceModel.IsManualMode)
                {
                    _logService.LogManualMode(DeviceId);
                }
                else
                {
                    _logService.LogAutoMode(DeviceId);
                }

                // PSTAR 펌프 모델 업데이트
                if (_pumpModel != null)
                {
                    _pumpModel.PressModeButton();
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
                // 장치 상태 업데이트
                DeviceModel.IsRunning = true;
                DeviceModel.IsStopped = false;
                DeviceModel.IsStandby = false;
                DeviceModel.IsOn = true;
                DeviceModel.IsSourceOn = true;

                _logService.LogDeviceStart(DeviceId);

                // PSTAR 펌프 모델 업데이트
                if (_pumpModel != null)
                {
                    _pumpModel.PressStartButton();
                }
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
                // 장치 상태 업데이트
                DeviceModel.IsRunning = false;
                DeviceModel.IsStopped = true;
                DeviceModel.IsStandby = true;

                _logService.LogDeviceStopReset(DeviceId);

                // PSTAR 펌프 모델 업데이트
                if (_pumpModel != null)
                {
                    _pumpModel.PressStopButton();
                }
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
                // 모든 상태 초기화
                DeviceModel.IsSourceOn = false;
                DeviceModel.IsAbnormal = false;
                DeviceModel.IsRunning = false;
                DeviceModel.IsStopped = true;
                DeviceModel.IsHeating = false;
                DeviceModel.IsCommFailure = false;
                DeviceModel.IsLowPressure = false;
                DeviceModel.IsStandby = true;
                DeviceModel.IsOn = false;
                DeviceModel.IsManualMode = true;

                _logService.LogDeviceReset(DeviceId);

                // PSTAR 펌프 모델 초기화
                if (_pumpModel != null)
                {
                    if (_pumpModel.RunStatus)
                        _pumpModel.PressStopButton();

                    if (!_pumpModel.ModeStatus)
                        _pumpModel.PressModeButton(); // STBY 모드로 설정

                    if (_pumpModel.HeatStatus)
                        _pumpModel.PressHeatButton(); // 히팅 OFF

                    _pumpModel.SetOverload(false);
                    _pumpModel.SetLowPressure(false);
                }
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
            }

            _canService.DataReceived -= OnCANDataReceived;
            _canService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        #endregion
    }
}