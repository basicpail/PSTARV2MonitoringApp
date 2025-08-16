using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.ViewModels.Controls
{
    public partial class DeviceStatusCardViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<DeviceStatusCardModel> _deviceStatusCardModels = new ObservableCollection<DeviceStatusCardModel>();

        private Random _random = new Random();

        public DeviceStatusCardViewModel()
        {

        }

        //초기 데이터 설정
        public void InitializeDeviceStatusCardModels()
        {
            DeviceStatusCardModels.Clear();
            
            // DeviceInfo 모델을 사용하여 초기 데이터 생성
            foreach (var deviceId in DeviceInfo.SupportedDeviceIds)
            {
                DeviceStatusCardModels.Add(new DeviceStatusCardModel
                {
                    DeviceId = deviceId.StatusCardId,
                    CommStatus = "Connected",
                    RunStatus = "Stopped",
                    RunMode = "Manual",
                    StandByStatus = "Standby", 
                    OverloadStatus = "Normal",
                    LowPressureStatus = "Normal"
                });
            }

            // 첫 번째 장치는 실행 중으로 설정
            if (DeviceStatusCardModels.Count > 0)
            {
                DeviceStatusCardModels[0].RunStatus = "Running";
                DeviceStatusCardModels[0].RunMode = "Auto";
            }
        }

        public void AddDeviceStatusCard(DeviceStatusCardModel model)
        {
            if (model == null) return;
            // 중복된 DeviceId가 있는지 확인
            var existingModel = DeviceStatusCardModels.FirstOrDefault(m => m.DeviceId == model.DeviceId);
            if (existingModel != null)
            {
                // 기존 모델 업데이트
                existingModel.CommStatus = model.CommStatus;
                existingModel.RunStatus = model.RunStatus;
                existingModel.RunMode = model.RunMode;
                existingModel.StandByStatus = model.StandByStatus;
                existingModel.OverloadStatus = model.OverloadStatus;
                existingModel.LowPressureStatus = model.LowPressureStatus;
            }
            else
            {
                // 새로운 모델 추가
                DeviceStatusCardModels.Add(model);
            }
        }

        // 테스트용 커맨드 추가
        [RelayCommand]
        public void UpdateRandomDeviceStatus()
        {
            // DeviceInfo 모델을 사용하여 랜덤 DeviceId 선택
            var randomDeviceInfo = DeviceInfo.SupportedDeviceIds[_random.Next(DeviceInfo.SupportedDeviceIds.Count)];

            // 랜덤 상태 값 생성
            string[] statuses = { "Connected", "Disconnected", "Warning" };
            string[] runStatuses = { "Running", "Stopped", "Error" };
            string[] runModes = { "Auto", "Manual", "Remote" };
            string[] standbyStatuses = { "Standby", "Ready", "Off" };
            string[] overloadStatuses = { "Normal", "Warning", "Critical" };
            string[] pressureStatuses = { "Normal", "Low", "Critical" };

            // 새 모델 생성 또는 기존 모델 업데이트
            var model = new DeviceStatusCardModel
            {
                DeviceId = randomDeviceInfo.StatusCardId,
                CommStatus = statuses[_random.Next(statuses.Length)],
                RunStatus = runStatuses[_random.Next(runStatuses.Length)],
                RunMode = runModes[_random.Next(runModes.Length)],
                StandByStatus = standbyStatuses[_random.Next(standbyStatuses.Length)],
                OverloadStatus = overloadStatuses[_random.Next(overloadStatuses.Length)],
                LowPressureStatus = pressureStatuses[_random.Next(pressureStatuses.Length)]
            };

            // 모델 추가 또는 업데이트
            AddDeviceStatusCard(model);
        }

        // 특정 ID의 카드 상태를 변경하는 메서드 (외부 통신 시뮬레이션)
        public void UpdateDeviceStatus(string statusCardId, string propertyName, string value)
        {
            var model = DeviceStatusCardModels.FirstOrDefault(m => m.DeviceId == statusCardId);
            if (model == null)
            {
                // ID에 해당하는 모델이 없으면 새로 생성
                model = new DeviceStatusCardModel { DeviceId = statusCardId };
                DeviceStatusCardModels.Add(model);
            }

            // 속성에 따라 값 업데이트
            switch (propertyName)
            {
                case nameof(DeviceStatusCardModel.CommStatus):
                    model.CommStatus = value;
                    break;
                case nameof(DeviceStatusCardModel.RunStatus):
                    model.RunStatus = value;
                    break;
                case nameof(DeviceStatusCardModel.RunMode):
                    model.RunMode = value;
                    break;
                case nameof(DeviceStatusCardModel.StandByStatus):
                    model.StandByStatus = value;
                    break;
                case nameof(DeviceStatusCardModel.OverloadStatus):
                    model.OverloadStatus = value;
                    break;
                case nameof(DeviceStatusCardModel.LowPressureStatus):
                    model.LowPressureStatus = value;
                    break;
            }
        }
    }
}
