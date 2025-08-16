//// TIR2209-50-P01
//// PSTAR-V2-5
//// Crative History
//// V1.0 : 24.11.11, JHChun
//// V1.1 : 25.03.26, JHChun / RunInput : Delete CountRunDelay_mS
//
//#include "mcc_generated_files/mcc.h"
//
//// POARTA
//#define F1                  LATAbits.LATA0
//#define F2                  LATAbits.LATA1
//#define F3                  LATAbits.LATA2
//#define F4                  LATAbits.LATA3
//#define F5                  LATAbits.LATA4
//#define F6                  LATAbits.LATA5
//#define CAN_ID1             PORTAbits.RA6
//#define CAN_ID2             PORTAbits.RA7
//
//// PORTB
//#define START_PB_I          PORTBbits.RB2
//#define STOP_PB_I           PORTBbits.RB3
//#define MODE_PB_I           PORTBbits.RB4
//#define HEATER_PB_I         PORTBbits.RB5
//
//// PORTC
//#define START_REMOTE_I      PORTCbits.RC0
//#define STOP_REMOTE_I       PORTCbits.RC1
//#define OVERLOAD_I          PORTCbits.RC2
//#define LOW_PRESS_I         PORTCbits.RC3
//#define RUN_I               PORTCbits.RC4
//#define Spare_I    PORTCbits.RC5
//#define SYSTEM_ALARM        LATCbits.LATC6
//#define F                   LATCbits.LATC7
//
//// PORTE
//#define CHATTERING0         PORTEbits.RE0
//#define CHATTERING1         PORTEbits.RE1
//#define CHATTERING2         PORTEbits.RE2
//
//// EEPROM
//#define RUN_S               1
//#define MODE_S              2
//#define HEAT_S              3
//#define COMSTATUS_S         4
//
//// HEAT
//#define HEAT_OFF            0
//#define HEAT_ON             1
//
//// MODE
//#define MANUAL_MODE         0
//#define STBY_MODE           1
//
//// STATUS
//#define ON                  1
//#define OFF                 0
//
//#define RUN                 1
//#define STOP                0
//
//#define StandByLamp_OFF     0
//#define StandByLamp_ON      1
//
//#define Overload_OFF        0
//#define Overload_ON         1
//
//#define Lowpress_OFF        0
//#define Lowpress_ON         1
//
//#define ResetButton_ON      1
//#define ResetButton_OFF     0
//
//#define PressureSW_1EA      0
//#define PressureSW_2EA      1
//
//// LAMP & SIGNAL
//#define ABN_LAMP            1
//#define STAND_BY_LAMP       2
//#define STOP_LAMP           3
//#define LOW_PRESS_LAMP      4
//#define COM_FAULT_LAMP      5
//#define HEAT_ON_LAMP        6
//#define MODE_MAUAL_LAMP     7
//#define MODE_STBY_LAMP      8
//#define RUN_LAMP            9
//#define RUN_SIG             10
//#define HEATING_SIG         11
//#define STOP_SIG            12
//#define STBY_START_ALARM    13
//#define STAND_BY_SIG        14
//
//// COM STATUS
//#define Com_Error           0
//#define Com_Normal          1
//
//#define NoConnection        0
//#define StandBy_3to2        1
//#define StandBy_2           2
//#define StandBy_3           3
//#define Manual              4
//#define StandBy_3_1RUN      5  // 3 ST'BY 1 RUN
//#define StandBy_3to2_1RUN   6  // 3 ST'BY 2 RUN -> 1 RUN
//
//// DELAY, COUNT
//#define BT_Delayms          2  // Input Chattering Delay : 200ms
//#define StopPulse           11 // Stop Pulse Signal : 1s
//#define ComInit_S           0  // First StandBy Status Control : 0s
//#define ComFault_S          1  // Com Fault(Power Fail) Count : 1s
//#define RunReq_S            1  // Overload Run Req Count : 1s
//#define CanDelay_mS         2  // Can Communication Period : 300ms
//
//// FUNCTION DEFINE
//void SeqTimeRead(void);
//void BuildUpTimeRead(void);
//void ParallelTimeRead(void);
//void HeatingOnTimeRead(void);
//void Latch(void);
//void ERPomRead(unsigned char command);
//void EPRomWrite(unsigned char command, unsigned char value);
//void RunStopCont(unsigned char command);
//void RunStopProc(unsigned char command);
//void RunInput(void);
//void HeatCont(unsigned char command);
//void HeatProc(unsigned char command);
//void ModeProc(unsigned char command);
//void KeyProc(void);
//void OverloadProc(unsigned char command);
//void LowpressProc(unsigned char command);
//void LampSigCont(unsigned char name, unsigned char cmd);
//void InputChattProc(void);
//void ComFailErrorFlag(void);
//void CanRevMsg(void);
//void CanIdRead(unsigned char value1, unsigned char value2);
//void ReceiveRunReq(void);
//void SendRunReq(void);
//void STBYStartAlarm(void);
//void ConnectProc(unsigned char status);
//void ConnectFunction(void);
//void Chattering(unsigned char value3, unsigned char value2, unsigned char value1);
//void StandByLampProc(unsigned char mode);
//
//// VARIABLE DEFINE
//// LATCH
//unsigned short F1_OUT = 0, F2_OUT = 0, F3_OUT = 0;
//unsigned char F4_OUT = 0, F5_OUT = 0, F6_OUT = 0;
//// TIMER  
//unsigned short SeqTime_mS = 0, BuildUpTime = 0, ParallelTime = 0, HeatingOnTime = 0;
//unsigned int BuildUp = 0, Parallel = 0, HeatOn = 0, BuildUp1 = 0, BuildUp2 = 0, Parallel1 = 0, Parallel2 = 0;
//// TIMER START FLAG
//unsigned char CountParaStart = 0, CountBuildUpStart = 0;
//// FLAG
//unsigned char Request_Flag = 0, InitFlag = 0, ComFailLamp_Flag = 0;
//unsigned char Error_Flag1 = 0, Error_Flag2 = 0, Error_Flag3 = 0;
//unsigned char StandBy_3_1RUN_Flag = 0;
//unsigned char CountOverload_Flag = 0, StandBy_2_Flag = 0;
//// DELAY
//unsigned short CountStopPulse_mS = 0, CountCanDelay_mS = 0;
//unsigned short CountSeqTime_mS = 0, CountHeatingOnTime_S = 0, CountParallelTime_S = 0, CountBuildUpTime_S = 0, CountBuildUpTime = 0;
//unsigned short CountResetButton_S = 0, CountComFailLamp_mS = 0;
//unsigned char CountRunReq_S = 0, CountStandByCheck_mS = 0;
//unsigned char CountOverload_S = 0, CountStandBy_2_mS = 0;
//// COM FAILURE COUNT
//unsigned short CountComInit = 0, CountComFault1_S = 0, CountComFault2_S = 0, CountComFault3_S = 0;
//// EEPROM
//unsigned char RunEE = 0, OldRunEEStatus = 0;
//unsigned char ModeEE = 0, OldModeEEStatus = 0;
//unsigned char HeatEE = 0, OldHeatEEStatus = 0;
//unsigned char ComStatusEE = 0, OldComStatusEEStatus = 0;
//// BUTTON
//unsigned char OldStartPB = 0, OldStopPB = 0, OldHeatPB = 0, OldModePB = 0;
//// STATUS
//unsigned char RunStatus = 0, HeatStatus = 0, ModeStatus = 0;
//unsigned char OldLowpress = 0, OldRunStatus = 0, FirstRunStatus = 0;
//unsigned char RunLamp = 0, StandByLamp = 0, StopLamp = 0;
//unsigned char Overload = 0, Lowpress = 0;
//unsigned char RUN_req = 0, ResetButton = 0;
//unsigned char STBY_Start = 0, STBY_Overload = 0, Stop_Overload = 0;
//unsigned char TXLowpress = 0, txLowpress = 0;
//// CHATTERING
//unsigned char RunFB_I = 0, RunRemote_I = 0, StopRemote_I = 0, Overload_I = 0, Lowpress_I = 0;
//unsigned int BTCount_ms[10] = { 0, };
//unsigned char LowPressChattTime = 0;
//double StartChatt = 0.0, StopChatt = 0.0;
//unsigned short Chattering1 = 0, Chattering2 = 0, Chatt1 = 0, Chatt2 = 0;
//// ID
//unsigned char CAN_ID = 0;
//unsigned int MSG_ID = 0;
//// COMMUNICATION
//CAN_MSG_OBJ msg_tx, msg_rx;
//CAN_TX_MSG_REQUEST_STATUS tx_status;
//unsigned char tx_data[8];
//unsigned char rx_data1[DLC_8], rx_data2[DLC_8], rx_data3[DLC_8];
//unsigned char nrMsg = 0;
//unsigned char ComStatus = 0, ComStatus_Flag = 0;
//
//void main(void)
//{
//    SYSTEM_Initialize();
//    CanIdRead(CAN_ID1, CAN_ID2);
//    SeqTimeRead();
//    TMR0_StartTimer();
//    INTERRUPT_GlobalInterruptEnable();
//
//    //---------------------------READ EEPROM---------------------------
//    ERPomRead(RUN_S);
//    ERPomRead(HEAT_S);
//    ERPomRead(MODE_S);
//    ERPomRead(COMSTATUS_S);
//
//    //---------------------------RUN---------------------------
//    if (RunEE == STOP)                          RunStopCont(STOP);
//    else if (RunEE == RUN && OVERLOAD_I == OFF) InitFlag = 1;
//
//    //---------------------------HEAT---------------------------
//    if (HeatEE == HEAT_OFF)                     HeatCont(HEAT_OFF);
//    else if (HeatEE == HEAT_ON)                 HeatStatus = HEAT_ON;
//
//    //---------------------------MODE---------------------------
//    if (ModeEE == MANUAL_MODE)                  ModeStatus = MANUAL_MODE;
//    else if (ModeEE == STBY_MODE)               ModeStatus = STBY_MODE;
//
//    //------------------MEMORY STAND BY STATUS------------------
//    if (ComStatusEE == NoConnection)            ComStatus = NoConnection;
//    else if (ComStatusEE == StandBy_3to2)       ComStatus = StandBy_3to2;
//    else if (ComStatusEE == StandBy_2)          ComStatus = StandBy_2;
//    else if (ComStatusEE == StandBy_3)          ComStatus = StandBy_3;
//    else if (ComStatusEE == Manual)             ComStatus = Manual;
//    else if (ComStatusEE == StandBy_3_1RUN)     ComStatus = StandBy_3_1RUN;
//    else if (ComStatusEE == StandBy_3to2_1RUN)  ComStatus = StandBy_3to2_1RUN;
//
//    //--------------------------SETTING : tx--------------------------
//    msg_tx.msgId = MSG_ID;
//    msg_tx.field.formatType = CAN_2_0_FORMAT;
//    msg_tx.field.brs = CAN_NON_BRS_MODE;
//    msg_tx.field.frameType = CAN_FRAME_DATA;
//    msg_tx.field.idType = CAN_FRAME_STD;
//    msg_tx.field.dlc = DLC_8;
//    msg_tx.data = tx_data;
//
//    while (1)
//    {
//        SYSTEM_ALARM = 0; // System Status : Normal
//
//        // Pressure Switch 1EA or 2EA
//        if (Spare_I == PressureSW_1EA)       TXLowpress = txLowpress;
//        else if (Spare_I == PressureSW_2EA)  TXLowpress = 0;
//
//        //-------------------CAN COMMUNICATION : tx---------------------
//        tx_data[0] = STBY_Start;
//        tx_data[1] = RunLamp;
//        tx_data[2] = Overload;
//        tx_data[3] = ModeStatus; // MANUAL or ST'BY Mode
//        tx_data[4] = RUN_req;
//        tx_data[5] = ResetButton;
//        tx_data[6] = StandByLamp;
//        tx_data[7] = TXLowpress;
//
//        if (CountCanDelay_mS >= CanDelay_mS)
//        {
//            CountCanDelay_mS = 0;
//
//            if (CAN_TX_FIFO_AVAILABLE == (CAN1_TransmitFIFOStatusGet(TXQ) & CAN_TX_FIFO_AVAILABLE))
//                tx_status = CAN1_Transmit(TXQ, &msg_tx);
//        }
//
//        //---------------------POWER RECOVERY AFTER POWER FAIL---------------------
//        if (InitFlag == 1)
//        {
//            if (SeqTime_mS < 630) // 0~62s : UVR
//            {
//                // Push the Reset Button -> STANDBY LAMP ON (Change MAIN Pump : MAIN -> STBY)
//                if (ModeStatus == STBY_MODE &&
//                    ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_ON) ||
//                        (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_ON) ||
//                        (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_ON)))
//                {
//                    RunStatus = STOP;
//                    DATAEE_WriteByte(0x07, STOP);
//                    InitFlag = 0;
//                }
//
//                // After Power Fail (3 ST'BY(2 RUN) - MAIN 1 & MAIN2)
//                else if ((CAN_ID == 1 && rx_data2[1] == RUN && rx_data2[3] == STBY_MODE && rx_data3[1] == RUN && rx_data3[3] == STBY_MODE) ||
//                    (CAN_ID == 2 && rx_data1[1] == RUN && rx_data1[3] == STBY_MODE && rx_data3[1] == RUN && rx_data3[3] == STBY_MODE) ||
//                    (CAN_ID == 3 && rx_data1[1] == RUN && rx_data1[3] == STBY_MODE && rx_data2[1] == RUN && rx_data2[3] == STBY_MODE))
//                {
//                    RunStatus = STOP;
//                    DATAEE_WriteByte(0x07, STOP);
//                    InitFlag = 0;
//                }
//
//                // After Power Fail (3 ST'BY(1 RUN) - ST'BY)
//                else if (ComStatus == StandBy_3_1RUN &&
//                    ((rx_data1[1] == STOP && rx_data1[5] == ResetButton_ON && rx_data1[6] == StandByLamp_ON) ||
//                        (rx_data2[1] == STOP && rx_data2[5] == ResetButton_ON && rx_data2[6] == StandByLamp_ON) ||
//                        (rx_data3[1] == STOP && rx_data3[5] == ResetButton_ON && rx_data3[6] == StandByLamp_ON)))
//                {
//                    RunStatus = STOP;
//                    DATAEE_WriteByte(0x07, STOP);
//                    InitFlag = 0;
//                }
//
//                // Sequential Time (MANUAL RUN / 2 STAND BY RUN & RUN / Power Recovery Before 1s (MAIN -> MAIN))
//                else
//                {
//                    LampSigCont(STOP_SIG, OFF);
//                    LampSigCont(STOP_LAMP, OFF);
//
//                    if (CountSeqTime_mS >= SeqTime_mS)
//                    {
//                        RunStatus = RUN;
//                        InitFlag = 0;
//                        CountSeqTime_mS = 0;
//                    }
//                }
//            }
//            else if (SeqTime_mS == 630) // 63s : UVP
//            {
//                RunStatus = STOP;
//                DATAEE_WriteByte(0x07, STOP);
//                InitFlag = 0;
//            }
//        }
//        else if (InitFlag == 0)    CountSeqTime_mS = 0;
//
//        if (rx_data1[2] == Overload_ON || rx_data2[2] == Overload_ON || rx_data3[2] == Overload_ON)
//            CountOverload_Flag = 1;
//        else
//            CountOverload_Flag = 0;
//
//        //---------------------------FUNCTIONS---------------------------
//        Latch();
//        BuildUpTimeRead();
//        ParallelTimeRead();
//        HeatingOnTimeRead();
//        InputChattProc();
//        RunStopProc(RunStatus);
//        RunInput();
//        HeatProc(HeatStatus);
//        ModeProc(ModeStatus);
//        KeyProc();
//        OverloadProc(Overload_I);
//        LowpressProc(Lowpress_I);
//        ComFailErrorFlag();
//        CanRevMsg();
//        ReceiveRunReq();
//        SendRunReq();
//
//        ConnectFunction();
//        ConnectProc(ComStatus);
//
//        Chattering(CHATTERING2, CHATTERING1, CHATTERING0);
//        StandByLampProc(ModeStatus);
//
//        STBYStartAlarm();
//
//        //---------------------------WRITE EEPROM---------------------------
//        EPRomWrite(RUN_S, RunStatus);
//        EPRomWrite(MODE_S, ModeStatus);
//        EPRomWrite(HEAT_S, HeatStatus);
//        EPRomWrite(COMSTATUS_S, ComStatus);
//    }
//}
//
//void SeqTimeRead(void)
//{
//    unsigned short temp1, temp2, temp3, temp4, temp5, temp6;
//    unsigned short Time = 0;
//
//    TRISD = 0xFF;
//
//    F4 = 0;
//    F5 = 0;
//    F6 = 0;
//
//    F1 = 1;
//    F1_OUT = PORTD;
//    F1_OUT = F1_OUT & 0x3F;
//    F1 = 0;
//
//    TRISD = 0x00;
//
//    if ((F1_OUT & 0x01) == 0x01)
//        temp1 = 0;
//    else if ((F1_OUT & 0x01) == 0x00)
//        temp1 = 10;
//
//    if ((F1_OUT & 0x02) == 0x02)
//        temp2 = 0;
//    else if ((F1_OUT & 0x02) == 0x00)
//        temp2 = 20;
//
//    if ((F1_OUT & 0x04) == 0x04)
//        temp3 = 0;
//    else if ((F1_OUT & 0x04) == 0x00)
//        temp3 = 40;
//
//    if ((F1_OUT & 0x08) == 0x08)
//        temp4 = 0;
//    else if ((F1_OUT & 0x08) == 0x00)
//        temp4 = 80;
//
//    if ((F1_OUT & 0x10) == 0x10)
//        temp5 = 0;
//    else if ((F1_OUT & 0x10) == 0x00)
//        temp5 = 160;
//
//    if ((F1_OUT & 0x20) == 0x20)
//        temp6 = 0;
//    else if ((F1_OUT & 0x20) == 0x00)
//        temp6 = 320;
//
//    Time = temp1 + temp2 + temp3 + temp4 + temp5 + temp6;
//
//    if (Time == 0)      SeqTime_mS = 3;     // Sequential time = 0s -> 300ms
//    else if (Time != 0) SeqTime_mS = Time;
//}
//
//void BuildUpTimeRead(void)
//{
//    unsigned short temp7, temp8, temp9, temp10, temp11, temp12;
//
//    TRISD = 0xFF; // PORTD = INPUT
//
//    F4 = 0;
//    F5 = 0;
//    F6 = 0;
//
//    F1 = 1;
//    F1_OUT = PORTD;
//    BuildUp1 = F1_OUT >> 6;
//    F1 = 0;
//
//    F2 = 1;
//    F2_OUT = PORTD;
//    BuildUp2 = F2_OUT << 2;
//    F2 = 0;
//
//    TRISD = 0x00;
//
//    BuildUp = BuildUp1 | BuildUp2;
//
//    if ((BuildUp & 0x01) == 0x01)
//        temp7 = 0;
//    else if ((BuildUp & 0x01) == 0x00)
//        temp7 = 1;
//
//    if ((BuildUp & 0x02) == 0x02)
//        temp8 = 0;
//    else if ((BuildUp & 0x02) == 0x00)
//        temp8 = 2;
//
//    if ((BuildUp & 0x04) == 0x04)
//        temp9 = 0;
//    else if ((BuildUp & 0x04) == 0x00)
//        temp9 = 4;
//
//    if ((BuildUp & 0x08) == 0x08)
//        temp10 = 0;
//    else if ((BuildUp & 0x08) == 0x00)
//        temp10 = 8;
//
//    if ((BuildUp & 0x10) == 0x10)
//        temp11 = 0;
//    else if ((BuildUp & 0x10) == 0x00)
//        temp11 = 16;
//
//    if ((BuildUp & 0x20) == 0x20)
//        temp12 = 0;
//    else if ((BuildUp & 0x20) == 0x00)
//        temp12 = 32;
//
//    BuildUpTime = temp7 + temp8 + temp9 + temp10 + temp11 + temp12;
//}
//
//void ParallelTimeRead(void)
//{
//    unsigned short temp13, temp14, temp15, temp16, temp17, temp18;
//
//    TRISD = 0xFF;
//
//    F4 = 0;
//    F5 = 0;
//    F6 = 0;
//
//    F2 = 1;
//    F2_OUT = PORTD;
//    F2_OUT = F2_OUT & 0xF0;
//    Parallel1 = F2_OUT >> 4;
//    F2 = 0;
//
//    F3 = 1;
//    F3_OUT = PORTD;
//    F3_OUT = F3_OUT & 0x03;
//    Parallel2 = F3_OUT << 4;
//    F3 = 0;
//
//    TRISD = 0x00;
//
//    Parallel = Parallel1 | Parallel2;
//
//    if ((Parallel & 0x01) == 0x01)
//        temp13 = 0;
//    else if ((Parallel & 0x01) == 0x00)
//        temp13 = 1;
//
//    if ((Parallel & 0x02) == 0x02)
//        temp14 = 0;
//    else if ((Parallel & 0x02) == 0x00)
//        temp14 = 2;
//
//    if ((Parallel & 0x04) == 0x04)
//        temp15 = 0;
//    else if ((Parallel & 0x04) == 0x00)
//        temp15 = 4;
//
//    if ((Parallel & 0x08) == 0x08)
//        temp16 = 0;
//    else if ((Parallel & 0x08) == 0x00)
//        temp16 = 8;
//
//    if ((Parallel & 0x10) == 0x10)
//        temp17 = 0;
//    else if ((Parallel & 0x10) == 0x00)
//        temp17 = 16;
//
//    if ((Parallel & 0x20) == 0x20)
//        temp18 = 0;
//    else if ((Parallel & 0x20) == 0x00)
//        temp18 = 32;
//
//    ParallelTime = temp13 + temp14 + temp15 + temp16 + temp17 + temp18;
//}
//
//void HeatingOnTimeRead(void)
//{
//    unsigned short temp19, temp20, temp21, temp22, temp23, temp24;
//
//    TRISD = 0xFF;
//
//    F4 = 0;
//    F5 = 0;
//    F6 = 0;
//
//    F3 = 1;
//    F3_OUT = PORTD;
//    HeatOn = F3_OUT >> 2;
//    F3 = 0;
//
//    TRISD = 0x00;
//
//    if ((HeatOn & 0x01) == 0x01)
//        temp19 = 0;
//    else if ((HeatOn & 0x01) == 0x00)
//        temp19 = 1;
//
//    if ((HeatOn & 0x02) == 0x02)
//        temp20 = 0;
//    else if ((HeatOn & 0x02) == 0x00)
//        temp20 = 2;
//
//    if ((HeatOn & 0x04) == 0x04)
//        temp21 = 0;
//    else if ((HeatOn & 0x04) == 0x00)
//        temp21 = 4;
//
//    if ((HeatOn & 0x08) == 0x08)
//        temp22 = 0;
//    else if ((HeatOn & 0x08) == 0x00)
//        temp22 = 8;
//
//    if ((HeatOn & 0x10) == 0x10)
//        temp23 = 0;
//    else if ((HeatOn & 0x10) == 0x00)
//        temp23 = 16;
//
//    if ((HeatOn & 0x20) == 0x20)
//        temp24 = 0;
//    else if ((HeatOn & 0x20) == 0x00)
//        temp24 = 32;
//
//    HeatingOnTime = temp19 + temp20 + temp21 + temp22 + temp23 + temp24;
//}
//
//void Latch(void)
//{
//    F4 = 0;
//    F5 = 0;
//    F6 = 0;
//    F = 1;          // OE = LOW
//
//    TRISD = 0x00;   // PORTD = OUTPUT
//
//    PORTD = F4_OUT; // F4 DATA D0~D7
//    F4 = 1;
//    F4 = 0;
//
//    PORTD = F5_OUT; // F5 DATA D0~D7
//    F5 = 1;
//    F5 = 0;
//
//    PORTD = F6_OUT; // F6 DATA D0~D7
//    F6 = 1;
//    F6 = 0;
//
//    TRISD = 0xFF;   // PORTD = INPUT
//}
//
//void LampSigCont(unsigned char name, unsigned char cmd)
//{
//    switch (name)
//    {
//    case ABN_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x01;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0xFE;
//        break;
//    case STAND_BY_LAMP:
//        if (cmd == ON)
//        {
//            F4_OUT = F4_OUT | 0x02;
//            StandByLamp = ON;
//        }
//        else if (cmd == OFF)
//        {
//            F4_OUT = F4_OUT & 0xFD;
//            StandByLamp = OFF;
//        }
//        break;
//    case STOP_LAMP:
//        if (cmd == ON)
//        {
//            F4_OUT = F4_OUT | 0x04;
//            StopLamp = ON;
//        }
//        else if (cmd == OFF)
//        {
//            F4_OUT = F4_OUT & 0xFB;
//            StopLamp = OFF;
//        }
//        break;
//    case LOW_PRESS_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x08;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0xF7;
//        break;
//    case COM_FAULT_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x10;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0xEF;
//        break;
//    case HEAT_ON_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x20;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0xDF;
//        break;
//    case MODE_MAUAL_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x40;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0xBF;
//        break;
//    case MODE_STBY_LAMP:
//        if (cmd == ON)       F4_OUT = F4_OUT | 0x80;
//        else if (cmd == OFF) F4_OUT = F4_OUT & 0x7F;
//        break;
//    case RUN_LAMP:
//        if (cmd == ON)
//        {
//            F5_OUT = F5_OUT | 0x01;
//            RunLamp = ON;
//        }
//        else if (cmd == OFF)
//        {
//            F5_OUT = F5_OUT & 0xFE;
//            RunLamp = OFF;
//        }
//        break;
//    case RUN_SIG:
//        if (cmd == ON)       F6_OUT = F6_OUT | 0x01;
//        else if (cmd == OFF) F6_OUT = F6_OUT & 0xFE;
//        break;
//    case HEATING_SIG:
//        if (cmd == ON)       F6_OUT = F6_OUT | 0x02;
//        else if (cmd == OFF) F6_OUT = F6_OUT & 0xFD;
//        break;
//    case STOP_SIG:
//        if (cmd == ON)       F6_OUT = F6_OUT | 0x04;
//        else if (cmd == OFF) F6_OUT = F6_OUT & 0xFB;
//        break;
//    case STBY_START_ALARM:
//        if (cmd == ON)       F6_OUT = F6_OUT | 0x08;
//        else if (cmd == OFF) F6_OUT = F6_OUT & 0xF7;
//        break;
//    case STAND_BY_SIG:
//        if (cmd == ON)       F6_OUT = F6_OUT | 0x10;
//        else if (cmd == OFF) F6_OUT = F6_OUT & 0xEF;
//        break;
//    default:
//        break;
//    }
//}
//
//void ERPomRead(unsigned char command)
//{
//    switch (command)
//    {
//    case RUN_S:
//        RunEE = DATAEE_ReadByte(0x07);
//        // RunEE = 0 : STOP / 1 : RUN
//        break;
//    case MODE_S:
//        ModeEE = DATAEE_ReadByte(0x04);
//        // ModeEE = 0 : MANUAL / 1 : STBY
//        break;
//    case HEAT_S:
//        HeatEE = DATAEE_ReadByte(0x05);
//        // HeatEE = 0 : HEAT OFF / 1 : HEAT ON
//        break;
//    case COMSTATUS_S:
//        ComStatusEE = DATAEE_ReadByte(0x08);
//        break;
//    default:
//        break;
//    }
//}
//
//void EPRomWrite(unsigned char command, unsigned char value) // value = now status
//{
//    if (command == RUN_S)
//    {
//        if (OldRunEEStatus != value)
//        {
//            DATAEE_WriteByte(0x0007, value);
//            OldRunEEStatus = value;
//        }
//    }
//
//    if (command == MODE_S)
//    {
//        if (OldModeEEStatus != value)
//        {
//            DATAEE_WriteByte(0x0004, value);
//            OldModeEEStatus = value;
//        }
//    }
//
//    if (command == HEAT_S)
//    {
//        if (OldHeatEEStatus != value)
//        {
//            DATAEE_WriteByte(0x0005, value);
//            OldHeatEEStatus = value;
//        }
//    }
//
//    if (command == COMSTATUS_S)
//    {
//        if (OldComStatusEEStatus != value)
//        {
//            DATAEE_WriteByte(0x0008, value);
//            OldComStatusEEStatus = value;
//        }
//    }
//}
//
//void InputChattProc(void)
//{
//    if (RUN_I == ON && BTCount_ms[0] > BT_Delayms)
//    {
//        BTCount_ms[1] = 0;
//        RunFB_I = ON;
//    }
//    else if (RUN_I == OFF && BTCount_ms[1] > BT_Delayms)
//    {
//        BTCount_ms[0] = 0;
//        RunFB_I = OFF;
//    }
//
//    if (START_REMOTE_I == ON && BTCount_ms[2] > BT_Delayms)
//    {
//        BTCount_ms[3] = 0;
//        RunRemote_I = ON;
//    }
//    else if (START_REMOTE_I == OFF && BTCount_ms[3] > BT_Delayms)
//    {
//        BTCount_ms[2] = 0;
//        RunRemote_I = OFF;
//    }
//
//    if (STOP_REMOTE_I == ON && BTCount_ms[4] > BT_Delayms)
//    {
//        BTCount_ms[5] = 0;
//        StopRemote_I = ON;
//    }
//    else if (STOP_REMOTE_I == OFF && BTCount_ms[5] > BT_Delayms)
//    {
//        BTCount_ms[4] = 0;
//        StopRemote_I = OFF;
//    }
//
//    if (OVERLOAD_I == ON && BTCount_ms[6] > BT_Delayms)
//    {
//        BTCount_ms[7] = 0;
//        Overload_I = ON;
//    }
//    else if (OVERLOAD_I == OFF && BTCount_ms[7] > BT_Delayms)
//    {
//        BTCount_ms[6] = 0;
//        Overload_I = OFF;
//    }
//
//    if (LOW_PRESS_I == ON && BTCount_ms[8] > LowPressChattTime)
//    {
//        BTCount_ms[9] = 0;
//        Lowpress_I = ON;
//    }
//    else if (LOW_PRESS_I == OFF && BTCount_ms[9] > BT_Delayms)
//    {
//        BTCount_ms[8] = 0;
//        Lowpress_I = OFF;
//    }
//}
//
//void CanIdRead(unsigned char value1, unsigned char value2)
//{
//    if (value1 == 0 && value2 == 1)
//    {
//        MSG_ID = 0x100;
//        CAN_ID = 1;
//    }
//    else if (value1 == 1 && value2 == 0)
//    {
//        MSG_ID = 0x200;
//        CAN_ID = 2;
//    }
//    else if (value1 == 0 && value2 == 0)
//    {
//        MSG_ID = 0x300;
//        CAN_ID = 3;
//    }
//}
//
//void RunStopCont(unsigned char command)
//{
//    if (command == RUN)
//    {
//        LampSigCont(STOP_SIG, OFF);
//        LampSigCont(RUN_SIG, ON);
//    }
//    else if (command == STOP)
//    {
//        LampSigCont(RUN_SIG, OFF);
//        LampSigCont(STOP_LAMP, ON);
//    }
//}
//
//void RunStopProc(unsigned char command)
//{
//    if (command == RUN)
//    {
//        if (FirstRunStatus == 0)                  FirstRunStatus = 1;
//
//        LampSigCont(RUN_SIG, ON);
//
//        ResetButton = OFF;
//        CountStopPulse_mS = 0;
//
//        LampSigCont(STOP_SIG, OFF);
//        LampSigCont(STOP_LAMP, OFF);
//    }
//    else if (command == STOP)
//    {
//        LampSigCont(STOP_LAMP, ON);
//        LampSigCont(RUN_SIG, OFF);
//        LampSigCont(RUN_LAMP, OFF);
//
//        if (FirstRunStatus == 1 && CountStopPulse_mS < StopPulse)   LampSigCont(STOP_SIG, ON);
//        else if (CountStopPulse_mS >= StopPulse)
//        {
//            CountStopPulse_mS = StopPulse;
//            LampSigCont(STOP_SIG, OFF);
//        }
//    }
//}
//
//void RunInput(void)
//{
//    if (RunFB_I == ON) // Run Signal ON & Run Input ON -> Run Lamp ON
//    {
//        LampSigCont(RUN_LAMP, ON);
//        LampSigCont(STOP_LAMP, OFF);
//    }
//}
//
//void HeatCont(unsigned char command)
//{
//    if (command == HEAT_OFF)
//    {
//        LampSigCont(HEAT_ON_LAMP, OFF);
//        LampSigCont(HEATING_SIG, OFF);
//
//        CountHeatingOnTime_S = 0;
//    }
//    else if (command == HEAT_ON)    LampSigCont(HEAT_ON_LAMP, ON);
//}
//
//void HeatProc(unsigned char command)
//{
//    if (command == HEAT_OFF)
//    {
//        LampSigCont(HEAT_ON_LAMP, OFF);
//        LampSigCont(HEATING_SIG, OFF);
//
//        CountHeatingOnTime_S = 0;
//    }
//    else if (command == HEAT_ON)
//    {
//        LampSigCont(HEAT_ON_LAMP, ON);
//
//        if (RunStatus == STOP)
//        {
//            if (CountHeatingOnTime_S >= HeatingOnTime)  LampSigCont(HEATING_SIG, ON);
//        }
//        else if (RunStatus == RUN)
//        {
//            LampSigCont(HEATING_SIG, OFF);
//
//            CountHeatingOnTime_S = 0;
//        }
//    }
//}
//
//void ModeProc(unsigned char command)
//{
//    if (command == MANUAL_MODE)
//    {
//        LampSigCont(MODE_MAUAL_LAMP, ON);
//        LampSigCont(MODE_STBY_LAMP, OFF);
//    }
//    else if (command == STBY_MODE)
//    {
//        LampSigCont(MODE_STBY_LAMP, ON);
//        LampSigCont(MODE_MAUAL_LAMP, OFF);
//    }
//}
//
//void KeyProc(void)
//{
//    //-------------------------RUN/STOP BUTTON-------------------------
//    if (Overload_I == OFF)
//    {
//        //-------------------------RUN-------------------------
//        if (RunStatus == STOP)
//        {
//            if (ModeStatus == MANUAL_MODE)
//            {
//                if (OldStartPB == 1 && START_PB_I == 1)
//                {
//                    OldStartPB = 0;
//                    RunStatus = RUN;
//                }
//                else if (OldStartPB == 0 && START_PB_I == 0)  OldStartPB = 1;
//
//                if (RunRemote_I == 1)    RunStatus = RUN;
//            }
//        }
//
//        //-------------------------STOP-------------------------
//        if (OldStopPB == 1 && STOP_PB_I == 1)
//        {
//            OldStopPB = 0;
//            RunStatus = STOP;
//        }
//        else if (OldStopPB == 0 && STOP_PB_I == 0)
//        {
//            if (rx_data1[0] == 1 || rx_data2[0] == 1 || rx_data3[0] == 1)    ResetButton = ON;
//            else
//                ResetButton = OFF;
//
//            OldStopPB = 1;
//        }
//
//        if (StopRemote_I == 1)   RunStatus = STOP;
//    }
//
//    //-------------------------HEAT BUTTON-------------------------
//    if (OldHeatPB == 1 && HEATER_PB_I == 1)
//    {
//        OldHeatPB = 0;
//
//        if (HeatStatus == HEAT_OFF)               HeatStatus = HEAT_ON;
//        else if (HeatStatus == HEAT_ON)           HeatStatus = HEAT_OFF;
//    }
//    else if (OldHeatPB == 0 && HEATER_PB_I == 0)  OldHeatPB = 1;
//
//    //-------------------------MODE BUTTON-------------------------
//    if (OldModePB == 1 && MODE_PB_I == 1)
//    {
//        OldModePB = 0;
//
//        if (ModeStatus == MANUAL_MODE)            ModeStatus = STBY_MODE;
//        else if (ModeStatus == STBY_MODE)         ModeStatus = MANUAL_MODE;
//    }
//    else if (OldModePB == 0 && MODE_PB_I == 0)    OldModePB = 1;
//}
//
//void OverloadProc(unsigned char command)
//{
//    if (command == ON)
//    {
//        if (RunStatus == RUN)
//        {
//            Stop_Overload = 1;
//            RunStatus = STOP;
//        }
//
//        LampSigCont(ABN_LAMP, ON);
//        Overload = ON;
//        CountParallelTime_S = 0;
//
//        ResetButton = OFF;
//
//        if (StandByLamp == ON)
//        {
//            LampSigCont(STAND_BY_LAMP, OFF); // Occur Overload -> StandBy x
//            STBY_Overload = 1; // STBY Overload -> Run Request x
//        }
//    }
//    else if (command == OFF)
//    {
//        LampSigCont(ABN_LAMP, OFF);
//        Overload = OFF;
//        CountRunReq_S = 0;
//
//        STBY_Overload = 0;
//        Stop_Overload = 0;
//    }
//}
//
//void LowpressProc(unsigned char command)
//{
//    if (command == ON)
//    {
//        LampSigCont(LOW_PRESS_LAMP, ON);
//        Lowpress = ON;
//
//        txLowpress = ON;
//    }
//    else if (ComStatus == StandBy_2 && (rx_data1[7] == ON || rx_data2[7] == ON || rx_data3[7] == ON))
//    {
//        LampSigCont(LOW_PRESS_LAMP, ON);
//        Lowpress = ON;
//
//        txLowpress = ON;
//    }
//    else if (command == OFF || (ComStatus == StandBy_2 && (rx_data1[7] == OFF && rx_data2[7] == OFF && rx_data3[7] == OFF)))
//    {
//        LampSigCont(LOW_PRESS_LAMP, OFF);
//        Lowpress = OFF;
//
//        CountBuildUpTime_S = 0;
//        CountBuildUpStart = OFF;
//
//        txLowpress = OFF;
//    }
//
//    //--------------------------BUILD UP TIME SETTING--------------------------
//    // RUN -> LowPressure
//    if (OldRunStatus == RUN && RunStatus == RUN)
//    {
//        if (OldLowpress == OFF && Lowpress == ON)
//            CountBuildUpTime = 3; // 3 seconds
//    }
//    // STOP -> LowPressure -> RUN
//    if (OldLowpress == ON && Lowpress == ON)
//    {
//        if (OldRunStatus == STOP && RunStatus == RUN)
//        {
//            if (StandByLamp == OFF) CountBuildUpTime = BuildUpTime; // Setting value
//            else if (StandByLamp == ON) CountBuildUpTime = 3; // 3 seconds
//        }
//    }
//
//    if (Lowpress == ON && RunLamp == ON && ((rx_data1[1] == STOP && rx_data1[6] == StandByLamp_ON) ||
//        (rx_data2[1] == STOP && rx_data2[6] == StandByLamp_ON) ||
//        (rx_data3[1] == STOP && rx_data3[6] == StandByLamp_ON)))
//        CountBuildUpStart = ON;
//    else
//        CountBuildUpStart = OFF;
//
//    //--------------------------PARALLEL TIME COUNT START (AFTER BUILD UP TIME OVER)--------------------------
//    if (RunLamp == ON && StandByLamp == ON && Lowpress == ON)
//    {
//        if (CountBuildUpTime_S >= CountBuildUpTime)
//        {
//            CountParaStart = OFF;
//            CountParallelTime_S = 0;
//            CountBuildUpStart = OFF;
//            CountBuildUpTime_S = 0;
//        }
//    }
//    else if (Lowpress == ON && ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_ON) ||
//        (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_ON) ||
//        (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_ON)))
//    {
//        if (CountBuildUpTime_S >= CountBuildUpTime)
//        {
//            CountParaStart = ON;
//            CountBuildUpStart = OFF;
//            CountBuildUpTime_S = 0;
//        }
//    }
//
//    //--------------------------PARALLEL TIME COUNT OVER--------------------------
//    if (CountParaStart == ON)
//    {
//        if (CountParallelTime_S >= ParallelTime)
//        {
//            RunStatus = STOP;
//            CountParaStart = OFF;
//            CountParallelTime_S = 0;
//        }
//    }
//
//    OldLowpress = Lowpress;
//    OldRunStatus = RunStatus;
//}
//
//void ComFailErrorFlag(void)
//{
//    //--------------------------COM FAIL - CAN ID 1--------------------------
//    if (CountComFault1_S > ComFault_S && Error_Flag1 == Com_Normal && CAN_ID != 1)
//    {
//        Error_Flag1 = Com_Error;
//
//        if (StandByLamp == ON && rx_data1[1] == RUN)    RunStatus = RUN; // Auto Change Over
//
//        // Reset Received Data
//        rx_data1[1] = 0;
//        rx_data1[2] = 0;
//        rx_data1[3] = 0;
//        rx_data1[4] = 0;
//        rx_data1[5] = 0;
//        rx_data1[6] = 0;
//        rx_data1[7] = 0;
//    }
//    else if (CountComInit == ComInit_S || CountComInit > ComInit_S)
//    {
//        if (CountComFault1_S <= ComFault_S && Error_Flag1 == Com_Error && CAN_ID != 1)
//            Error_Flag1 = Com_Normal;
//    }
//
//    // MANUAL Mode -> No Connect
//    if (rx_data1[3] == MANUAL_MODE && CAN_ID != 1)    Error_Flag1 = Com_Error;
//    else if (rx_data1[3] == STBY_MODE && CAN_ID != 1) Error_Flag1 = Com_Normal;
//
//    //--------------------------COM FAIL - CAN ID 2--------------------------
//    if (CountComFault2_S > ComFault_S && Error_Flag2 == Com_Normal && CAN_ID != 2)
//    {
//        Error_Flag2 = Com_Error;
//
//        if (StandByLamp == ON && rx_data2[1] == RUN)    RunStatus = RUN; // Auto Change Over
//
//        // Reset Received Data
//        rx_data2[1] = 0;
//        rx_data2[2] = 0;
//        rx_data2[3] = 0;
//        rx_data2[4] = 0;
//        rx_data2[5] = 0;
//        rx_data2[6] = 0;
//        rx_data2[7] = 0;
//    }
//    else if (CountComInit == ComInit_S || CountComInit > ComInit_S)
//    {
//        if (CountComFault2_S <= ComFault_S && Error_Flag2 == Com_Error && CAN_ID != 2)
//            Error_Flag2 = Com_Normal;
//    }
//
//    // MANUAL Mode -> No Connect
//    if (rx_data2[3] == MANUAL_MODE && CAN_ID != 2)    Error_Flag2 = Com_Error;
//    else if (rx_data2[3] == STBY_MODE && CAN_ID != 2) Error_Flag2 = Com_Normal;
//
//    //--------------------------COM FAIL - CAN ID 3--------------------------
//    if (CountComFault3_S > ComFault_S && Error_Flag3 == Com_Normal && CAN_ID != 3)
//    {
//        Error_Flag3 = Com_Error;
//
//        if (StandByLamp == ON && rx_data3[1] == RUN)    RunStatus = RUN; // Auto Change Over
//
//        // Reset Received Data
//        rx_data3[1] = 0;
//        rx_data3[2] = 0;
//        rx_data3[3] = 0;
//        rx_data3[4] = 0;
//        rx_data3[5] = 0;
//        rx_data3[6] = 0;
//        rx_data3[7] = 0;
//    }
//    else if (CountComInit == ComInit_S || CountComInit > ComInit_S)
//    {
//        if (CountComFault3_S <= ComFault_S && Error_Flag3 == Com_Error && CAN_ID != 3)
//            Error_Flag3 = Com_Normal;
//    }
//
//    // MANUAL Mode -> No Connect
//    if (rx_data3[3] == MANUAL_MODE && CAN_ID != 3)    Error_Flag3 = Com_Error;
//    else if (rx_data3[3] == STBY_MODE && CAN_ID != 3) Error_Flag3 = Com_Normal;
//}
//
//void CanRevMsg(void)
//{
//    nrMsg = CAN1_ReceivedMessageCountGet();
//
//    if (nrMsg > 0) // Normal Status (Receiving Something)
//    {
//        if (true == CAN1_Receive(&msg_rx))
//        {
//            if (msg_rx.msgId == 0x100 && CAN_ID != 1) // Match Frame ID - CAN ID 1
//            {
//                CountComFault1_S = 0; // Count for ComFail - CAN ID 1
//
//                for (unsigned char i = 0; i < msg_rx.field.dlc; i++)
//                {
//                    rx_data1[i] = msg_rx.data[i];
//                }
//            }
//            if (msg_rx.msgId == 0x200 && CAN_ID != 2) // Match Frame ID - CAN ID 2
//            {
//                CountComFault2_S = 0; // Count for ComFail - CAN ID 2
//
//                for (unsigned char i = 0; i < msg_rx.field.dlc; i++)
//                {
//                    rx_data2[i] = msg_rx.data[i];
//                }
//            }
//            if (msg_rx.msgId == 0x300 && CAN_ID != 3) // Match Frame ID - CAN ID 3
//            {
//                CountComFault3_S = 0; // Count for ComFail - CAN ID 3
//
//                for (unsigned char i = 0; i < msg_rx.field.dlc; i++)
//                {
//                    rx_data3[i] = msg_rx.data[i];
//                }
//            }
//        }
//        nrMsg--;
//    }
//}
//
///*
//[0] STBY_Start     [1] RunLamp        [2] Overload       [3] ModeStatus
//[4] RUN_req        [5] ResetButton    [6] StandByLamp    [7] Lowpress
//*/
//
//void StandByLampProc(unsigned char mode)
//{
//    if (mode == STBY_MODE)
//    {
//        if (ComStatus == StandBy_3)
//        {
//            // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP   Lowpress == OFF && 
//            if (Overload == OFF && ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_ON) ||
//                (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_ON) ||
//                (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_ON)))
//            {
//                if (ResetButton == ON)    LampSigCont(STAND_BY_LAMP, ON); // CHANGE MAIN PUMP (MAIN -> STBY)
//            }
//
//            // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP  (rx_data1[7] == Lowpress_OFF || rx_data2[7] == Lowpress_OFF || rx_data3[7] == Lowpress_OFF) && 
//            else if (RunLamp == ON && StandByLamp == ON)
//            {
//                if ((rx_data1[1] == STOP && rx_data1[5] == ResetButton_ON) || (rx_data2[1] == STOP && rx_data2[5] == ResetButton_ON) || (rx_data3[1] == STOP && rx_data3[5] == ResetButton_ON))
//                    LampSigCont(STAND_BY_LAMP, OFF); // CHANGE MAIN PUMP (STBY -> MAIN)
//            }
//
//            // AFTER POWER RECOVERY
//            else if ((CAN_ID == 1 && ((rx_data2[1] == RUN && rx_data2[6] == StandByLamp_OFF && rx_data3[1] == STOP && rx_data3[6] == StandByLamp_ON) || (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_OFF && rx_data2[1] == STOP && rx_data2[6] == StandByLamp_ON)))
//                || (CAN_ID == 2 && ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_OFF && rx_data3[1] == STOP && rx_data3[6] == StandByLamp_ON) || (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_OFF && rx_data1[1] == STOP && rx_data1[6] == StandByLamp_ON)))
//                || (CAN_ID == 3 && ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_OFF && rx_data2[1] == STOP && rx_data2[6] == StandByLamp_ON) || (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_OFF && rx_data1[1] == STOP && rx_data1[6] == StandByLamp_ON))))
//                LampSigCont(STAND_BY_LAMP, OFF);
//
//            // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON   Lowpress == OFF && 
//            else if (((rx_data1[1] == RUN && rx_data1[3] == STBY_MODE && rx_data1[6] == StandByLamp_OFF)
//                || (rx_data2[1] == RUN && rx_data2[3] == STBY_MODE && rx_data2[6] == StandByLamp_OFF)
//                || (rx_data3[1] == RUN && rx_data3[3] == STBY_MODE && rx_data3[6] == StandByLamp_OFF))
//                && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                LampSigCont(STAND_BY_LAMP, ON);
//
//            // NOMAL - MAIN PUMP : STBY MODE & RUN -> STOP  /  ST'BY PUMP : STAND BY LAMP ON -> OFF
//            else if ((rx_data1[1] == STOP && rx_data1[2] == Overload_OFF && rx_data1[3] == STBY_MODE)
//                || (rx_data2[1] == STOP && rx_data2[2] == Overload_OFF && rx_data2[3] == STBY_MODE)
//                || (rx_data3[1] == STOP && rx_data3[2] == Overload_OFF && rx_data3[3] == STBY_MODE))
//                LampSigCont(STAND_BY_LAMP, OFF);
//
//            // NORMAL - MAIN PUMP : MANUAL MODE & RUN  /  ST'BY PUMP : STAND BY LAMP OFF
//            else if ((rx_data1[1] == RUN && rx_data1[3] == MANUAL_MODE) || (rx_data2[1] == RUN && rx_data2[3] == MANUAL_MODE) || (rx_data3[1] == RUN && rx_data3[3] == MANUAL_MODE))
//                LampSigCont(STAND_BY_LAMP, OFF);
//        }
//
//        else if (ComStatus == StandBy_3_1RUN)
//        {
//            if (rx_data1[1] == RUN && rx_data1[6] == StandByLamp_OFF)
//            {
//                if (CAN_ID == 2)
//                {
//                    if (rx_data3[6] == StandByLamp_OFF)
//                    {
//                        // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                        if (rx_data1[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                            LampSigCont(STAND_BY_LAMP, ON);
//                    }
//                    if (rx_data3[6] == StandByLamp_ON)  LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                else if (CAN_ID == 3)
//                {
//                    if (Overload == ON && rx_data2[2] == Overload_ON)    CountStandByCheck_mS = 0;
//
//                    else if (Overload == OFF && (rx_data2[2] == Overload_ON || rx_data2[6] == StandByLamp_OFF) && CountStandByCheck_mS >= 5)
//                    {
//                        if (rx_data2[6] == StandByLamp_OFF)
//                        {
//                            // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                            if (rx_data1[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                                LampSigCont(STAND_BY_LAMP, ON);
//                        }
//                    }
//
//                    if (rx_data2[6] == StandByLamp_ON)  LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                if (rx_data1[3] == MANUAL_MODE)    LampSigCont(STAND_BY_LAMP, OFF);
//            }
//            else if (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_OFF)
//            {
//                if (CAN_ID == 3)
//                {
//                    if (rx_data1[6] == StandByLamp_OFF)
//                    {
//                        // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                        if (rx_data2[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                            LampSigCont(STAND_BY_LAMP, ON);
//                    }
//                    if (rx_data1[6] == StandByLamp_ON)  LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                else if (CAN_ID == 1)
//                {
//                    if (Overload == ON && rx_data3[2] == Overload_ON)    CountStandByCheck_mS = 0;
//
//                    else if (Overload == OFF && (rx_data3[2] == Overload_ON || rx_data3[6] == StandByLamp_OFF) && CountStandByCheck_mS >= 5)
//                    {
//                        if (rx_data3[6] == StandByLamp_OFF)
//                        {
//                            // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                            if (rx_data2[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                                LampSigCont(STAND_BY_LAMP, ON);
//                        }
//                    }
//
//                    if (rx_data3[6] == StandByLamp_ON)  LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                if (rx_data2[3] == MANUAL_MODE)    LampSigCont(STAND_BY_LAMP, OFF);
//            }
//            else if (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_OFF)
//            {
//                if (CAN_ID == 1)
//                {
//                    if (rx_data2[6] == StandByLamp_OFF)
//                    {
//                        // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                        if (rx_data3[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                            LampSigCont(STAND_BY_LAMP, ON);
//                    }
//                    else if (rx_data2[6] == StandByLamp_ON)  LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                else if (CAN_ID == 2)
//                {
//                    if (Overload == ON && rx_data1[2] == Overload_ON)    CountStandByCheck_mS = 0;
//
//                    else if (Overload == OFF && (rx_data1[2] == Overload_ON || rx_data1[6] == StandByLamp_OFF) && CountStandByCheck_mS >= 5)
//                    {
//                        if (rx_data1[6] == StandByLamp_OFF)
//                        {
//                            // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                            if (rx_data3[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && StopLamp == ON && InitFlag == 0)
//                                LampSigCont(STAND_BY_LAMP, ON);
//                        }
//                    }
//
//                    if (rx_data1[6] == StandByLamp_ON)   LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                if (rx_data3[3] == MANUAL_MODE)    LampSigCont(STAND_BY_LAMP, OFF);
//            }
//            else
//            {
//                CountStandByCheck_mS = 0;
//
//                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
//                if (Overload == OFF && ((rx_data1[1] == RUN && rx_data1[6] == StandByLamp_ON) || (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_ON) || (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_ON)))
//                {
//                    if (ResetButton == ON)
//                        LampSigCont(STAND_BY_LAMP, ON); // CHANGE MAIN PUMP (MAIN -> STBY)
//                }
//
//                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
//                else if (RunLamp == ON && StandByLamp == ON)
//                {
//                    if ((rx_data1[1] == STOP && rx_data1[5] == ResetButton_ON) || (rx_data2[1] == STOP && rx_data2[5] == ResetButton_ON) || (rx_data3[1] == STOP && rx_data3[5] == ResetButton_ON))
//                        LampSigCont(STAND_BY_LAMP, OFF); // CHANGE MAIN PUMP (STBY -> MAIN)
//                }
//
//                if (CAN_ID == 1 && StandByLamp == ON)
//                {
//                    if (rx_data2[2] == Overload_OFF && rx_data3[2] == Overload_OFF)
//                    {
//                        if (rx_data2[1] == STOP && rx_data2[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//
//                        else if (rx_data2[1] == RUN && rx_data2[3] == MANUAL_MODE)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                    else if (CountOverload_S > 1)
//                    {
//                        if (rx_data2[1] == STOP && rx_data2[2] == Overload_OFF && rx_data2[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data3[1] == STOP && rx_data3[2] == Overload_OFF && rx_data3[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data2[2] == Overload_ON && rx_data3[2] == Overload_ON && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                }
//                else if (CAN_ID == 2 && StandByLamp == ON)
//                {
//                    if (rx_data3[2] == Overload_OFF && rx_data1[2] == Overload_OFF)
//                    {
//                        if (rx_data3[1] == STOP && rx_data3[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//
//                        else if (rx_data3[1] == RUN && rx_data3[3] == MANUAL_MODE)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                    else if (CountOverload_S > 1)
//                    {
//                        if (rx_data1[1] == STOP && rx_data1[2] == Overload_OFF && rx_data1[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data3[1] == STOP && rx_data3[2] == Overload_OFF && rx_data3[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data3[2] == Overload_ON && rx_data1[2] == Overload_ON && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                }
//                else if (CAN_ID == 3 && StandByLamp == ON)
//                {
//                    if (rx_data1[2] == Overload_OFF && rx_data2[2] == Overload_OFF)
//                    {
//                        if (rx_data1[1] == STOP && rx_data1[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//
//                        else if (rx_data1[1] == RUN && rx_data1[3] == MANUAL_MODE)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                    else if (CountOverload_S > 1)
//                    {
//                        if (rx_data1[1] == STOP && rx_data1[2] == Overload_OFF && rx_data1[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data2[1] == STOP && rx_data2[2] == Overload_OFF && rx_data2[3] == STBY_MODE && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                        else if (rx_data1[2] == Overload_ON && rx_data2[2] == Overload_ON && RunLamp == OFF)
//                            LampSigCont(STAND_BY_LAMP, OFF);
//                    }
//                }
//            }
//        }
//
//        else if (ComStatus == StandBy_2 || ComStatus == StandBy_3to2 || ComStatus == StandBy_3to2_1RUN)
//        {
//            StandBy_2_Flag = 1;
//
//            if (CountStandBy_2_mS >= 5)
//            {
//                if (Error_Flag1 == Com_Normal)
//                {
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
//                    if (Overload == OFF && (rx_data1[1] == RUN && rx_data1[6] == StandByLamp_ON))
//                    {
//                        if (ResetButton == ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, ON);  // CHANGE MAIN PUMP (MAIN -> STBY)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
//                    else if (rx_data1[2] == Overload_OFF && RunLamp == ON && StandByLamp == ON)
//                    {
//                        if (rx_data1[1] == STOP && rx_data1[5] == ResetButton_ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, OFF); // CHANGE MAIN PUMP (STBY -> MAIN)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                    else if (rx_data1[1] == RUN && rx_data1[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && InitFlag == 0 && StopLamp == ON)
//                        LampSigCont(STAND_BY_LAMP, ON);
//
//                    // NOMAL - MAIN PUMP : STBY MODE & RUN -> STOP  /  ST'BY PUMP : STAND BY LAMP ON -> OFF
//                    else if (StandBy_3_1RUN_Flag == 0 && rx_data1[1] == STOP && rx_data1[2] == Overload_OFF && rx_data1[3] == STBY_MODE && RunLamp == OFF)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//
//                    // NORMAL - MAIN PUMP : MANUAL MODE & RUN  /  ST'BY PUMP : STAND BY LAMP OFF
//                    else if (rx_data1[1] == RUN && rx_data1[3] == MANUAL_MODE)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                else if (Error_Flag2 == Com_Normal)
//                {
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
//                    if (Overload == OFF && (rx_data2[1] == RUN && rx_data2[6] == StandByLamp_ON))
//                    {
//                        if (ResetButton == ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, ON); // CHANGE MAIN PUMP (MAIN -> STBY)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
//                    else if (RunLamp == ON && StandByLamp == ON)
//                    {
//                        if (rx_data2[1] == STOP && rx_data2[5] == ResetButton_ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, OFF); // CHANGE MAIN PUMP (STBY -> MAIN)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                    else if (rx_data2[1] == RUN && rx_data2[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && InitFlag == 0 && StopLamp == ON)
//                        LampSigCont(STAND_BY_LAMP, ON);
//
//                    // NOMAL - MAIN PUMP : STBY MODE & RUN -> STOP  /  ST'BY PUMP : STAND BY LAMP ON -> OFF
//                    else if (StandBy_3_1RUN_Flag == 0 && rx_data2[1] == STOP && rx_data2[2] == Overload_OFF && rx_data2[3] == STBY_MODE && RunLamp == OFF)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//
//                    // NORMAL - MAIN PUMP : MANUAL MODE & RUN  /  ST'BY PUMP : STAND BY LAMP OFF
//                    else if (rx_data2[1] == RUN && rx_data2[3] == MANUAL_MODE)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//                }
//
//                else if (Error_Flag3 == Com_Normal)
//                {
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
//                    if (Overload == OFF && (rx_data3[1] == RUN && rx_data3[6] == StandByLamp_ON))
//                    {
//                        if (ResetButton == ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, ON); // CHANGE MAIN PUMP (MAIN -> STBY)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
//                    else if (RunLamp == ON && StandByLamp == ON)
//                    {
//                        if (rx_data3[1] == STOP && rx_data3[5] == ResetButton_ON)
//                        {
//                            LampSigCont(STAND_BY_LAMP, OFF); // CHANGE MAIN PUMP (STBY -> MAIN)
//                            if (StandBy_3_1RUN_Flag == 1)    StandBy_3_1RUN_Flag = 0;
//                        }
//                    }
//
//                    // NORMAL - MAIN PUMP : STBY MODE & RUN  /  ST'BY PUMP : STAND BY LAMP ON
//                    else if (rx_data3[1] == RUN && rx_data3[3] == STBY_MODE && Overload == OFF && RunLamp == OFF && InitFlag == 0 && StopLamp == ON)
//                        LampSigCont(STAND_BY_LAMP, ON);
//
//                    // NOMAL - MAIN PUMP : STBY MODE & RUN -> STOP  /  ST'BY PUMP : STAND BY LAMP ON -> OFF
//                    else if (StandBy_3_1RUN_Flag == 0 && rx_data3[1] == STOP && rx_data3[2] == Overload_OFF && rx_data3[3] == STBY_MODE && RunLamp == OFF)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//
//                    // NORMAL - MAIN PUMP : MANUAL MODE & RUN  /  ST'BY PUMP : STAND BY LAMP OFF
//                    else if (rx_data3[1] == RUN && rx_data3[3] == MANUAL_MODE)
//                        LampSigCont(STAND_BY_LAMP, OFF);
//                }
//            }
//        }
//    }
//    else if (mode == MANUAL_MODE) LampSigCont(STAND_BY_LAMP, OFF);
//
//    //---------------------------STAND BY SIGNAL OUT---------------------------
//    if (StandByLamp == ON)        LampSigCont(STAND_BY_SIG, ON);
//    else if (StandByLamp == OFF)  LampSigCont(STAND_BY_SIG, OFF);
//}
//
//void SendRunReq(void) // MAIN PUMP
//{
//    if (ModeStatus == STBY_MODE && ComStatus != NoConnection)
//    {
//        if (Lowpress == ON && RunLamp == ON && CountBuildUpTime_S == CountBuildUpTime && RUN_req == OFF)
//            RUN_req = ON;
//        else if (Stop_Overload == 1 && Overload == ON && STBY_Overload == 0 && CountRunReq_S == RunReq_S && RUN_req == OFF)
//            RUN_req = ON;
//        else if (Overload == OFF && Lowpress == OFF && RunLamp == OFF && (rx_data1[1] == RUN || rx_data2[1] == RUN || rx_data3[1] == RUN) && RUN_req == ON)
//            RUN_req = OFF;
//        else if (Lowpress == ON && (rx_data1[1] == RUN || rx_data2[1] == RUN || rx_data3[1] == RUN) && RunLamp == OFF && RUN_req == ON)
//            RUN_req = OFF;
//        else if (Overload == ON && CountRunReq_S > RunReq_S && RUN_req == ON)
//            RUN_req = OFF;
//    }
//}
//
//void ReceiveRunReq(void) // ST'BY PUMP
//{
//    if (Overload == OFF && StandByLamp == ON)
//    {
//        if (Request_Flag == 0 && (rx_data1[4] == ON || rx_data2[4] == ON || rx_data3[4] == ON)) // Run Request
//        {
//            Request_Flag = 1;
//            RunStatus = RUN;
//        }
//        else if (rx_data1[4] == OFF && rx_data2[4] == OFF && rx_data3[4] == OFF) // Run Request Cancle
//            Request_Flag = 0;
//    }
//}
//
//void STBYStartAlarm(void)
//{
//    if (RunLamp == ON && StandByLamp == ON)
//    {
//        LampSigCont(STBY_START_ALARM, OFF); // NC
//        STBY_Start = ON;
//    }
//    else
//    {
//        LampSigCont(STBY_START_ALARM, ON);
//        STBY_Start = OFF;
//    }
//}
//
//void ConnectProc(unsigned char status)
//{
//    if (status == NoConnection) // NO CONNECT
//    {
//        LampSigCont(COM_FAULT_LAMP, ON);
//        ComFailLamp_Flag = 0;
//
//        CountStandByCheck_mS = 0;
//
//        StandBy_2_Flag = 0;
//    }
//    else if (status == StandBy_3to2 || status == StandBy_3to2_1RUN) // 3 STAND BY (COM FAIL OR POWER FAIL) -> 2 STAND BY : Blinking COM.FAIL LAMP
//    {
//        if (CountComFailLamp_mS < 5)                                   LampSigCont(COM_FAULT_LAMP, ON);
//        else if (5 <= CountComFailLamp_mS && CountComFailLamp_mS < 10) LampSigCont(COM_FAULT_LAMP, OFF);
//        else if (CountComFailLamp_mS >= 10)                            CountComFailLamp_mS = 0;
//
//        ComStatus_Flag = 1; // Memory Status
//
//        CountStandByCheck_mS = 0;
//
//        // Push the Reset Button -> Change Status (3 to 2 -> 2 STAND BY)
//        if (STOP_PB_I == 0 && CountResetButton_S >= 2)
//        {
//            ComFailLamp_Flag = 1;
//            ComStatus = StandBy_2;
//        }
//    }
//    else if (status == StandBy_2) // 2 STAND BY
//    {
//        LampSigCont(COM_FAULT_LAMP, OFF);
//        ComFailLamp_Flag = 0;
//        ComStatus_Flag = 0;
//
//        CountStandByCheck_mS = 0;
//    }
//    else if (status == StandBy_3) // 3 STAND BY(2 RUN)
//    {
//        LampSigCont(COM_FAULT_LAMP, OFF);
//        ComFailLamp_Flag = 0;
//        ComStatus_Flag = 0;
//
//        CountStandByCheck_mS = 0;
//
//        StandBy_2_Flag = 0;
//    }
//    else if (status == Manual) // MANUAL MODE -> NO CONNECT
//    {
//        LampSigCont(COM_FAULT_LAMP, OFF);
//        ComFailLamp_Flag = 0;
//
//        CountStandByCheck_mS = 0;
//
//        StandBy_2_Flag = 0;
//    }
//    else if (status == StandBy_3_1RUN) // 3 STAND BY(1 RUN)
//    {
//        LampSigCont(COM_FAULT_LAMP, OFF);
//        ComFailLamp_Flag = 0;
//        ComStatus_Flag = 1; // Memory Status
//
//        StandBy_2_Flag = 0;
//    }
//
//    if (ComFailLamp_Flag == 1)       LampSigCont(COM_FAULT_LAMP, OFF);
//}
//
//void ConnectFunction(void)
//{
//    if (CAN_ID == 1)
//    {
//        // MANUAL MODE -> NO CONNECT
//        if (ModeStatus == MANUAL_MODE)               ComStatus = Manual;
//
//        // NO CONNECT
//        else if (Error_Flag2 == Com_Error && Error_Flag3 == Com_Error)
//            ComStatus = NoConnection;
//
//        // 3 STAND BY
//        else if (Error_Flag2 == Com_Normal && Error_Flag3 == Com_Normal)
//        {
//            StandBy_3_1RUN_Flag = 0;
//
//            // 2 RUN or 1 RUN
//            if (RunLamp == ON && rx_data2[1] == RUN && rx_data3[1] == STOP)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data2[1] == STOP && rx_data3[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == OFF && rx_data2[1] == RUN && rx_data3[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data2[1] == STOP && rx_data3[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data2[1] == RUN && rx_data3[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data2[1] == STOP && rx_data3[1] == RUN)
//                ComStatus = StandBy_3_1RUN;
//        }
//
//        // 2 STAND BY
//        else if ((Error_Flag2 == Com_Normal && Error_Flag3 == Com_Error) || (Error_Flag2 == Com_Error && Error_Flag3 == Com_Normal))
//        {
//            if (ComStatus == StandBy_3)              ComStatus = StandBy_3to2;
//            else if (ComStatus == StandBy_2)         ComStatus = StandBy_2;
//            else if (ComStatus == NoConnection || ComStatus == Manual)
//            {
//                if (ComStatus_Flag == 1)             ComStatus = StandBy_3to2;
//                else if (ComStatus_Flag == 0)        ComStatus = StandBy_2;
//            }
//            else if (ComStatus == StandBy_3_1RUN)
//            {
//                ComStatus = StandBy_3to2_1RUN;
//                StandBy_3_1RUN_Flag = 1;
//            }
//        }
//    }
//    else if (CAN_ID == 2)
//    {
//        // MANUAL MODE -> NO CONNECT
//        if (ModeStatus == MANUAL_MODE)               ComStatus = Manual;
//
//        // NO CONNECT
//        else if (Error_Flag1 == Com_Error && Error_Flag3 == Com_Error)
//            ComStatus = NoConnection;
//
//        // 3 STAND BY
//        else if (Error_Flag1 == Com_Normal && Error_Flag3 == Com_Normal)
//        {
//            StandBy_3_1RUN_Flag = 0;
//
//            // 2 RUN or 1 RUN
//            if (RunLamp == ON && rx_data1[1] == RUN && rx_data3[1] == STOP)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data1[1] == STOP && rx_data3[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == OFF && rx_data1[1] == RUN && rx_data3[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data1[1] == STOP && rx_data3[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data1[1] == RUN && rx_data3[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data1[1] == STOP && rx_data3[1] == RUN)
//                ComStatus = StandBy_3_1RUN;
//        }
//
//        // 2 STAND BY
//        else if ((Error_Flag1 == Com_Normal && Error_Flag3 == Com_Error) || (Error_Flag1 == Com_Error && Error_Flag3 == Com_Normal))
//        {
//            if (ComStatus == StandBy_3)              ComStatus = StandBy_3to2;
//            else if (ComStatus == StandBy_2)         ComStatus = StandBy_2;
//            else if (ComStatus == NoConnection || ComStatus == Manual)
//            {
//                if (ComStatus_Flag == 1)             ComStatus = StandBy_3to2;
//                else if (ComStatus_Flag == 0)        ComStatus = StandBy_2;
//            }
//            else if (ComStatus == StandBy_3_1RUN)
//            {
//                ComStatus = StandBy_3to2_1RUN;
//                StandBy_3_1RUN_Flag = 1;
//            }
//        }
//    }
//    else if (CAN_ID == 3)
//    {
//        // MANUAL MODE -> NO CONNECT
//        if (ModeStatus == MANUAL_MODE)               ComStatus = Manual;
//
//        // NO CONNECT
//        else if (Error_Flag1 == Com_Error && Error_Flag2 == Com_Error)
//            ComStatus = NoConnection;
//
//        // 3 STAND BY
//        else if (Error_Flag1 == Com_Normal && Error_Flag2 == Com_Normal)
//        {
//            StandBy_3_1RUN_Flag = 0;
//
//            // 2 RUN or 1 RUN       
//            if (RunLamp == ON && rx_data1[1] == RUN && rx_data2[1] == STOP)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data1[1] == STOP && rx_data2[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == OFF && rx_data1[1] == RUN && rx_data2[1] == RUN)
//                ComStatus = StandBy_3;
//            else if (RunLamp == ON && rx_data1[1] == STOP && rx_data2[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data1[1] == RUN && rx_data2[1] == STOP)
//                ComStatus = StandBy_3_1RUN;
//            else if (RunLamp == OFF && rx_data1[1] == STOP && rx_data2[1] == RUN)
//                ComStatus = StandBy_3_1RUN;
//        }
//
//        // 2 STAND BY
//        else if ((Error_Flag1 == Com_Normal && Error_Flag2 == Com_Error) || (Error_Flag1 == Com_Error && Error_Flag2 == Com_Normal))
//        {
//            if (ComStatus == StandBy_3)              ComStatus = StandBy_3to2;
//            else if (ComStatus == StandBy_2)         ComStatus = StandBy_2;
//            else if (ComStatus == NoConnection || ComStatus == Manual)
//            {
//                if (ComStatus_Flag == 1)             ComStatus = StandBy_3to2;
//                else if (ComStatus_Flag == 0)        ComStatus = StandBy_2;
//            }
//            else if (ComStatus == StandBy_3_1RUN)
//            {
//                ComStatus = StandBy_3to2_1RUN;
//                StandBy_3_1RUN_Flag = 1;
//            }
//        }
//    }
//}
//
//void Chattering(unsigned char value3, unsigned char value2, unsigned char value1)
//{
//    if (value3 == 0 && value2 == 0 && value1 == 0)
//        LowPressChattTime = 70 + BT_Delayms;
//    else if (value3 == 0 && value2 == 0 && value1 == 1)
//        LowPressChattTime = 60 + BT_Delayms;
//    else if (value3 == 0 && value2 == 1 && value1 == 0)
//        LowPressChattTime = 50 + BT_Delayms;
//    else if (value3 == 0 && value2 == 1 && value1 == 1)
//        LowPressChattTime = 40 + BT_Delayms;
//    else if (value3 == 1 && value2 == 0 && value1 == 0)
//        LowPressChattTime = 30 + BT_Delayms;
//    else if (value3 == 1 && value2 == 0 && value1 == 1)
//        LowPressChattTime = 20 + BT_Delayms;
//    else if (value3 == 1 && value2 == 1 && value1 == 0)
//        LowPressChattTime = 10 + BT_Delayms;
//    else if (value3 == 1 && value2 == 1 && value1 == 1)
//        LowPressChattTime = BT_Delayms;
//}