using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TetrisExtension;

namespace TetrisExtension
{
    public enum BlockColor { Grey = 0, Yellow = 1, Red = 6, Blue = 3, Green = 5, LightBlue = 7, Purple = 2, Orange = 4};

    public enum BlockProperties { PartOfMovingObject, RotationPoint };

    public class Block
    {
        public BlockColor Color { get; private set; }

        public BlockProperties[] Properties { get; private set; }

        public int Value { get; private set; }

        public int XPos { get; private set; }

        public int YPos { get; private set; }

        public Block(BlockColor color, int x_pos, int y_pos, params BlockProperties[] properties)
        {
            Color = color;
            XPos = x_pos;
            YPos = y_pos;
            Properties = properties;
            Value = (int)Color;
        }

        public void SetPosition(int newX_pos, int newY_pos)
        {
            XPos += newX_pos;
            YPos += newY_pos;
        }

        public void SetColor(BlockColor newColor)
        {
            Color = newColor;
            Value = (int)Color;
        }

        public void SetBlockProperties(params BlockProperties[] newProperties)
        {
            Properties = newProperties;
        }
    }

    public class Board
    {
        public Block[,] CurrentBoard { get; private set; }

        public int XSize { get; private set; }

        public int YSize { get; private set; }

        public Board(int x_size, int y_size)
        {
            XSize = x_size;
            YSize = y_size;
            CurrentBoard = new Block[x_size, y_size];
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    CurrentBoard[i, j] = new Block(BlockColor.Grey, i, j);
                }
            }
        }

        public void SetBlock(Block newBlock)
        {
            CurrentBoard[newBlock.XPos, newBlock.YPos] = newBlock;
        }

        public void MoveObjectParts(int newX_pos, int newY_pos)
        {
            List<Block> blocksToMove = new List<Block>();
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    if (CurrentBoard[i, j].Properties.Contains(BlockProperties.PartOfMovingObject))
                    {
                        blocksToMove.Add(CurrentBoard[i, j]);
                        CurrentBoard[i, j] = new Block(BlockColor.Grey, i, j);
                    }
                }
            }

            for (int i = 0; i < blocksToMove.Count; i++)
            {
                blocksToMove[i].SetPosition(newX_pos, newY_pos);
                SetBlock(blocksToMove[i]);
            }
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
                    newBoard[i, j] = new Block(BlockColor.Grey, 0, 0);
                    if (CurrentBoard[i, j].Value == 0)
                    {
                        rowsToMove.Add(i);
                        rowIsFull = false;
                        break;
                    }
                }

                if (rowIsFull)
                {
                    for (int j = 0; j < YSize; j++)
                    {
                        CurrentBoard[i, j].SetColor(BlockColor.Grey);
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

            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    if (newBoard[i, j] == null)
                    {
                        newBoard[i, j] = new Block(BlockColor.Grey, i, j);
                    }
                }
            }
            CurrentBoard = newBoard;
        }

        public void SetNewBoard(Block[,] newBoard)
        {
            CurrentBoard = newBoard;
        }

        public Block[,] GetCurrentBoard()
        {
            return CurrentBoard;
        }

        public void ClearBoard()
        {
            for (int i = 0; i < XSize; i++)
            {
                for (int j = 0; j < YSize; j++)
                {
                    CurrentBoard[i, j] = new Block(BlockColor.Grey, i, j);
                }
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
            public byte[] ConvertBoardToFrame(Board boardToConvert)
            {
                StringBuilder buffer = new StringBuilder();
                buffer.AppendLine(string.Format("{0};{1}", boardToConvert.XSize, boardToConvert.YSize));
                for (int i = 0; i < boardToConvert.XSize; i++)
                {
                    for (int j = 0; j < boardToConvert.YSize; j++)
                    {
                        buffer.Append(string.Format("{0};{1};", boardToConvert.CurrentBoard[i, j].XPos, boardToConvert.CurrentBoard[i, j].YPos));
                        if (boardToConvert.CurrentBoard[i, j].Properties.Contains(BlockProperties.PartOfMovingObject))
                        {
                            buffer.Append(string.Format("{0};", 1));
                        }
                        else
                        {
                            buffer.Append(string.Format("{0};", 0));
                        }
                        if (boardToConvert.CurrentBoard[i, j].Properties.Contains(BlockProperties.RotationPoint))
                        {
                            buffer.Append(string.Format("{0};", 1));
                        }
                        else
                        {
                            buffer.Append(string.Format("{0};", 0));
                        }
                        buffer.AppendLine(string.Format("{0}", boardToConvert.CurrentBoard[i, j].Value));
                    }
                }
                return Encoding.ASCII.GetBytes(buffer.ToString());
            }

            public Board ConvertFrameToBoard(byte[] frameToConvert)
            {
                string buffer = Encoding.ASCII.GetString(frameToConvert);
                string[] lines = buffer.Replace("\n", "$").Split('$');
                Board convertedBoard = new Board(int.Parse(lines[0].Split(';')[0]), int.Parse(lines[0].Split(';')[1]));
                for (int i = 1; i < lines.Length; i++)
                {
                    switch (int.Parse(lines[i].Split(';')[4]))
                    {
                        case 0:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Grey);
                            break;
                        case 1:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Yellow);
                            break;
                        case 2:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Purple);
                            break;
                        case 3:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Blue);
                            break;
                        case 4:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Orange);
                            break;
                        case 5:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Green);
                            break;
                        case 6:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.Red);
                            break;
                        case 7:
                            convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetColor(BlockColor.LightBlue);
                            break;
                    }
                    List<BlockProperties> props = new List<BlockProperties>();
                    if (int.Parse(lines[i].Split(';')[2]) == 1)
                    {
                        props.Add(BlockProperties.PartOfMovingObject);
                    }
                    if (int.Parse(lines[i].Split(';')[3]) == 1)
                    {
                        props.Add(BlockProperties.RotationPoint);
                    }
                    convertedBoard.CurrentBoard[int.Parse(lines[i].Split(';')[0]), int.Parse(lines[i].Split(';')[1])].SetBlockProperties(props.ToArray());
                }
                return convertedBoard;
            }
        }
    }
}
