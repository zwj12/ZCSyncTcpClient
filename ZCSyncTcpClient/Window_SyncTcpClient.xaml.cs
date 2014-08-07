using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace ZCSyncTcpClient
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window_SyncTcpClient : Window
    {
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private BinaryReader br;
        private BinaryWriter bw;
        private int sendCount = 1;
        private int receiveCount = 10;

        //显示消息
        private delegate void ShwMsgforViewCallBack(string str);
        private ShwMsgforViewCallBack shwMsgforViewCallBack;
        //显示状态
        private delegate void ShwStatusInfoCallBack(string str);
        private ShwStatusInfoCallBack shwStatusInfoCallBack;
        //显示进度
        private delegate void ShwProgressProcCallBack(int progress);
        private ShwProgressProcCallBack shwProgressProcCallBack;
        //重置消息文本
        private delegate void ResetMsgTxtCallBack();
        private ResetMsgTxtCallBack resetMsgTxtCallBack;
        
        public Window_SyncTcpClient()
        {
            InitializeComponent();
            shwMsgforViewCallBack = new ShwMsgforViewCallBack(ShwMsgforView);
            shwStatusInfoCallBack = new ShwStatusInfoCallBack(ShwStatusInfo);
            shwProgressProcCallBack = new ShwProgressProcCallBack(ShwProgressProc);
            resetMsgTxtCallBack = new ResetMsgTxtCallBack(ResetMsgTxt);


            textBox_SendCount.Text = sendCount.ToString();
            textBox_ReceiveCount.Text = receiveCount.ToString();
            progressBar_Proc.Minimum = 0;
        }

        private void ShwMsgforView(string str)
        {
            if (!Dispatcher.CheckAccess())
            {
                ShwMsgforViewCallBack update = new ShwMsgforViewCallBack(ShwMsgforView);
                Dispatcher.Invoke(update, new object[] { str });
            }
            else
            {
                listBox_MsgView.Items.Add(str);
                //listBox_MsgView.ScrollIntoView(listBox_MsgView.Items[listBox_MsgView.Items.Count - 1]);
            }

        }

        private void ShwStatusInfo(string str)
        {
            if (!Dispatcher.CheckAccess())
            {
                ShwStatusInfoCallBack update = new ShwStatusInfoCallBack(ShwStatusInfo);
                Dispatcher.Invoke(update, new object[] { str });
            }
            else
            {
                label_Info.Content = str;
            }

        }

        private void ShwProgressProc(int progress)
        {
            if (!Dispatcher.CheckAccess())
            {
                ShwProgressProcCallBack update = new ShwProgressProcCallBack(ShwProgressProc);
                Dispatcher.Invoke(update, new object[] { progress });
            }
            else
            {
                progressBar_Proc.Value = progress;
            }
        }

        private void ResetMsgTxt()
        {
            if (!Dispatcher.CheckAccess())
            {
                ResetMsgTxtCallBack update = new ResetMsgTxtCallBack(ResetMsgTxt);
                Dispatcher.Invoke(update, new object[] { });
            }
            else
            {
                textbox_Msg.Text = "";
                textbox_Msg.Focus();
            }

        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            progressBar_Proc.Maximum = 100;
            progressBar_Proc.Value = 0;
            Thread threadConnect = new Thread(ConnectoServer);
            threadConnect.IsBackground = true;
            threadConnect.Start();
        }

        private void ConnectoServer()
        {
            try
            {
                shwStatusInfoCallBack("正在连接...");
                IPHostEntry remoteHost = Dns.GetHostEntry(textBox_SrvIp.Text);
                tcpClient = new TcpClient();
                shwProgressProcCallBack(1);
                tcpClient.Connect(remoteHost.HostName,int.Parse(textBox_Port.Text));
                shwProgressProcCallBack(100);
                Thread.Sleep(1000);
                if(tcpClient!=null)
                {
                    shwStatusInfoCallBack("连接成功");
                    networkStream=tcpClient.GetStream();
                    br=new BinaryReader(networkStream);
                    bw=new BinaryWriter(networkStream);
                }
            }
            catch
            {
                shwStatusInfoCallBack("连接失败！");
                Thread.Sleep(1000);
                shwProgressProcCallBack(0);
                shwStatusInfoCallBack("就绪");
            }
        }

        private void button_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (br != null)
            {
                br.Close();
            }
            if (bw != null)
            {
                bw.Close();
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
            label_Info.Content = "连接断开";
            progressBar_Proc.Value = 0;

        }

        private void button_Receive_Click(object sender, RoutedEventArgs e)
        {
            receiveCount = int.Parse(textBox_ReceiveCount.Text);
            progressBar_Proc.Maximum = receiveCount;
            progressBar_Proc.Value = 0;
            Thread threadReceive = new Thread(ReceiveMessage);
            threadReceive.Start();
        }

        private void ReceiveMessage()
        {
            ShwStatusInfo("接收中...");
            for (int i = 0; i < receiveCount; i++)
            {
                try
                {
                    string rcvMsgStr = br.ReadString();
                    ShwProgressProc(i + 1);
                    if (rcvMsgStr != null)
                    {
                        ShwMsgforView(rcvMsgStr);
                    }
                }
                catch
                {
                    if (br != null)
                    {
                        br.Close();
                    }
                    if (bw != null)
                    {
                        bw.Close();
                    }
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                    }
                    ShwStatusInfo("连接断开");
                    ShwProgressProc(0);
                    break;
                }
            }
            ShwStatusInfo("接收了" + receiveCount + "条消息。");
        }

        private void button_Send_Click(object sender, RoutedEventArgs e)
        {
            sendCount = int.Parse(textBox_SendCount.Text);
            progressBar_Proc.Maximum = sendCount;
            progressBar_Proc.Value = 0;
            Thread threadSend = new Thread(new ParameterizedThreadStart(SendMessage));
            threadSend.IsBackground = true;
            threadSend.Start(textbox_Msg.Text);
        }

        private void SendMessage(object state)
        {
            ShwStatusInfo("正在发送...");
            for (int i = 0; i < sendCount; i++)
            {
                try
                {
                    bw.Write(state.ToString());
                    ShwProgressProc(i + 1);
                    Thread.Sleep(5000);
                    bw.Flush();
                }
                catch
                {
                    if (br != null)
                    {
                        br.Close();
                    }
                    if (bw != null)
                    {
                        bw.Close();
                    }
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                    }
                    ShwStatusInfo("连接断开");
                    ShwProgressProc(0);
                    break;
                }
            }
            ShwStatusInfo("完毕");
            Thread.Sleep(1000);
            ShwProgressProc(0);

            resetMsgTxtCallBack();
        }

        private void button_Clear_Click(object sender, RoutedEventArgs e)
        {
            listBox_MsgView.Items.Clear();
        }
    }
}
