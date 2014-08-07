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

namespace ZCSyncTcpServer
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window_SyncTcpServer : Window
    {
        private IPAddress localAddress;
        private const int port = 9002;
        private TcpListener tcpListener;
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

        public Window_SyncTcpServer()
        {
            InitializeComponent();
            shwMsgforViewCallBack=new ShwMsgforViewCallBack(ShwMsgforView);
            shwStatusInfoCallBack = new ShwStatusInfoCallBack(ShwStatusInfo);
            shwProgressProcCallBack = new ShwProgressProcCallBack(ShwProgressProc);
            resetMsgTxtCallBack = new ResetMsgTxtCallBack(ResetMsgTxt);

            localAddress = IPAddress.Any;
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
                textbox_Msg.Text  = "";
                textbox_Msg.Focus();
            }

        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            tcpListener = new TcpListener(localAddress, port);
            tcpListener.Start();
            progressBar_Proc.Maximum = 100;
            progressBar_Proc.Value = 0;
            Thread threadAccept = new Thread(AcceptClientConnect);
            threadAccept.IsBackground = true;
            threadAccept.Start();
        }

        private void AcceptClientConnect()
        {
            ShwStatusInfo("[" + localAddress + ":" + port + "]侦听...");
            //DateTime nowtime = DateTime.Now;
            //while (nowtime.AddSeconds(1) > DateTime.Now) { }
            Thread.Sleep(1000);
            try
            {
                ShwStatusInfo("等待连接...");
                ShwProgressProc(1);
                tcpClient = tcpListener.AcceptTcpClient();
                ShwProgressProc(100);
                if (tcpClient != null)
                {
                    ShwStatusInfo("接受了一个连接。");
                    networkStream = tcpClient.GetStream();
                    br = new BinaryReader(networkStream);
                    bw = new BinaryWriter(networkStream);
                }
            }
            catch
            {
                ShwStatusInfo("停止侦听。");
                Thread.Sleep(1000);
                ShwProgressProc(0);
                ShwStatusInfo("就绪");
            }
        }

        private void button_Receive_Click(object sender, RoutedEventArgs e)
        {
            receiveCount = int.Parse(textBox_ReceiveCount.Text);
            progressBar_Proc.Maximum = receiveCount;
            progressBar_Proc.Value = 0;
            Thread threadReceive = new Thread(ReceiveMessage);
            threadReceive.IsBackground = true;
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
                    Thread.Sleep(2000);
                    Thread threadAccept = new Thread(AcceptClientConnect);
                    threadAccept.IsBackground = true;
                    threadAccept.Start();
                    break;
                }
            }
            ShwStatusInfo("接收了"+receiveCount + "条消息。");
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
                    Thread.Sleep(2000);
                    Thread threadAccept = new Thread(AcceptClientConnect);
                    threadAccept.IsBackground = true;
                    threadAccept.Start();
                    break;
                }
            }
            ShwStatusInfo("完毕");
            Thread.Sleep(1000);
            ShwProgressProc(0);

            resetMsgTxtCallBack();
        }

        private void button_Stop_Click(object sender, RoutedEventArgs e)
        {
            tcpListener.Stop();
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
            Thread.Sleep(2000);
            Thread threadAccept = new Thread(AcceptClientConnect);
            threadAccept.IsBackground = true;
            threadAccept.Start();

        }

        private void button_Clear_Click(object sender, RoutedEventArgs e)
        {
            listBox_MsgView.Items.Clear();
            你
        }
    }
}
