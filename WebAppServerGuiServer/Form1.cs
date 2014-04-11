using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using AspNetConnector;
using Newtonsoft.Json;
using ZMQ;

namespace WebGui
{
	public partial class Form1 : Form
	{
		private bool serverRunning;
		
		public Form1()
		{
			serverRunning = false;

			InitializeComponent();
		}

		private void startServer_Click(object sender, EventArgs e)
		{
			if (!serverRunning)
			{
				serverRunning = true;

				backgroundWorker1.RunWorkerAsync();

				startServer.Text = "Stop App Server";
			}
			else
			{
				if (backgroundWorker1.IsBusy)
				{
					backgroundWorker1.CancelAsync();
					serverRunning = false;
					startServer.Enabled = false;
				}
			}
		}

		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;

			using (var context = new Context(1))
			{
				using (Socket listener = context.Socket(SocketType.REP))
				{
					listener.Bind("tcp://127.0.0.1:9000");

					while (!worker.CancellationPending)
					{
						string requestJson = listener.Recv(Encoding.Unicode);

						Request request = JsonConvert.DeserializeObject<Request>(requestJson);

						if (request != null)
						{
							var response = new Response();
							response.ContentType = "text/html";
							response.Body = request.Path;
							response.Status = 200;

							listener.Send(JsonConvert.SerializeObject(response), Encoding.Unicode);

							if (InvokeRequired)
							{
								Invoke(new EventHandler(delegate
								{
									string[] row = { request.IpAddress, request.SessionID, request.Identity ?? string.Empty, request.Path, response.Body };
									ListViewItem lvi = new ListViewItem(row);
									dataView.Items.Add(lvi);
								}));
							}
						}
						else
						{
							var response = new Response();

							response.ContentType = "text/html";
							response.Body = "Could not process this request";
							response.Status = 500;

							listener.Send(JsonConvert.SerializeObject(response), Encoding.Unicode);
						}
					}
				}
			}

			if (worker.CancellationPending)
			{
				e.Cancel = true;

				if (InvokeRequired)
				{
					Invoke(new EventHandler(delegate
					{
						startServer.Enabled = true;
						startServer.Text = "Start App Server";
					}));
				}
			}
		}
	}
}
