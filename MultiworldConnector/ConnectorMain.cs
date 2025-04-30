using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace MultiworldConnector.MultiworldConnector
{
	[ExternalTool("Multiworld Connector")]
	public class ConnectorMain : ToolFormBase, IExternalToolForm
	{
		protected override string WindowTitleStatic => "Multiworld Connector";

		public static string Status = "Not connected";
		public bool Connected = false;
		public ApiContainer? _maybeAPIContainer { get; set; }
		private ApiContainer APIs => _maybeAPIContainer!;
		
		private bool _debugEnabled = false;

		private readonly TextBox logBox;

		private Thread? SocketThread;
		private TcpClient? client;
		private NetworkStream? stream;

		const int PORT = 13249;

		static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(5);

		public ConnectorMain()
		{
			logBox = new TextBox
			{
				Multiline   = true,
				ReadOnly    = true,
				ScrollBars  = ScrollBars.Vertical,
				Dock        = DockStyle.Fill,
				Font        = new Font("Consolas", 9),
				BackColor   = Color.Black,
				ForeColor   = Color.LightGreen
			};

			BuildUI();
		}

		private void BuildUI()
		{
			SuspendLayout();
			Controls.Clear();

			ClientSize = new Size(480, 320);

			var layout = new TableLayoutPanel
			{
				Dock        = DockStyle.Fill,
				ColumnCount = 1,
				RowCount    = 4,
				AutoSize    = true,
				Padding     = new Padding(10),
			};
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // title
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // status
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // buttons
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // log

			var titleLabel = new Label
			{
				AutoSize = true,
				Text     = "Welcome to the Multiworld Connector!",
				Font     = new Font("Segoe UI", 12, FontStyle.Bold),
				Dock     = DockStyle.Fill,
				TextAlign= ContentAlignment.MiddleLeft,
				Margin   = new Padding(0, 0, 0, 10)
			};

			var statusLabel = new Label
			{
				AutoSize = true,
				Text     = $"Status: {Status}",
				Font     = new Font("Segoe UI", 10),
				Dock     = DockStyle.Fill,
				TextAlign= ContentAlignment.MiddleLeft,
				Margin   = new Padding(0, 0, 0, 10)
			};
			
			var buttonPanel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize      = true,
				Dock          = DockStyle.Top,
				Margin        = new Padding(0, 0, 0, 10)
			};

			if (Connected)
			{
				var disconnectButton = new Button { Text = "Disconnect", AutoSize = true };
				disconnectButton.Click += (s, e) => AttemptDisconnection();
				buttonPanel.Controls.Add(disconnectButton);
			}
			else
			{
				var connectButton = new Button { Text = "Connect", AutoSize = true };
				connectButton.Click += (s, e) => AttemptConnection();
				buttonPanel.Controls.Add(connectButton);
			}
			
			var debugButton = new Button
			{
				AutoSize = true,
				Text     = _debugEnabled ? "Debug ON" : "Debug OFF"
			};
			debugButton.Click += (s, e) =>
			{
				_debugEnabled = !_debugEnabled;
				debugButton.Text = _debugEnabled ? "Debug ON" : "Debug OFF";
				Log($"Read/Write debug {(_debugEnabled ? "enabled" : "disabled")}");
			};
			buttonPanel.Controls.Add(debugButton);

			layout.Controls.Add(titleLabel,  0, 0);
			layout.Controls.Add(statusLabel, 0, 1);
			layout.Controls.Add(buttonPanel, 0, 2);
			layout.Controls.Add(logBox,      0, 3);

			Controls.Add(layout);
			ResumeLayout(performLayout: true);
		}

		private void SafeInvoke(Action uiAction)
		{
			if (InvokeRequired) Invoke(uiAction);
			else uiAction();
		}
		
		private void DebugLog(string msg)
		{
			if (!_debugEnabled) return;
			Log(msg);
		}

		private void Log(string msg)
		{
			Console.WriteLine(msg); // still goes to the console
	
			SafeInvoke(() =>
			{
				logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
				logBox.SelectionStart = logBox.Text.Length;
				logBox.ScrollToCaret();
			});
		}
		
		public void AttemptConnection()
		{
			SafeInvoke(() =>
			{
				Status = "Attempting connection...";
				BuildUI();
			});

			SocketThread = new Thread(ConnectLoop) { IsBackground = true };
			SocketThread.Start();
		}

		public void AttemptDisconnection()
		{
			SafeInvoke(() =>
			{
				Status = "Attempting disconnection...";
				BuildUI();
			});

			try
			{
				client?.Close();
				SocketThread?.Join(500);
			}
			catch { }

			SafeInvoke(() =>
			{
				Connected = false;
				Status = "Disconnected";
				BuildUI();
			});
		}

		private void ConnectLoop()
		{
			if (!ConnectSocketOnce())
			{
				Log("Initial connect failed; aborting.");
				return;
			}

			while (client!.Connected)
			{
				try
				{
					if (stream!.DataAvailable)
					{
						int opcode = stream.ReadByte();
						if (opcode == -1)
						{
							Log("Connection closed by peer.");
							break;
						}
						DebugLog($"→ OPCODE {opcode}");
						DispatchOpcode(opcode);
					}
				}
				catch (Exception ex)
				{
					Log($"Error in receive loop: {ex.Message}");
					break;
				}
			}

			SafeInvoke(() =>
			{
				Connected = false;
				Status = "Connection ended";
				BuildUI();
			});

			Log("Connect thread exiting.");
		}

		private bool ConnectSocketOnce()
		{
			try
			{
				client = new TcpClient(AddressFamily.InterNetworkV6);

				var task = client.ConnectAsync(IPAddress.IPv6Loopback, PORT);
				if (!task.Wait(CONNECT_TIMEOUT))
					throw new TimeoutException("Connect timed out");

				stream = client.GetStream();
				Log($"Connected to [::1]:{PORT}");
				SafeInvoke(() =>
				{
					Connected = true;
					Status = $"Connected to [::1]:{PORT}";
					BuildUI();
				});
				return true;
			}
			catch (Exception ex)
			{
				Log($"Connect failed: {ex}");
				SafeInvoke(() =>
				{
					Connected = false;
					Status = $"Failed to connect to [::1]:{PORT}";
					BuildUI();
				});
				return false;
			}
		}

		private void DispatchOpcode(int opcode)
		{
			APIs.Memory.UseMemoryDomain("System Bus");
			APIs.Memory.SetBigEndian(true);
			switch (opcode)
			{
				case 2: HandleRead (1, addr => APIs.Memory.ReadByte(addr));  break;
				case 3: HandleRead (2, addr => APIs.Memory.ReadU16(addr));   break;
				case 4: HandleRead (4, addr => APIs.Memory.ReadU32(addr));   break;
				case 6: HandleWrite(1, (addr, data) => APIs.Memory.WriteByte(addr, (byte )data)); break;
				case 7: HandleWrite(2, (addr, data) => APIs.Memory.WriteU16(addr, (ushort)data)); break;
				case 8: HandleWrite(4, (addr, data) => APIs.Memory.WriteU32(addr, (uint  )data)); break;
				default: Log($"Unknown OPCODE {opcode}"); break;
			}
		}

		private void HandleRead(int size, Func<uint, uint> readFunc)
		{
			var buf = ReadBytes(4);
			if (buf == null) return;

			uint addr = BitConverter.ToUInt32(buf, 0);
			uint data = readFunc(addr);

			byte[] resp = size switch
			{
				1 => new[] { (byte)(data & 0xFF) },
				2 => BitConverter.GetBytes((ushort)data),
				4 => BitConverter.GetBytes(data),
				_ => throw new ArgumentOutOfRangeException()
			};

			stream!.Write(resp, 0, resp.Length);
			DebugLog($"READ{size * 8} @0x{addr:X8} = {data}");
		}

		private void HandleWrite(int size, Action<uint, uint> writeFunc)
		{
			var addrBuf = ReadBytes(4);
			var dataBuf = ReadBytes(size);
			if (addrBuf == null || dataBuf == null) return;

			uint addr = BitConverter.ToUInt32(addrBuf, 0);
			uint data = size switch
			{
				1 => dataBuf[0],
				2 => BitConverter.ToUInt16(dataBuf, 0),
				4 => BitConverter.ToUInt32(dataBuf, 0),
				_ => throw new ArgumentOutOfRangeException()
			};

			writeFunc(addr, data);
			DebugLog($"WRITE{size * 8} @0x{addr:X8}");
		}

		private byte[]? ReadBytes(int count)
		{
			var buffer = new byte[count];
			int read = 0;
			while (read < count)
			{
				if (stream == null || !stream.DataAvailable)
					return null;
				int got = stream.Read(buffer, read, count - read);
				if (got == 0) return null;
				read += got;
			}
			return buffer;
		}
	}
}