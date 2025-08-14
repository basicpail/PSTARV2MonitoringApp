using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PSTARV2MonitoringApp.ViewModels.Controls
{
    public partial class PSTARDevicePanelViewModel : ObservableObject
    {
        // 현재 선택된/활성화된 장치의 ID
        [ObservableProperty]
        private string _currentDeviceId;

        // 여러 장치 모델을 관리하는 컬렉션
        [ObservableProperty]
        private ObservableCollection<PSTARDevicePanelModel> _devices = new ObservableCollection<PSTARDevicePanelModel>();

        public PSTARDevicePanelViewModel()
        {
            // 필요한 초기화 로직
        }

        /// <summary>
        /// 새 장치를 추가합니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <param name="deviceModel">장치 모델</param>
        /// <returns>추가된 장치 모델</returns>
        public PSTARDevicePanelModel AddDevice(string deviceId, string deviceModel)
        {
            // 새 장치 상태 생성 및 추가
            var newDevice = new PSTARDevicePanelModel(deviceId, deviceModel);
            Devices.Add(newDevice);
            CurrentDeviceId = deviceId; // 추가된 장치를 현재 장치로 설정
            return newDevice;
        }

        /// <summary>
        /// 장치 ID에 해당하는 모델을 가져옵니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <returns>장치 모델 또는 null</returns>
        public PSTARDevicePanelModel GetDevice(string deviceId)
        {
            return Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        }

        /// <summary>
        /// 현재 활성화된 장치를 가져옵니다.
        /// </summary>
        public PSTARDevicePanelModel GetCurrentDevice()
        {
            return GetDevice(CurrentDeviceId);
        }

        /// <summary>
        /// DeviceStatusCardModel 정보로 장치 상태를 업데이트합니다.
        /// </summary>
        /// <param name="cardModel">DeviceStatusCardModel 객체</param>
        public void UpdateDeviceFromCardModel(DeviceStatusCardModel cardModel)
        {
            if (cardModel == null) return;

            var device = GetDevice(cardModel.DeviceId);
            if (device == null)
            {
                // 장치가 없으면 새로 생성
                device = new PSTARDevicePanelModel(cardModel.DeviceId, "Unknown");
                Devices.Add(device);
            }

            // 상태 업데이트
            device.UpdateFromDeviceStatusCard(cardModel);
        }

        /// <summary>
        /// 장치 상태를 직접 업데이트합니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <param name="isSourceOn">소스 상태</param>
        /// <param name="isAbnormal">비정상 상태</param>
        /// <param name="isRunning">운전 상태</param>
        /// <param name="isStopped">정지 상태</param>
        /// <param name="isHeating">가열 상태</param>
        /// <param name="isCommFailure">통신 실패</param>
        /// <param name="isLowPressure">저압 상태</param>
        /// <param name="isStandby">대기 상태</param>
        public void UpdateDeviceState(string deviceId, bool isSourceOn = false, bool isAbnormal = false,
            bool isRunning = false, bool isStopped = false, bool isHeating = false,
            bool isCommFailure = false, bool isLowPressure = false, bool isStandby = false)
        {
            var device = GetDevice(deviceId);
            if (device == null) return;

            device.IsSourceOn = isSourceOn;
            device.IsAbnormal = isAbnormal;
            device.IsRunning = isRunning;
            device.IsStopped = isStopped;
            device.IsHeating = isHeating;
            device.IsCommFailure = isCommFailure;
            device.IsLowPressure = isLowPressure;
            device.IsStandby = isStandby;
        }

        /// <summary>
        /// 장치를 제거합니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        public void RemoveDevice(string deviceId)
        {
            var device = GetDevice(deviceId);
            if (device != null)
            {
                Devices.Remove(device);
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
            var currentDevice = GetCurrentDevice();
            if (currentDevice == null) return;

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = "가열 기능 제어",
                Content = currentDevice.IsHeating ?
                    "가열 기능을 비활성화하시겠습니까?" :
                    "가열 기능을 활성화하시겠습니까?",
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
                currentDevice.IsHeating = !currentDevice.IsHeating;

                // 가열이 활성화되면 ON 상태로 변경
                if (currentDevice.IsHeating)
                {
                    currentDevice.IsOn = true;
                }
            }
        }

        [RelayCommand]
        public async Task ToggleMode()
        {
            //throw new NotImplementedException("모드 전환 기능은 아직 구현되지 않았습니다.");
            var currentDevice = GetCurrentDevice();
            if (currentDevice == null) return;

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = "모드 전환",
                Content = currentDevice.IsManualMode ?
                    "자동 모드로 전환하시겠습니까?" :
                    "수동 모드로 전환하시겠습니까?",
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
                currentDevice.IsManualMode = !currentDevice.IsManualMode;
            }
        }

        [RelayCommand]
        public async Task Start()
        {
            var currentDevice = GetCurrentDevice();
            if (currentDevice == null) return;

            // 이미 실행 중인지 확인
            if (currentDevice.IsRunning)
            {
                var alreadyRunningDialog = new ContentDialog
                {
                    Title = "알림",
                    Content = "장치가 이미 실행 중입니다.",
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
                Title = "장치 시작",
                Content = "장치를 시작하시겠습니까?",
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
                currentDevice.IsRunning = true;
                currentDevice.IsStopped = false;
                currentDevice.IsStandby = false;
                currentDevice.IsOn = true;
                currentDevice.IsSourceOn = true;
            }
        }

        [RelayCommand]
        public async Task Stop()
        {
            var currentDevice = GetCurrentDevice();
            if (currentDevice == null) return;

            // 이미 정지 상태인지 확인
            if (!currentDevice.IsRunning && currentDevice.IsStopped)
            {
                var alreadyStoppedDialog = new ContentDialog
                {
                    Title = "알림",
                    Content = "장치가 이미 정지 상태입니다.",
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
                Title = "장치 정지",
                Content = "장치를 정지하시겠습니까?",
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
                currentDevice.IsRunning = false;
                currentDevice.IsStopped = true;
                currentDevice.IsStandby = true;
                // 히팅은 유지하고 실행 상태만 변경
            }
        }

        [RelayCommand]
        public async Task Reset()
        {
            var currentDevice = GetCurrentDevice();
            if (currentDevice == null) return;

            // 확인 대화 상자 표시
            var dialog = new ContentDialog
            {
                Title = "장치 초기화",
                Content = "모든 장치 상태를 초기화하시겠습니까?",
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
                currentDevice.IsSourceOn = false;
                currentDevice.IsAbnormal = false;
                currentDevice.IsRunning = false;
                currentDevice.IsStopped = true;
                currentDevice.IsHeating = false;
                currentDevice.IsCommFailure = false;
                currentDevice.IsLowPressure = false;
                currentDevice.IsStandby = true;
                currentDevice.IsOn = false;
                currentDevice.IsManualMode = true;
            }
        }

        #endregion
    }
}