using BytAiAlarmService.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BytAiAlarmService
{
    public partial class Service1 : ServiceBase
    {
        /// <summary>
        /// 开启一个线程处理
        /// </summary>
        Thread AiServiceThread = null;

        /// <summary>
        /// 海康录像机网络参数
        /// </summary>
        HikvisionNvr NVR = null;

        /// <summary>
        /// 设备登录后返回ID
        /// </summary>
        private static Int32 NVRLoginUserID;

        /// <summary>
        /// 布防返回ID
        /// </summary>
        private static Int32 AlarmHandleID;

        public Service1()
        {
            InitializeComponent();
            // SDK初始化
            if (!HikHelper.NetDVRInit())
            {
                Environment.Exit(0);
                return;
            }
            NVR = new HikvisionNvr()
            {
                DeviceIP = ConfigurationManager.AppSettings.Get("NVR_IP").ToString(),
                DevicePort = Convert.ToInt16(ConfigurationManager.AppSettings.Get("NVR_Port").ToString()),
                UserName = ConfigurationManager.AppSettings.Get("NVR_UserName").ToString(),
                Password = ConfigurationManager.AppSettings.Get("NVR_Password").ToString()
            };
            //设置SDK 日志
            HikHelper.NetDVRSetLogToFile();
            //设置透传报警信息类型
            HikHelper.NetDVRSetSDKLocalCfg();
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            if (NVR.DeviceIP == "" || NVR.DevicePort == default(Int32) || NVR.UserName == "" || NVR.Password == "")
            {
                WriteLog("Please input IP, Port, User name and Password!");
                return;
            }
            //设置报警回调函数
            HikHelper.SetAlarmCallBack();
            // 设备登录
            NVRLoginUserID = HikHelper.NetDVRLogin(NVR);
            // 设备布防
            AiServiceThread = new Thread(CollectArgManage)
            {
                IsBackground = true
            };
            AiServiceThread.Start();
        }


        /// <summary>
        /// 终止服务
        /// </summary>
        protected override void OnStop()
        {
            // 撤防
            if (AlarmHandleID != null)
            {
                var res = HikHelper.NetDVRCloseAlarmChan(AlarmHandleID);
            }

            //注销登录
            if (NVRLoginUserID != null)
            {
                HikHelper.NetDVRLogout(NVRLoginUserID);
            }
            //释放SDK资源，在程序结束之前调用
            HikHelper.NetDVRClearup();
            // 清空日志
            var fPath = AppDomain.CurrentDomain.BaseDirectory + "/log/";
            FileHelper.DeleteFolderFiles(fPath, "");
            //this.Stop();
        }

        private void CollectArgManage() 
        {
            AlarmHandleID = HikHelper.NetDVRSetupAlarm(NVRLoginUserID);
        }

        private void WriteLog(string msg) 
        {
            FileHelper.AppendText("ErrorLog.txt", msg + "\n");
        }
    }
}
