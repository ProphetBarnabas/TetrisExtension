using GameBoard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Object
{
    public class Object
    {
        public Action RotationMethod { get; }

        public int StartLocation { get; }

        public ObjectPart[] ObjectParts { get; private set; }

        public int ObjectPartCount { get; }

        public Object(int startLocation, ObjectPart[] objParts, Action rotationMethod = null)
        {
            RotationMethod = rotationMethod;
            StartLocation = startLocation;
            ObjectPartCount = objParts.Length;
        }
    }

    public class ObjectPart
    {
        public bool RotationPoint { get; }

        public int XPos { get; private set; }

        public int YPos { get; private set; }

        public ObjectPart()
        {

        }

        public ObjectPart(int xpos, int ypos)
        {
            XPos = xpos;
            YPos = ypos;
        }

        public void SetPosition(int xpos = 0, int ypos = 0)
        {
            XPos = xpos;
            YPos = ypos;
        }
    }
}

namespace GameBoard
{
    public class IntegerBoard
    {
        public int XSize { get; }

        public int YSize { get; }

        public int[,] CurrentBoard { get; private set; }

        public IntegerBoard(int x_size, int y_size)
        {
            XSize = x_size;
            YSize = y_size;
            CurrentBoard = new int[XSize, YSize];
        }

        public void SetBoardValue(int x_index, int y_index, int newValue)
        {
            CurrentBoard[x_index, y_index] = newValue;
        }

        public int[,] GetCurrentBoard()
        {
            return CurrentBoard;
        }

        public void ClearFullRows()
        {
            int[,] newBoard = new int[XSize, YSize];
            List<int> rowsToMove = new List<int>();
            bool rowIsFull = true;
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    if (CurrentBoard[i, j] == 0)
                    {
                        rowIsFull = false;
                        rowsToMove.Add(i);
                        break;
                    }
                }

                if (rowIsFull)
                {
                    for (int j = 0; j < YSize; j++)
                    {
                        CurrentBoard[i, j] = 0;
                    }
                }
            }

            rowsToMove.Reverse();
            int count = 0;
            do
            {
                for (int i = 0; i < YSize; i++)
                {
                    newBoard[(XSize - 1) - count, i] = CurrentBoard[rowsToMove[count], i];
                }
                count++;
            } while (count != rowsToMove.Count);
            CurrentBoard = newBoard;
        }

        public void ClearBoard()
        {
            CurrentBoard = new int[XSize, YSize];
        }
    }

    public class Block
    {
        public int Value { get; set; }

        public bool ContainsObjectPart { get; set; }

        public int XPos { get; set; }

        public int YPos { get; set; }

        public Block(int value = new int(), bool containsObjectPart = false)
        {
            Value = value;
            ContainsObjectPart = containsObjectPart;
        }
    }

    public class BlockBoard
    {
        public int XSize { get; }

        public int YSize { get; }

        public Block[,] CurrentBoard { get; private set; }

        public BlockBoard(int x_size, int y_size)
        {
            XSize = x_size;
            YSize = y_size;
            CurrentBoard = new Block[XSize, YSize];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    CurrentBoard[i, j].XPos = i;
                    CurrentBoard[i, j].YPos = j;
                }
            }
        }

        public void SetBlockValue(int x_index, int y_index, int newValue, bool containsObjectPart)
        {
            CurrentBoard[x_index, y_index].Value = newValue;
            CurrentBoard[x_index, y_index].ContainsObjectPart = containsObjectPart;
        }

        public Block[,] GetCurrentBoard()
        {
            return CurrentBoard;
        }

        public void ClearFullRows()
        {
            Block[,] newBoard = new Block[XSize, YSize];
            List<int> rowsToMove = new List<int>();
            bool rowIsFull = true;
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    if (CurrentBoard[i, j].Value == 0)
                    {
                        rowIsFull = false;
                        rowsToMove.Add(i);
                        break;
                    }
                }

                if (rowIsFull)
                {
                    for (int j = 0; j < YSize; j++)
                    {
                        CurrentBoard[i, j].Value = 0;
                    }
                }
            }

            rowsToMove.Reverse();
            int count = 0;
            do
            {
                for (int i = 0; i < YSize; i++)
                {
                    newBoard[(XSize - 1) - count, i] = CurrentBoard[rowsToMove[count], i];
                }
                count++;
            } while (count != rowsToMove.Count);
            CurrentBoard = newBoard;
        }

        public void ClearBoard()
        {
            CurrentBoard = new Block[XSize, YSize];
        }
    }
}

namespace TickSystem
{
    public class Tick
    {
        public int TickRate { get; private set; }

        public int TickCount { get; private set; }

        private CancellationToken CT;

        private CancellationTokenSource CTS = new CancellationTokenSource();

        public Tick(int tickRate)
        {
            TickRate = tickRate;
        }

        public async void StartTick()
        {
            while (!CT.IsCancellationRequested)
            {
                TickCount++;
                TickEventArgs = new TickEventArgs();
                TickEventArgs.TickCount = TickCount;
                TickEvent(this, TickEventArgs);
                await Task.Delay(TickRate);
            }
        }

        public void SetTickRate(int newTickRate)
        {
            TickRate = newTickRate;
        }

        public void CancelTick()
        {
            CT = CTS.Token;
        }

        public event TickEvent TickEvent;

        public TickEventArgs TickEventArgs;
    }

    public class TickEventArgs : EventArgs
    {
        public int TickCount;
    }

    public delegate void TickEvent(object sender, TickEventArgs e);
}

