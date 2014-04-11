namespace WebGui
{
	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.startServer = new System.Windows.Forms.Button();
			this.dataView = new System.Windows.Forms.ListView();
			this.ClientIDHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.IdentityHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RequestPathHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ResponseHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.IpAddressHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
			this.SuspendLayout();
			// 
			// startServer
			// 
			this.startServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.startServer.Location = new System.Drawing.Point(768, 389);
			this.startServer.Name = "startServer";
			this.startServer.Size = new System.Drawing.Size(127, 26);
			this.startServer.TabIndex = 0;
			this.startServer.Text = "Start App Server";
			this.startServer.UseVisualStyleBackColor = true;
			this.startServer.Click += new System.EventHandler(this.startServer_Click);
			// 
			// dataView
			// 
			this.dataView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IpAddressHeader,
            this.ClientIDHeader,
            this.IdentityHeader,
            this.RequestPathHeader,
            this.ResponseHeader});
			this.dataView.FullRowSelect = true;
			this.dataView.Location = new System.Drawing.Point(18, 12);
			this.dataView.Name = "dataView";
			this.dataView.Size = new System.Drawing.Size(877, 371);
			this.dataView.TabIndex = 1;
			this.dataView.UseCompatibleStateImageBehavior = false;
			this.dataView.View = System.Windows.Forms.View.Details;
			// 
			// ClientIDHeader
			// 
			this.ClientIDHeader.Text = "Client ID";
			this.ClientIDHeader.Width = 154;
			// 
			// IdentityHeader
			// 
			this.IdentityHeader.Text = "Identity";
			this.IdentityHeader.Width = 170;
			// 
			// RequestPathHeader
			// 
			this.RequestPathHeader.Text = "Request Path";
			this.RequestPathHeader.Width = 198;
			// 
			// ResponseHeader
			// 
			this.ResponseHeader.Text = "Response";
			this.ResponseHeader.Width = 188;
			// 
			// IpAddressHeader
			// 
			this.IpAddressHeader.Text = "IP Address";
			this.IpAddressHeader.Width = 125;
			// 
			// backgroundWorker1
			// 
			this.backgroundWorker1.WorkerSupportsCancellation = true;
			this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(907, 427);
			this.Controls.Add(this.dataView);
			this.Controls.Add(this.startServer);
			this.Name = "Form1";
			this.Text = "Web App Control Center";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button startServer;
		private System.Windows.Forms.ListView dataView;
		private System.Windows.Forms.ColumnHeader ClientIDHeader;
		private System.Windows.Forms.ColumnHeader RequestPathHeader;
		private System.Windows.Forms.ColumnHeader ResponseHeader;
		private System.ComponentModel.BackgroundWorker backgroundWorker1;
		private System.Windows.Forms.ColumnHeader IdentityHeader;
		private System.Windows.Forms.ColumnHeader IpAddressHeader;



	}
}

