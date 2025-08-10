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

        public DeviceStatusCardViewModel()
        {

        }

        //초기    데이터 설정
        public void InitializeDeviceStatusCardModels()
        {
            DeviceStatusCardModels.Clear();
            DeviceStatusCardModels.Add(new DeviceStatusCardModel
            {
                DeviceId = "ID1",
                CommStatus = "Connected",
                RunStatus = "Running",
                RunMode = "Auto",
                StandByStatus = "Standby",
                OverloadStatus = "Normal",
                LowPressureStatus = "Normal"
            });
            DeviceStatusCardModels.Add(new DeviceStatusCardModel
            {
                DeviceId = "ID2",
                CommStatus = "Normal",
                RunStatus = "Stopped",
                RunMode = "Manual",
                StandByStatus = "Standby",
                OverloadStatus = "Normal",
                LowPressureStatus = "Normal"
            });
            DeviceStatusCardModels.Add(new DeviceStatusCardModel
            {
                DeviceId = "ID3",
                CommStatus = "Normal",
                RunStatus = "Stopped",
                RunMode = "Manual",
                StandByStatus = "Standby",
                OverloadStatus = "Normal",
                LowPressureStatus = "Normal"
            });
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
    }
}