namespace Networking
{
    public class Server
    {
        public Server(string ipAddress, int port)
        {
            SERVER_IP = IPAddress.Parse(ipAddress);
            PORT_NO = port;
        }

        private IPAddress SERVER_IP;

        private int PORT_NO;

        private TcpListener TCP_LISTENER;

        private TcpClient TCP_CLIENT;

        private byte[] SEND_BUFFER, RECEIVE_BUFFER;

        private NetworkStream NET_STREAM;

        public event FrameReceivedEvent FrameReceived;

        public FrameReceivedEventArgs EventArgs;

        public void Start()
        {
            TCP_LISTENER = new TcpListener(SERVER_IP, PORT_NO);
            TCP_LISTENER.Start();
            TCP_LISTENER.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            TCP_CLIENT = TCP_LISTENER.EndAcceptTcpClient(ar);
            RECEIVE_BUFFER = new byte[TCP_CLIENT.ReceiveBufferSize];
            NET_STREAM = TCP_CLIENT.GetStream();
            NET_STREAM.BeginRead(RECEIVE_BUFFER, 0, RECEIVE_BUFFER.Length, new AsyncCallback(ReadCallback), null);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            NET_STREAM.EndRead(ar);
            EventArgs = new FrameReceivedEventArgs();
            EventArgs.Frame = RECEIVE_BUFFER;
            FrameReceived(this, EventArgs);
            NET_STREAM.BeginRead(RECEIVE_BUFFER, 0, RECEIVE_BUFFER.Length, new AsyncCallback(ReadCallback), null);
        }

        public void Send(string text)
        {
            SEND_BUFFER = Encoding.UTF8.GetBytes(text);
            NET_STREAM.BeginWrite(SEND_BUFFER, 0, SEND_BUFFER.Length, new AsyncCallback(SendCallback), null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            NET_STREAM.EndWrite(ar);
        }
    }

    public class Client
    {
        private Action ReceiveAction;

        private TcpClient TCP_CLIENT;

        private string SERVER_IP;

        private int PORT_NO;

        private NetworkStream NET_STREAM;

        private byte[] SEND_BUFFER, RECEIVE_BUFFER;

        public event FrameReceivedEvent FrameReceived;

        public FrameReceivedEventArgs EventArgs;

        public Client(string ipAddress, int port, Action receiveAction)
        {
            ReceiveAction = receiveAction;
            SERVER_IP = ipAddress;
            PORT_NO = port;
        }

        public void Connect()
        {
            TCP_CLIENT = new TcpClient(SERVER_IP, PORT_NO);
            NET_STREAM = TCP_CLIENT.GetStream();
            RECEIVE_BUFFER = new byte[TCP_CLIENT.ReceiveBufferSize];
            NET_STREAM.BeginRead(RECEIVE_BUFFER, 0, RECEIVE_BUFFER.Length, new AsyncCallback(ReceiveCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            NET_STREAM.EndRead(ar);
            EventArgs = new FrameReceivedEventArgs();
            EventArgs.Frame = RECEIVE_BUFFER;
            FrameReceived(this, EventArgs);
            NET_STREAM.BeginRead(RECEIVE_BUFFER, 0, RECEIVE_BUFFER.Length, new AsyncCallback(ReceiveCallback), null);
        }

        public void Send(string text)
        {
            SEND_BUFFER = Encoding.UTF8.GetBytes(text);
            NET_STREAM.BeginWrite(SEND_BUFFER, 0, SEND_BUFFER.Length, new AsyncCallback(SendCallback), null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            NET_STREAM.EndWrite(ar);
        }
    }

    public class FrameReceivedEventArgs : EventArgs
    {
        public byte[] Frame;
    }

    public delegate void FrameReceivedEvent(object sender, FrameReceivedEventArgs e);

    public class FrameConverter
    {
        public byte[] ConvertBoardToFrame(int[,] board, int x_size, int y_size)
        {
            List<byte> bufferList = new List<byte>();
            for (int i = 0; i < x_size; i++)
            {
                for (int j = 0; j < y_size; j++)
                {
                    bufferList.Add((byte)board[i, j]);
                }
            }
            return bufferList.ToArray();
        }

        public byte[] ConvertBoardToFrame(Block[,] board, int x_size, int y_size)
        {
            List<byte> bufferList = new List<byte>();
            for (int i = 0; i < x_size; i++)
            {
                for (int j = 0; j < y_size; j++)
                {
                    if (board[i, j].ContainsObjectPart)
                    {
                        bufferList.Add((byte)board[i, j].Value);
                    }
                    else
                    {                      
                        bufferList.Add(0x00);
                    }          
                }
            }
            return bufferList.ToArray();
        }

        public int[,] ConvertFrameToIntegerBoard(byte[] frame, int x_size, int y_size)
        {
            int[,] board = new int[x_size, y_size];
            int lineCount = 0;
            for (int i = 0; i < x_size; i++)
            {
                for (int j = 0; j < y_size; j++)
                {
                    board[i, j] = frame[lineCount];
                    lineCount++;
                }
            }
            return board;
        }

        public Block[,] ConvertFrameToBlockBoard(byte[] frame, int x_size, int y_size)
        {
            Block[,] board = new Block[x_size, y_size];
            int lineCount = 0;
            for (int i = 0; i < x_size; i++)
            {
                for (int j = 0; j < y_size; j++)
                {
                    board[i, j] = new Block(frame[lineCount], frame[lineCount] == 0);
                    lineCount++;
                }
            }
            return board;
        }
    }
}
