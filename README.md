# HikvisionAiAlarm
海康威视Win SDK 解析 报警布防-AI开放平台报警信息



## 功能

通过报警布防采集AI录像机超脑 智能事件的报警信息并做成winService方式

## 环境

64位windows操作系统、海康window64 SDK6.1.4.42

## 使用说明

1. 由于代码里存在许多业务代码，大家只需要核心的解析代码就可以，这里我把代码贴上来

    Sdk\CHCNetSDK.cs

   ```c#
   [StructLayoutAttribute(LayoutKind.Sequential)]
   /// <summary>
   /// AI开放平台接入视频检测报警信息
   /// </summary>
   public struct NET_AIOP_VIDEO_HEAD 
   {
       public uint dwSize;// 结构体大小 
       public uint dwChannel;// 通道号
       public NET_DVR_SYSTEM_TIME struTime;//时间参数
       [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.I1)]
       public byte[] szTaskID;   //视频任务ID，来自于视频任务派发    
       public uint dwAIOPDataSize; //AIOPData的数据长度 
       public uint dwPictureSize; //对应分析图片长度 
       [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.I1)]
       public byte[] szMPID;   //检测模型包ID，用于匹配AIOP的检测数据解析  
       public IntPtr pBufferAIOPData;// AIOPData数据
       public IntPtr pBufferPictureSize;// 对应分析图片数据 
       public byte byPictureMode;
       [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
       public byte[] byRes2;   //保留
       public uint dwPresetIndex;
       [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 176, ArraySubType = UnmanagedType.I1)]
       public byte[] byRes;   //保留
   }
   ```

   ```c#
   // 系统时间信息结构体
   [StructLayoutAttribute(LayoutKind.Sequential)]
   public struct NET_DVR_SYSTEM_TIME 
   {
       public ushort wYear;
       public ushort wMonth;
       public ushort wDay;
       public ushort wHour;
       public ushort wMinute;
       public ushort wSecond;
       public ushort wMilliSec;
       [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
       public byte[] byRes;
   }
   ```

   部分调用代码(Helper\HikHelper.cs)

   ```c#
   CHCNetSDK.NET_AIOP_VIDEO_HEAD struAiopVideoHead = new CHCNetSDK.NET_AIOP_VIDEO_HEAD();
   object temp = Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_AIOP_VIDEO_HEAD));
   struAiopVideoHead = (CHCNetSDK.NET_AIOP_VIDEO_HEAD)temp;
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
   //AIOPData数据，我解析出来后发现会多些乱码字符，一直没找到好的法子便使用了字符串截取
   if (struAiopVideoHead.dwAIOPDataSize > 0){
       string AIOPData = Marshal.PtrToStringAnsi(struAiopVideoHead.pBufferAIOPData).TrimEnd('\0').TrimEnd('\0');
       var tIndex = AIOPData.LastIndexOf("??");
       string jsonStr = AIOPData.Substring(0, tIndex);
   }
   ```

   

2. 报警布防流程请参考官方手册以及官方示例代码

3. 如果想实现报警主机监听，请先在设备上设置报警主机，在参考官方手册以及官方示例代码，最后加入上面说的核心解析代码

4. 创建或修改配置文件App.config（选择framework依赖时应该会生成配置文件）

    ```c#
    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
      <appSettings>
        <!--API api接口地址是我个人业务使用，无须关心-->
        <add key="API_HIK_ALARM" value="####"/>
        <!--设备信息-->
        <add key="NVR_IP" value="NVR IP"/>
        <add key="NVR_Port" value="NVR 服务端口"/>
        <add key="NVR_UserName" value="NVR 登录用户名"/>
        <add key="NVR_Password" value="NVR 登录密码"/>
        <!--报警主机服务器-->
        <add key="Alarm_Host" value="0.0.0.0"/>
        <add key="Alarm_Host" value="9578"/>
        <!--报警布放间隔上报物联网平台时间（分钟）-->
        <add key="Alarm_intervalTime" value="20"/>
      </appSettings>
        <startup> 
            <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
        </startup>
    </configuration>
    ```

    



## 特别说明

1. vs生成请把生成目录改成"bin",如果不改成bin那就修改CHCNetSDK中dll文件的引入。
2. <font color=red>请将HikDLL.zip解压到bin目录下，这里是海康sdk</font>
3. bin文件夹下为已编译的可执行程序（Release版本），SDK日志保存路径："项目\\bin\\SdkLog\\"，业务日志路径:"项目\\bin\\Log\\"
4. 安装windows服务
   1. "C:\Windows\Microsoft.NET\"找到.net framework文件夹，32位选择framework 64位选择framework64,例如我是"C:\Windows\Microsoft.NET\Framework64\v4.0.30319",如果没有请先安装.net framework
   2. 使用InstallUtil.exe 安装服务，然后找到项目\bin\BytAiAlarmService.exe
   3. 操作命令：C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe F:\c#\BytAiAlarmService\BytAiAlarmService\bin\BytAiAlarmService.exe
   4. 需要使用以管理员身份运行命令行
   5. 参考链接：[![c#创建windowsService]](https://www.cnblogs.com/moretry/p/4149489.html)
5. 卸载windows服务
   1. 基本同安装服务
   2. 操作命令：C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u F:\c#\BytAiAlarmService\BytAiAlarmService\bin\BytAiAlarmService.exe
6. 开启服务
   1. 命令行输入：services.msc
   2. 找到：服务名为"HikAIAlarmService"的服务，描述为：“海康ai报警布防服务”，启动它并修改为自动启动