using BytAiAlarmService.Sdk;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BytAiAlarmService.Helper
{
    public class HikvisionNvr
    {
        /// <summary>
        /// 录像机IP
        /// </summary>
        public string DeviceIP { get; set; }
        public Int16 DevicePort { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class HikHelper
    {
        public static CHCNetSDK.NET_DVR_SETUPALARM_PARAM StruAlarmParam = new CHCNetSDK.NET_DVR_SETUPALARM_PARAM();

        public static CHCNetSDK.MSGCallBack_V31 m_falarmData_V31 = null;

        public static AlarmInfo AI_AlarmInfo = null;

        /// <summary>
        /// 日志最大存储大小，超过该大小则清空
        /// </summary>
        public static Int32 MaxStrogeFileSize = 1024 * 1024 * 2;

        /// <summary>
        /// 初始化SDK
        /// </summary>
        /// <returns></returns>
        public static bool NetDVRInit() 
        {
            bool m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (!m_bInitSDK)
            {
                WriteMsg("NET_DVR_Init error!");
            }
            return m_bInitSDK;
        }
        /// <summary>
        /// 保存SDK日志 To save the SDK log
        /// </summary>
        public static void NetDVRSetLogToFile()
        {
            var logPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Sdk_Log\\";
            CHCNetSDK.NET_DVR_SetLogToFile(3, logPath, true);
        }

        /// <summary>
        /// 设置透传报警信息类型
        /// </summary>
        public static void NetDVRSetSDKLocalCfg()
        {
            //设置透传报警信息类型
            CHCNetSDK.NET_DVR_LOCAL_GENERAL_CFG struLocalCfg = new CHCNetSDK.NET_DVR_LOCAL_GENERAL_CFG();
            struLocalCfg.byAlarmJsonPictureSeparate = 1;//控制JSON透传报警数据和图片是否分离，0-不分离(COMM_VCA_ALARM返回)，1-分离（分离后走COMM_ISAPI_ALARM回调返回）

            Int32 nSize = Marshal.SizeOf(struLocalCfg);
            IntPtr ptrLocalCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(struLocalCfg, ptrLocalCfg, false);
            if (!CHCNetSDK.NET_DVR_SetSDKLocalCfg(17, ptrLocalCfg))  //NET_DVR_LOCAL_CFG_TYPE_GENERAL
            {
                var iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                var strErr = "NET_DVR_SetSDKLocalCfg failed, error code= " + iLastErr;
                WriteMsg(strErr);
            }
            Marshal.FreeHGlobal(ptrLocalCfg);
        }

        public static Int32 NetDVRLogin(HikvisionNvr NVR)
        {
            CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();
            //登录设备 Login the device
            Int32 m_lUserID = CHCNetSDK.NET_DVR_Login_V30(NVR.DeviceIP, NVR.DevicePort, NVR.UserName, NVR.Password, ref DeviceInfo);
            if (m_lUserID < 0)
            {
                var iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                var strErr = "NET_DVR_Login_V30 failed, error code= " + iLastErr; //登录失败，输出错误号 Failed to login and output the error code
                WriteMsg(strErr);
            }
            else
            {
                WriteMsg(string.Format("设备:{0};用户{1}登录成功", NVR.DeviceIP, m_lUserID), "ok");
            }
            return m_lUserID;
        }

        /// <summary>
        /// 布防
        /// </summary>
        /// <param name="UserID">登录ID</param>
        /// <returns></returns>
        public static Int32 NetDVRSetupAlarm(Int32 UserID)
        {
            //CHCNetSDK.NET_DVR_SETUPALARM_PARAM struAlarmParam = new CHCNetSDK.NET_DVR_SETUPALARM_PARAM();
            StruAlarmParam.dwSize = (uint)Marshal.SizeOf(StruAlarmParam);
            StruAlarmParam.byLevel = 1; //0- 一级布防,1- 二级布防
            StruAlarmParam.byAlarmInfoType = 1;//智能交通设备有效，新报警信息类型
            StruAlarmParam.byFaceAlarmDetection = 1;//1-人脸侦测
            Int32 result = -1;
            try
            {
                result = CHCNetSDK.NET_DVR_SetupAlarmChan_V41(UserID, ref StruAlarmParam);
                if (result < 0)
                {
                    var iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    var strErr = "布防失败，错误号：" + iLastErr; //布防失败，输出错误号
                    WriteMsg(strErr);
                }
                else
                {
                    WriteMsg("布防成功", "ok");
                }
            }
            catch (IOException ex)
            {
                WriteMsg("布防操作失败:" + ex.Message.ToString());
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// 撤防
        /// </summary>
        /// <param name="AlarmHandleID">布防操作ID</param>
        /// <returns></returns>
        public static bool NetDVRCloseAlarmChan(Int32 AlarmHandleID)
        {
            bool res = CHCNetSDK.NET_DVR_CloseAlarmChan_V30(AlarmHandleID);
            if (!res)
            {
                var iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                var strErr = "撤防失败，错误号：" + iLastErr; //撤防失败，输出错误号
                WriteMsg(strErr);
            }
            else
            {
                var strErr = "当前设备撤防成功,时间:" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒");
                WriteMsg(strErr, "ok");
            }
            return res;
        }

        /// <summary>
        /// 注销登录
        /// </summary>
        /// <param name="NVRLoginUserID"></param>
        public static void NetDVRLogout(Int32 NVRLoginUserID)
        {
            CHCNetSDK.NET_DVR_Logout(NVRLoginUserID);
            WriteMsg("当前设备已注销登录,时间" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"), "ok");
        }

        /// <summary>
        /// 释放SDK资源，在程序结束之前调用
        /// </summary>
        public static void NetDVRClearup()
        {
            CHCNetSDK.NET_DVR_Cleanup();
            WriteMsg("当前设备已释放SDK资源,时间" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"), "ok");
        }

        /// <summary>
        /// 设置报警回调
        /// </summary>
        public static void SetAlarmCallBack()
        {
            if (m_falarmData_V31 == null)
            {
                m_falarmData_V31 = new CHCNetSDK.MSGCallBack_V31(MsgCallback_V31);
            }
            CHCNetSDK.NET_DVR_SetDVRMessageCallBack_V31(m_falarmData_V31, IntPtr.Zero);
            WriteMsg("当前设备设置报警回调,时间" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"), "ok");
        }

        public static bool MsgCallback_V31(int lCommand, ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            if (AI_AlarmInfo == null)
            {
                AI_AlarmInfo = new AlarmInfo();
            }
            //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
            AI_AlarmInfo.AlarmMessageHandle(lCommand, ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
            return true; //回调函数需要有返回，表示正常接收到数据
        }

        public class AlarmInfo
        {
            private int iFileNumber = 0; //保存的文件个数
            private bool isInit = true;
            private uint iLastErr = 0;
            private string strErr;
            private string tmpFilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase; // 文件生成目录
            public static int num = 0;
            /// <summary>
            /// 时间段--开始时间
            /// </summary>
            private DateTime DtStar = new DateTime();
            /// <summary>
            /// 时间段--结束时间
            /// </summary>
            private DateTime DtEnd = new DateTime();

            /// <summary>
            /// ai农事上报执行时间
            /// </summary>
            private DateTime DtExec = new DateTime();

            /// <summary>
            /// AI农事上报间隔时间
            /// </summary>
            Int32 AlarmIntervalTime = 20;
            private static DateTime dtExec = new DateTime();// ai农事采集执行时间

            public AlarmInfo()
            {
                DtStar = DateTime.Now.Date.AddDays(0).AddHours(7);//今天7点。
                DtEnd = DateTime.Now.Date.AddDays(0).AddHours(18);//今天18点。
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get("Alarm_intervalTime").ToString()))
                {
                    AlarmIntervalTime = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Alarm_intervalTime").ToString());
                }
            }
            public void AlarmMessageHandle(int lCommand, ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
            {
                //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
                switch (lCommand)
                {
                    case CHCNetSDK.COMM_UPLOAD_AIOP_VIDEO:// AI开放平台接入视频检测报警信息
                        ProcessCommAIOP_VIDEO(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                        break;
                    case CHCNetSDK.COMM_UPLOAD_AIOP_PICTURE:// AI开放平台接入图片检测报警信息
                        ProcessCommAIOP_VIDEO(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                        break;
                    case CHCNetSDK.COMM_UPLOAD_AIOP_POLLING_SNAP:// AI开放平台接入轮询抓图检测报警信息
                        ProcessCommAIOP_VIDEO(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                        break;
                    case CHCNetSDK.COMM_UPLOAD_AIOP_POLLING_VIDEO:// AI开放平台接入轮询抓图检测报警信息
                        ProcessCommAIOP_VIDEO(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                        break;
                    default:
                        {
                            //报警设备IP地址
                            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

                            //报警信息类型
                            string stringAlarm = "报警上传，信息类型：" + lCommand + ";报警时间：" + DateTime.Now.ToString() + ";设备IP：" + strIP;
                            WriteMsg(stringAlarm, "ok");
                        }
                        break;
                }
            }

            /// <summary>
            /// AI识别报警布放
            /// </summary>
            /// <param name="pAlarmer"></param>
            /// <param name="pAlarmInfo"></param>
            /// <param name="dwBufLen"></param>
            /// <param name="pUser"></param>
            private void ProcessCommAIOP_VIDEO(ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
            {
                CHCNetSDK.NET_AIOP_VIDEO_HEAD struAiopVideoHead = new CHCNetSDK.NET_AIOP_VIDEO_HEAD();
                object temp = Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_AIOP_VIDEO_HEAD));
                struAiopVideoHead = (CHCNetSDK.NET_AIOP_VIDEO_HEAD)temp;
                // 报警信息
                string stringAI = "";
                uint dwSize = (uint)Marshal.SizeOf(struAiopVideoHead);
                //报警设备IP地址
                string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0').TrimEnd('\0');
                //报警时间：年月日时分秒
                string strTimeYear = struAiopVideoHead.struTime.wYear.ToString();
                string strTimeMonth = struAiopVideoHead.struTime.wMonth.ToString("d2");
                string strTimeDay = struAiopVideoHead.struTime.wDay.ToString("d2");
                string strTimeHour = struAiopVideoHead.struTime.wHour.ToString("d2");
                string strTimeMinute = struAiopVideoHead.struTime.wMinute.ToString("d2");
                string strTimeSecond = struAiopVideoHead.struTime.wSecond.ToString("d2");
                string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;
                string szTaskID = System.Text.Encoding.UTF8.GetString(struAiopVideoHead.szTaskID).TrimEnd('\0');
                string szMPID = System.Text.Encoding.UTF8.GetString(struAiopVideoHead.szMPID).TrimEnd('\0');
                //var picSize = Marshal.PtrToStringAnsi(struAiopVideoHead.pBufferPictureSize);
                stringAI = "AI识别，报警触发时间：" + strTime + ",szTaskID:" + szTaskID + ",szMPID:" + szMPID;
                string fPath = tmpFilePath + "picture\\";
                string filename = fPath + "UserID_" + pAlarmer.lUserID + "_AI开放平台检测报警_" + iFileNumber + ".jpg";
                //报警图片保存-对应分析图片数据 
                if (struAiopVideoHead.dwPictureSize > 0)
                {
                    MkDir(fPath);
                    FileStream fs = new FileStream(filename, FileMode.Create);
                    int iLen = (int)struAiopVideoHead.dwPictureSize;
                    byte[] by = new byte[iLen];
                    Marshal.Copy(struAiopVideoHead.pBufferPictureSize, by, 0, iLen);
                    fs.Write(by, 0, iLen);
                    fs.Close();
                    iFileNumber++;
                    // 超过10个文件就清除文件夹
                }
                //AIOPData数据 
                if (struAiopVideoHead.dwAIOPDataSize > 0)
                {
                    string AIOPData = Marshal.PtrToStringAnsi(struAiopVideoHead.pBufferAIOPData).TrimEnd('\0').TrimEnd('\0');
                    var tIndex = AIOPData.LastIndexOf("??");
                    string jsonStr = AIOPData.Substring(0, tIndex);
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        //var aiopJson = JsonHelper.DeserializeJsonToObject<object>(jsonStr);
                        //trainModelId: 8274
                        var aiopJson = Newtonsoft.Json.Linq.JObject.Parse(jsonStr);
                        string url = ConfigurationManager.AppSettings.Get("API_HIK_ALARM").ToString();
                        var obj = new
                        {
                            url = url,
                            data = jsonStr
                        };
                        var postData = new Dictionary<string, string>() {
                    {"time",strTime },
                    {"szTaskID",szTaskID },
                    {"szMPID",szMPID },
                    {"aiopJson",jsonStr },
                    };
                        var now = DateTime.Now;
                        if (isInit)
                        {
                            isInit = false;
                            AddAgManage(url, filename, postData);
                        }
                        else
                        {

                            TimeSpan ts1 = new TimeSpan(now.Ticks);
                            TimeSpan ts2 = new TimeSpan(dtExec.Ticks);
                            TimeSpan ts = ts1.Subtract(ts2).Duration();
                            var dateDiff = ts.Minutes;
                            //如果当前时间与采集时间的差值超过30分钟就采集
                            if (dateDiff >= AlarmIntervalTime)
                            {
                                AddAgManage(url, filename, postData);
                            }
                        }
                        //if (now.Hour >= DtStar.Hour && now.Hour <= DtEnd.Hour && now.Date == DtStar.Date)//开始时间和结束时间之间，并且是今天
                        //{
                        //    if (isInit)
                        //    {
                        //        isInit = false;
                        //        AddAgManage(url, filename, postData);
                        //    }
                        //    else
                        //    {

                        //        TimeSpan ts1 = new TimeSpan(now.Ticks);
                        //        TimeSpan ts2 = new TimeSpan(dtExec.Ticks);
                        //        TimeSpan ts = ts1.Subtract(ts2).Duration();
                        //        var dateDiff = ts.Minutes;
                        //        //如果当前时间与采集时间的差值超过30分钟就采集
                        //        if (dateDiff >= AlarmIntervalTime)
                        //        {
                        //            AddAgManage(url, filename, postData);
                        //        }
                        //    }
                        //}
                        if (iFileNumber > 10)
                        {
                            FileHelper.DeleteFolderFiles(fPath, "");
                            iFileNumber = 0;
                            //listViewAlarmInfo.Items.Clear();
                        }
                    }
                }
                WriteMsg(stringAI, "ok");
            }

            public void MkDir(string absolutePath)
            {
                if (!System.IO.Directory.Exists(absolutePath))
                    System.IO.Directory.CreateDirectory(absolutePath);
            }

            private void AddAgManage(string url, string filename, Dictionary<string, string> postData)
            {
                var res = HttpHelper.HttpPost(url, postData, new List<string>() {
                        filename
                    });
                // 更新当前的采集执行时间
                dtExec = DateTime.Now;
                FileHelper.AppendText("AddRemoteNs.txt", res);
                FileHelper.AppendText("AddRemoteNsAndNum.txt", JsonHelper.SerializeObject(new
                {
                    time = DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒"),
                    num = num
                }));
            }
            public void print(DateTime dt)
            {
                num++;
                Console.WriteLine("执行第{0}次，时间：{1}", num, dt);
            }
        }

        public static void WriteMsg(string msg, string type = "error")
        {
            if ("error" == type.ToLower())
            {
                FileHelper.AppendText("Hik_Error_Log.txt", msg + "\n", MaxStrogeFileSize);
            }
            else
            {
                FileHelper.AppendText("Hik_OK_Log.txt", msg + "\n", MaxStrogeFileSize);
            }
        }
    }
}
