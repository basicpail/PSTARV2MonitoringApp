using System.Collections.Generic;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// 장치 로그 메시지 카테고리
    /// </summary>
    public enum LogCategory
    {
        System,     // 시스템 관련
        Device,     // 장치 기본 동작
        Status,     // 상태 변경
        Error,      // 오류
        Test        // 테스트
    }

    /// <summary>
    /// 장치 로그 메시지를 중앙에서 관리하는 클래스
    /// </summary>
    public static class DeviceLogMessages
    {
        #region 시스템 메시지
        public static class System
        {
            public const string MonitoringStart = "모니터링 시스템 시작";
            public const string DashboardAccess = "대시보드 페이지 접속";
            public const string TestPageAccess = "테스트 페이지 접속";
            public const string LogCleared = "로그가 초기화됨";
            public const string AllDevicesRemoved = "모든 장치 제거됨";
            public const string Error = "오류 발생: {0}";
        }
        #endregion

        #region 장치 기본 동작 메시지
        public static class Device
        {
            public const string Added = "장치 추가됨 (모델: {0})";
            public const string Removed = "장치 제거됨";
            public const string ModelSet = "장치 모델 설정: {0}";
            public const string MonitoringStart = "장치 모니터링 시작";
            public const string MonitoringEnd = "장치 모니터링 종료";
            public const string ConnectionWaiting = "장치 연결 대기 중";
            public const string CardNotFound = "카드를 찾을 수 없음 - 장치 추가 실패";
            public const string ReregisteredAfterRemoval = "기존 장치 제거 후 재등록";
        }
        #endregion

        #region 장치 제어 메시지
        public static class Control
        {
            public const string Start = "장치 시작";
            public const string Stop = "장치 정지";
            public const string StopReset = "장치 정지/리셋";
            public const string Reset = "장치 전체 초기화";
            public const string HeatOn = "가열 기능 활성화";
            public const string HeatOff = "가열 기능 비활성화";
            public const string ManualMode = "수동 모드로 전환";
            public const string AutoMode = "자동 모드로 전환";
        }
        #endregion

        #region 테스트 메시지
        public static class Test
        {
            public const string LPTestStart = "LP 테스트 시작";
            public const string LPTestEnd = "LP 테스트 종료";
            public const string RandomTest = "랜덤 테스트 실행";
        }
        #endregion

        #region 상태 변경 메시지
        public static class Status
        {
            // DeviceStatusCardModel 속성별 한글 표시명
            public static readonly Dictionary<string, string> PropertyDisplayNames = new()
            {
                { nameof(DeviceStatusCardModel.CommStatus), "통신 상태" },
                { nameof(DeviceStatusCardModel.RunStatus), "실행 상태" },
                { nameof(DeviceStatusCardModel.RunMode), "실행 모드" },
                { nameof(DeviceStatusCardModel.StandByStatus), "대기 상태" },
                { nameof(DeviceStatusCardModel.OverloadStatus), "과부하 상태" },
                { nameof(DeviceStatusCardModel.LowPressureStatus), "저압 상태" }
            };

            /// <summary>
            /// 상태 변경 메시지 생성
            /// </summary>
            /// <param name="propertyName">속성명</param>
            /// <param name="value">새 값</param>
            /// <returns>포맷된 상태 변경 메시지</returns>
            public static string CreateStatusChangeMessage(string propertyName, string value)
            {
                var displayName = PropertyDisplayNames.GetValueOrDefault(propertyName, propertyName);
                return $"{displayName}: {value}";
            }
        }
        #endregion

        #region 일반적인 검사 동작 메시지
        public static class Inspection
        {
            public const string TemperatureCheck = "온도 체크";
            public const string PressureMeasurement = "압력 측정";
            public const string StatusCheck = "상태 확인";
            public const string SensorInspection = "센서 점검";
            public const string DataCollection = "데이터 수집";
            public const string AlarmCheck = "알람 체크";
            public const string SystemDiagnosis = "시스템 진단";
            public const string ConnectivityTest = "연결성 테스트";
            public const string PerformanceTest = "성능 테스트";
            public const string SafetyCheck = "안전 점검";

            /// <summary>
            /// 랜덤 검사 동작 메시지 배열
            /// </summary>
            public static readonly string[] RandomInspectionActions = 
            {
                TemperatureCheck,
                PressureMeasurement,
                StatusCheck,
                SensorInspection,
                DataCollection,
                AlarmCheck,
                SystemDiagnosis,
                ConnectivityTest,
                PerformanceTest,
                SafetyCheck
            };
        }
        #endregion

        #region 헬퍼 메서드
        /// <summary>
        /// 매개변수가 있는 메시지를 포맷팅
        /// </summary>
        /// <param name="message">포맷 문자열</param>
        /// <param name="args">매개변수</param>
        /// <returns>포맷된 메시지</returns>
        public static string Format(string message, params object[] args)
        {
            return string.Format(message, args);
        }

        /// <summary>
        /// 장치 ID에 따른 표시 ID 가져오기
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <returns>표시용 ID</returns>
        public static string GetDisplayId(string deviceId)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdById(deviceId);
            return deviceInfo?.DisplayText ?? deviceId;
        }

        /// <summary>
        /// StatusCard ID에 따른 표시 ID 가져오기
        /// </summary>
        /// <param name="statusCardId">StatusCard ID</param>
        /// <returns>표시용 ID</returns>
        public static string GetDisplayIdFromStatusCard(string statusCardId)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdByStatusCardId(statusCardId);
            return deviceInfo?.DisplayText ?? statusCardId;
        }
        #endregion
    }
}