using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PSTARV2MonitoringApp.ViewModels.Controls
{
    public partial class DeviceStatusCardViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<DeviceStatusCardModel> _deviceStatusCardModels = new ObservableCollection<DeviceStatusCardModel>();

        public DeviceStatusCardViewModel()
        {
            // 빈 컬렉션으로 초기화
            _deviceStatusCardModels = new ObservableCollection<DeviceStatusCardModel>();
        }

        // 초기 데이터 설정 - 더 이상 랜덤 데이터가 아닌 기본 초기 상태 설정
        public void InitializeDeviceStatusCardModels()
        {
            DeviceStatusCardModels.Clear();

            // DeviceInfo 모델을 사용하여 초기 데이터 생성
            foreach (var deviceId in DeviceInfo.SupportedDeviceIds)
            {
                DeviceStatusCardModels.Add(new DeviceStatusCardModel
                {
                    DeviceId = deviceId.StatusCardId,
                    CommStatus = "Disconnected", // 초기 상태는 연결 안됨
                    RunStatus = "Stopped",
                    RunMode = "Manual",
                    StandByStatus = "Ready",
                    OverloadStatus = "Normal",
                    LowPressureStatus = "Normal"
                });
            }
        }

        // 장치 상태 카드 추가 또는 업데이트
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

        // 특정 ID의 카드 상태를 변경하는 메서드
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

        // PSTARDeviceModel의 데이터를 기반으로 상태 카드 업데이트
        public void UpdateFromDeviceModel(string statusCardId, PSTARDeviceModel deviceModel)
        {
            if (deviceModel == null) return;

            var cardModel = new DeviceStatusCardModel
            {
                DeviceId = statusCardId,
                CommStatus = deviceModel.IsCommFailure ? "Disconnected" : "Connected",
                RunStatus = deviceModel.IsRunning ? "Running" : "Stopped",
                RunMode = deviceModel.IsManualMode ? "Manual" : "StandBy",
                StandByStatus = deviceModel.IsStandby ? "StandbyStart" : "Standby", //StandbyStart가 아닌 상태를 뭐라고 해야하지
                OverloadStatus = deviceModel.IsAbnormal ? "Abnormal" : "Normal",
                LowPressureStatus = deviceModel.IsLowPressure ? "LowPressure" : "Normal"
            };

            AddDeviceStatusCard(cardModel);
        }
    }
}