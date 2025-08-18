using System.Collections.Generic;
using System.Linq;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// ��ġ ID ���� ��
    /// </summary>
    public class DeviceId
    {
        public string Id { get; set; }
        public string DisplayText { get; set; }
        public string StatusCardId { get; set; }
        public string UICardElementName { get; set; }

        public DeviceId(string id, string displayText, string statusCardId, string uiCardElementName)
        {
            Id = id;
            DisplayText = displayText;
            StatusCardId = statusCardId;
            UICardElementName = uiCardElementName;
        }
    }

    /// <summary>
    /// ��ġ �� ����
    /// </summary>
    public class DeviceModel
    {
        public string ModelName { get; set; }
        public string Description { get; set; }

        public DeviceModel(string modelName, string description = null)
        {
            ModelName = modelName;
            Description = description ?? modelName;
        }
    }

    /// <summary>
    /// ��ġ ������ �����ϴ� �߾� Ŭ����
    /// </summary>
    public static class DeviceInfo
    {
        /// <summary>
        /// �����ϴ� ��� ��ġ ID ���
        /// </summary>
        public static readonly List<DeviceId> SupportedDeviceIds = new()
        {
            new DeviceId("1", "ID 1", "ID1", "DevicePanelCard1"),
            new DeviceId("2", "ID 2", "ID2", "DevicePanelCard2"),
            new DeviceId("3", "ID 3", "ID3", "DevicePanelCard3")
        };

        /// <summary>
        /// �����ϴ� ��� ��ġ �� ���
        /// </summary>
        public static readonly List<DeviceModel> SupportedDeviceModels = new()
        {
            new DeviceModel("PSTAR-V2-5", "PSTAR V2 ��"),
            new DeviceModel("PSTAR-V2-5H", "PSTAR V2 ��"),
            new DeviceModel("PSTAR-V2-5-S", "PSTAR V2 ��"),
            new DeviceModel("PSTAR-V2-5H-S", "PSTAR V2 ��"),
            new DeviceModel("PSTAR-V2-5-R", "PSTAR V2 ��"),
            new DeviceModel("PSTAR-V2-5H-R", "PSTAR V2 ��")
        };

        #region Methods

        /// <summary>
        /// ID�� DeviceId ��ü ã��
        /// </summary>
        public static DeviceId GetDeviceIdById(string id)
        {
            return SupportedDeviceIds.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// DisplayText�� DeviceId ��ü ã��
        /// </summary>
        public static DeviceId GetDeviceIdByDisplayText(string displayText)
        {
            return SupportedDeviceIds.FirstOrDefault(d => d.DisplayText == displayText);
        }

        /// <summary>
        /// StatusCardId�� DeviceId ��ü ã��
        /// </summary>
        public static DeviceId GetDeviceIdByStatusCardId(string statusCardId)
        {
            return SupportedDeviceIds.FirstOrDefault(d => d.StatusCardId == statusCardId);
        }

        /// <summary>
        /// UICardElementName���� DeviceId ��ü ã��
        /// </summary>
        public static DeviceId GetDeviceIdByUICardElementName(string uiCardElementName)
        {
            return SupportedDeviceIds.FirstOrDefault(d => d.UICardElementName == uiCardElementName);
        }

        /// <summary>
        /// �𵨸����� DeviceModel ��ü ã��
        /// </summary>
        public static DeviceModel GetDeviceModelByName(string modelName)
        {
            return SupportedDeviceModels.FirstOrDefault(m => m.ModelName == modelName);
        }

        /// <summary>
        /// ID�� ��ȿ���� Ȯ��
        /// </summary>
        public static bool IsValidDeviceId(string id)
        {
            return SupportedDeviceIds.Any(d => d.Id == id);
        }

        /// <summary>
        /// �𵨸��� ��ȿ���� Ȯ��
        /// </summary>
        public static bool IsValidDeviceModel(string modelName)
        {
            return SupportedDeviceModels.Any(m => m.ModelName == modelName);
        }

        #endregion
    }
}