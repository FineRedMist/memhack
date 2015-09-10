using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using PSView2;

namespace MemHack
{
	/// <summary>
	/// Summary description for MemDisplay.
	/// </summary>
	public class MemDisplay : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnFindFirst;
		private System.Windows.Forms.Button btnFindNext;
		private System.Windows.Forms.Button btnSet;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label lblFound;
		private System.Threading.TimerCallback m_cb = new System.Threading.TimerCallback(UpdateList);
		private System.Threading.Timer m_timer = null;
		private System.Windows.Forms.ListView lstAddresses;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtMessages;
		
		Type m_searchType = null;
		CProcessModifier m_pm = null;
		private System.Windows.Forms.Label lblSearchSize;
		CAddressValue[] m_last = new CAddressValue[0];
		long m_count = 0;

		private delegate void UpdateListDelegate();

		UpdateListDelegate updList = null;
		public MemDisplay()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			lstAddresses.Columns.Add(Resources.Address, 80, HorizontalAlignment.Left);
			lstAddresses.Columns.Add(Resources.Value, 140, HorizontalAlignment.Left);
			MinimumSize = new Size(390, 288);
			this.btnFindFirst.Text = Resources.FindFirst;
			this.btnFindNext.Text = Resources.FindNext;
			this.lblFound.Text = Resources.AddressesFoundLabel;
			this.btnSet.Text = Resources.Set;
			this.label2.Text = Resources.MessagesLabel;
			this.txtMessages.Text = Resources.NoAddressesFound;
			this.Text = Resources.MemoryHacker;
			(new ToolTip()).SetToolTip(this, Resources.ToolTip_MemDisplayDlg);
			(new ToolTip()).SetToolTip(this.btnFindFirst, Resources.ToolTip_FindFirst);
			(new ToolTip()).SetToolTip(this.btnFindNext, Resources.ToolTip_FindNext);
			(new ToolTip()).SetToolTip(this.btnSet, Resources.ToolTip_Set);
			(new ToolTip()).SetToolTip(this.lstAddresses, Resources.ToolTip_AddressList);
			(new ToolTip()).SetToolTip(this.txtMessages, Resources.ToolTip_Messages);

			this.lstAddresses.DoubleClick += new EventHandler(lstAddresses_DoubleClick);

		}

		public bool Configure(CProcessInfo pinfo)
		{
			Text = System.String.Format(Resources.MemoryHackerFormatString, pinfo.ID, pinfo.Name, pinfo.FullPath);
			m_pm = new CProcessModifier();
			if(!m_pm.Open(pinfo.ID))
				return false;
			UpdateList();
			UpdateList();	// The second call is for debugging any problems in this function.
			updList = new UpdateListDelegate(UpdateList);
			m_timer = new System.Threading.Timer(m_cb, this, 2000, 2000);
			return true;
		}

		// For a while I thought UpdateList was generating a memory leak but it turns out that the handle it 
		// allocates every interval just doesn't get garbage collected immediately.  Ran it for 48 hours to 
		// ensure it was ok.
		static protected void UpdateList(object obj)
		{
			MemDisplay m = (MemDisplay) obj;
			if(!m.m_noUpdates)
				m.InvokeUpdate();
		}

		protected void InvokeUpdate()
		{
			Invoke(updList);
		}


		object m_lockobj = new object();
		bool m_fRunning = false;
		protected void UpdateList()
		{
			lock(m_lockobj)
			{
				if(m_fRunning)
					return;
				m_fRunning = true;
			}
			m_count = m_pm.Count;
			bool fRetrieved = false;
			CAddressValue[] avs = new CAddressValue[0];

			if(m_count > 2000)
			{
				lstAddresses.Items.Clear();
				goto Done;
			}

			fRetrieved = true;

			if(m_count == 0)
			{
				lstAddresses.Items.Clear();
				lblSearchSize.Text = "";
				goto Done;
			}

			avs = m_pm.AddressValues;
			m_count = avs.LongLength;

			if(m_count == 0)
			{
				lstAddresses.Items.Clear();
				lblSearchSize.Text = "";
				goto Done;
			}

//			txtMessages.Text = count.ToString() + " addresses found.";

			// Remove old items
			int pos = 0, pos2 = 0;
			if(m_last == null)
				m_last = new CAddressValue[0];

			bool fWasEmpty = lstAddresses.Items.Count == 0;

			foreach(CAddressValue inf in avs)
			{
				if(inf == null)
					break;
				while(pos2 < m_last.Length && 
					inf.Address > m_last[pos2].Address)
				{
					if(!fWasEmpty)
						lstAddresses.Items.RemoveAt(pos);
					++pos2;
				}
				if(pos2 >= m_last.Length || fWasEmpty)
				{
					ListViewItem lvi = new ListViewItem();
					lvi.Text = "0x" + inf.Address.ToString("X8");
					lvi.SubItems.Add(inf.Value.ToString()); 
					lstAddresses.Items.Add(lvi);
				}
				else if(inf.Address == m_last[pos2].Address &&
					m_last[pos2].Value != inf.Value)
				{
					lstAddresses.Items[pos].SubItems[1].Text = inf.Value.ToString();
				}
				++pos;
				++pos2;
			}
			while(pos < lstAddresses.Items.Count)
				lstAddresses.Items.RemoveAt(lstAddresses.Items.Count - 1);

			Done:
			if(fRetrieved)
				m_last = avs;
			CheckButtons();
			lblFound.Text = "Addresses Found: " + m_count.ToString() + ((m_count > 2000) ? " (hidden)" : "");
			lock(m_lockobj)
			{
				m_fRunning = false;
			}

		}

		protected void CheckButtons()
		{
			lock(m_lockobj)
			{
				if(m_count == 0)
				{
					btnFindNext.Enabled = false;
					btnSet.Enabled = false;
				}
				else
				{
					btnFindNext.Enabled = true;
				}
				if(lstAddresses.SelectedItems.Count == 0)
				{
					btnSet.Enabled = false;
				}
				else
				{
					btnSet.Enabled = true;
				}
			}
		}

		bool m_noUpdates = false;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			m_noUpdates = true;
			if( disposing )
			{
				if(m_timer != null)
					m_timer.Dispose();
				m_timer = null;
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MemDisplay));
			this.btnFindFirst = new System.Windows.Forms.Button();
			this.btnFindNext = new System.Windows.Forms.Button();
			this.lblFound = new System.Windows.Forms.Label();
			this.btnSet = new System.Windows.Forms.Button();
			this.lstAddresses = new System.Windows.Forms.ListView();
			this.label2 = new System.Windows.Forms.Label();
			this.txtMessages = new System.Windows.Forms.TextBox();
			this.lblSearchSize = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnFindFirst
			// 
			this.btnFindFirst.AccessibleDescription = resources.GetString("btnFindFirst.AccessibleDescription");
			this.btnFindFirst.AccessibleName = resources.GetString("btnFindFirst.AccessibleName");
			this.btnFindFirst.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("btnFindFirst.Anchor")));
			this.btnFindFirst.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnFindFirst.BackgroundImage")));
			this.btnFindFirst.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("btnFindFirst.Dock")));
			this.btnFindFirst.Enabled = ((bool)(resources.GetObject("btnFindFirst.Enabled")));
			this.btnFindFirst.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("btnFindFirst.FlatStyle")));
			this.btnFindFirst.Font = ((System.Drawing.Font)(resources.GetObject("btnFindFirst.Font")));
			this.btnFindFirst.Image = ((System.Drawing.Image)(resources.GetObject("btnFindFirst.Image")));
			this.btnFindFirst.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnFindFirst.ImageAlign")));
			this.btnFindFirst.ImageIndex = ((int)(resources.GetObject("btnFindFirst.ImageIndex")));
			this.btnFindFirst.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("btnFindFirst.ImeMode")));
			this.btnFindFirst.Location = ((System.Drawing.Point)(resources.GetObject("btnFindFirst.Location")));
			this.btnFindFirst.Name = "btnFindFirst";
			this.btnFindFirst.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("btnFindFirst.RightToLeft")));
			this.btnFindFirst.Size = ((System.Drawing.Size)(resources.GetObject("btnFindFirst.Size")));
			this.btnFindFirst.TabIndex = ((int)(resources.GetObject("btnFindFirst.TabIndex")));
			this.btnFindFirst.Text = resources.GetString("btnFindFirst.Text");
			this.btnFindFirst.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnFindFirst.TextAlign")));
			this.btnFindFirst.Visible = ((bool)(resources.GetObject("btnFindFirst.Visible")));
			this.btnFindFirst.Click += new System.EventHandler(this.btnFindFirst_Click);
			// 
			// btnFindNext
			// 
			this.btnFindNext.AccessibleDescription = resources.GetString("btnFindNext.AccessibleDescription");
			this.btnFindNext.AccessibleName = resources.GetString("btnFindNext.AccessibleName");
			this.btnFindNext.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("btnFindNext.Anchor")));
			this.btnFindNext.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnFindNext.BackgroundImage")));
			this.btnFindNext.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("btnFindNext.Dock")));
			this.btnFindNext.Enabled = ((bool)(resources.GetObject("btnFindNext.Enabled")));
			this.btnFindNext.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("btnFindNext.FlatStyle")));
			this.btnFindNext.Font = ((System.Drawing.Font)(resources.GetObject("btnFindNext.Font")));
			this.btnFindNext.Image = ((System.Drawing.Image)(resources.GetObject("btnFindNext.Image")));
			this.btnFindNext.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnFindNext.ImageAlign")));
			this.btnFindNext.ImageIndex = ((int)(resources.GetObject("btnFindNext.ImageIndex")));
			this.btnFindNext.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("btnFindNext.ImeMode")));
			this.btnFindNext.Location = ((System.Drawing.Point)(resources.GetObject("btnFindNext.Location")));
			this.btnFindNext.Name = "btnFindNext";
			this.btnFindNext.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("btnFindNext.RightToLeft")));
			this.btnFindNext.Size = ((System.Drawing.Size)(resources.GetObject("btnFindNext.Size")));
			this.btnFindNext.TabIndex = ((int)(resources.GetObject("btnFindNext.TabIndex")));
			this.btnFindNext.Text = resources.GetString("btnFindNext.Text");
			this.btnFindNext.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnFindNext.TextAlign")));
			this.btnFindNext.Visible = ((bool)(resources.GetObject("btnFindNext.Visible")));
			this.btnFindNext.Click += new System.EventHandler(this.btnFindNext_Click);
			// 
			// lblFound
			// 
			this.lblFound.AccessibleDescription = resources.GetString("lblFound.AccessibleDescription");
			this.lblFound.AccessibleName = resources.GetString("lblFound.AccessibleName");
			this.lblFound.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("lblFound.Anchor")));
			this.lblFound.AutoSize = ((bool)(resources.GetObject("lblFound.AutoSize")));
			this.lblFound.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("lblFound.Dock")));
			this.lblFound.Enabled = ((bool)(resources.GetObject("lblFound.Enabled")));
			this.lblFound.Font = ((System.Drawing.Font)(resources.GetObject("lblFound.Font")));
			this.lblFound.Image = ((System.Drawing.Image)(resources.GetObject("lblFound.Image")));
			this.lblFound.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("lblFound.ImageAlign")));
			this.lblFound.ImageIndex = ((int)(resources.GetObject("lblFound.ImageIndex")));
			this.lblFound.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("lblFound.ImeMode")));
			this.lblFound.Location = ((System.Drawing.Point)(resources.GetObject("lblFound.Location")));
			this.lblFound.Name = "lblFound";
			this.lblFound.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("lblFound.RightToLeft")));
			this.lblFound.Size = ((System.Drawing.Size)(resources.GetObject("lblFound.Size")));
			this.lblFound.TabIndex = ((int)(resources.GetObject("lblFound.TabIndex")));
			this.lblFound.Text = resources.GetString("lblFound.Text");
			this.lblFound.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("lblFound.TextAlign")));
			this.lblFound.Visible = ((bool)(resources.GetObject("lblFound.Visible")));
			// 
			// btnSet
			// 
			this.btnSet.AccessibleDescription = resources.GetString("btnSet.AccessibleDescription");
			this.btnSet.AccessibleName = resources.GetString("btnSet.AccessibleName");
			this.btnSet.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("btnSet.Anchor")));
			this.btnSet.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSet.BackgroundImage")));
			this.btnSet.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("btnSet.Dock")));
			this.btnSet.Enabled = ((bool)(resources.GetObject("btnSet.Enabled")));
			this.btnSet.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("btnSet.FlatStyle")));
			this.btnSet.Font = ((System.Drawing.Font)(resources.GetObject("btnSet.Font")));
			this.btnSet.Image = ((System.Drawing.Image)(resources.GetObject("btnSet.Image")));
			this.btnSet.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnSet.ImageAlign")));
			this.btnSet.ImageIndex = ((int)(resources.GetObject("btnSet.ImageIndex")));
			this.btnSet.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("btnSet.ImeMode")));
			this.btnSet.Location = ((System.Drawing.Point)(resources.GetObject("btnSet.Location")));
			this.btnSet.Name = "btnSet";
			this.btnSet.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("btnSet.RightToLeft")));
			this.btnSet.Size = ((System.Drawing.Size)(resources.GetObject("btnSet.Size")));
			this.btnSet.TabIndex = ((int)(resources.GetObject("btnSet.TabIndex")));
			this.btnSet.Text = resources.GetString("btnSet.Text");
			this.btnSet.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("btnSet.TextAlign")));
			this.btnSet.Visible = ((bool)(resources.GetObject("btnSet.Visible")));
			this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
			// 
			// lstAddresses
			// 
			this.lstAddresses.AccessibleDescription = resources.GetString("lstAddresses.AccessibleDescription");
			this.lstAddresses.AccessibleName = resources.GetString("lstAddresses.AccessibleName");
			this.lstAddresses.Alignment = ((System.Windows.Forms.ListViewAlignment)(resources.GetObject("lstAddresses.Alignment")));
			this.lstAddresses.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("lstAddresses.Anchor")));
			this.lstAddresses.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("lstAddresses.BackgroundImage")));
			this.lstAddresses.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("lstAddresses.Dock")));
			this.lstAddresses.Enabled = ((bool)(resources.GetObject("lstAddresses.Enabled")));
			this.lstAddresses.Font = ((System.Drawing.Font)(resources.GetObject("lstAddresses.Font")));
			this.lstAddresses.FullRowSelect = true;
			this.lstAddresses.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("lstAddresses.ImeMode")));
			this.lstAddresses.LabelWrap = ((bool)(resources.GetObject("lstAddresses.LabelWrap")));
			this.lstAddresses.Location = ((System.Drawing.Point)(resources.GetObject("lstAddresses.Location")));
			this.lstAddresses.MultiSelect = false;
			this.lstAddresses.Name = "lstAddresses";
			this.lstAddresses.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("lstAddresses.RightToLeft")));
			this.lstAddresses.Size = ((System.Drawing.Size)(resources.GetObject("lstAddresses.Size")));
			this.lstAddresses.TabIndex = ((int)(resources.GetObject("lstAddresses.TabIndex")));
			this.lstAddresses.Text = resources.GetString("lstAddresses.Text");
			this.lstAddresses.View = System.Windows.Forms.View.Details;
			this.lstAddresses.Visible = ((bool)(resources.GetObject("lstAddresses.Visible")));
			this.lstAddresses.SelectedIndexChanged += new System.EventHandler(this.lstAddresses_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AccessibleDescription = resources.GetString("label2.AccessibleDescription");
			this.label2.AccessibleName = resources.GetString("label2.AccessibleName");
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("label2.Anchor")));
			this.label2.AutoSize = ((bool)(resources.GetObject("label2.AutoSize")));
			this.label2.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("label2.Dock")));
			this.label2.Enabled = ((bool)(resources.GetObject("label2.Enabled")));
			this.label2.Font = ((System.Drawing.Font)(resources.GetObject("label2.Font")));
			this.label2.Image = ((System.Drawing.Image)(resources.GetObject("label2.Image")));
			this.label2.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("label2.ImageAlign")));
			this.label2.ImageIndex = ((int)(resources.GetObject("label2.ImageIndex")));
			this.label2.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("label2.ImeMode")));
			this.label2.Location = ((System.Drawing.Point)(resources.GetObject("label2.Location")));
			this.label2.Name = "label2";
			this.label2.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("label2.RightToLeft")));
			this.label2.Size = ((System.Drawing.Size)(resources.GetObject("label2.Size")));
			this.label2.TabIndex = ((int)(resources.GetObject("label2.TabIndex")));
			this.label2.Text = resources.GetString("label2.Text");
			this.label2.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("label2.TextAlign")));
			this.label2.Visible = ((bool)(resources.GetObject("label2.Visible")));
			// 
			// txtMessages
			// 
			this.txtMessages.AccessibleDescription = resources.GetString("txtMessages.AccessibleDescription");
			this.txtMessages.AccessibleName = resources.GetString("txtMessages.AccessibleName");
			this.txtMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("txtMessages.Anchor")));
			this.txtMessages.AutoSize = ((bool)(resources.GetObject("txtMessages.AutoSize")));
			this.txtMessages.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("txtMessages.BackgroundImage")));
			this.txtMessages.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("txtMessages.Dock")));
			this.txtMessages.Enabled = ((bool)(resources.GetObject("txtMessages.Enabled")));
			this.txtMessages.Font = ((System.Drawing.Font)(resources.GetObject("txtMessages.Font")));
			this.txtMessages.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("txtMessages.ImeMode")));
			this.txtMessages.Location = ((System.Drawing.Point)(resources.GetObject("txtMessages.Location")));
			this.txtMessages.MaxLength = ((int)(resources.GetObject("txtMessages.MaxLength")));
			this.txtMessages.Multiline = ((bool)(resources.GetObject("txtMessages.Multiline")));
			this.txtMessages.Name = "txtMessages";
			this.txtMessages.PasswordChar = ((char)(resources.GetObject("txtMessages.PasswordChar")));
			this.txtMessages.ReadOnly = true;
			this.txtMessages.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("txtMessages.RightToLeft")));
			this.txtMessages.ScrollBars = ((System.Windows.Forms.ScrollBars)(resources.GetObject("txtMessages.ScrollBars")));
			this.txtMessages.Size = ((System.Drawing.Size)(resources.GetObject("txtMessages.Size")));
			this.txtMessages.TabIndex = ((int)(resources.GetObject("txtMessages.TabIndex")));
			this.txtMessages.TabStop = false;
			this.txtMessages.Text = resources.GetString("txtMessages.Text");
			this.txtMessages.TextAlign = ((System.Windows.Forms.HorizontalAlignment)(resources.GetObject("txtMessages.TextAlign")));
			this.txtMessages.Visible = ((bool)(resources.GetObject("txtMessages.Visible")));
			this.txtMessages.WordWrap = ((bool)(resources.GetObject("txtMessages.WordWrap")));
			// 
			// lblSearchSize
			// 
			this.lblSearchSize.AccessibleDescription = resources.GetString("lblSearchSize.AccessibleDescription");
			this.lblSearchSize.AccessibleName = resources.GetString("lblSearchSize.AccessibleName");
			this.lblSearchSize.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("lblSearchSize.Anchor")));
			this.lblSearchSize.AutoSize = ((bool)(resources.GetObject("lblSearchSize.AutoSize")));
			this.lblSearchSize.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("lblSearchSize.Dock")));
			this.lblSearchSize.Enabled = ((bool)(resources.GetObject("lblSearchSize.Enabled")));
			this.lblSearchSize.Font = ((System.Drawing.Font)(resources.GetObject("lblSearchSize.Font")));
			this.lblSearchSize.Image = ((System.Drawing.Image)(resources.GetObject("lblSearchSize.Image")));
			this.lblSearchSize.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("lblSearchSize.ImageAlign")));
			this.lblSearchSize.ImageIndex = ((int)(resources.GetObject("lblSearchSize.ImageIndex")));
			this.lblSearchSize.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("lblSearchSize.ImeMode")));
			this.lblSearchSize.Location = ((System.Drawing.Point)(resources.GetObject("lblSearchSize.Location")));
			this.lblSearchSize.Name = "lblSearchSize";
			this.lblSearchSize.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("lblSearchSize.RightToLeft")));
			this.lblSearchSize.Size = ((System.Drawing.Size)(resources.GetObject("lblSearchSize.Size")));
			this.lblSearchSize.TabIndex = ((int)(resources.GetObject("lblSearchSize.TabIndex")));
			this.lblSearchSize.Text = resources.GetString("lblSearchSize.Text");
			this.lblSearchSize.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("lblSearchSize.TextAlign")));
			this.lblSearchSize.Visible = ((bool)(resources.GetObject("lblSearchSize.Visible")));
			// 
			// MemDisplay
			// 
			this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
			this.AccessibleName = resources.GetString("$this.AccessibleName");
			this.AutoScaleBaseSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScaleBaseSize")));
			this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
			this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
			this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.ClientSize = ((System.Drawing.Size)(resources.GetObject("$this.ClientSize")));
			this.Controls.Add(this.lblSearchSize);
			this.Controls.Add(this.txtMessages);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.lstAddresses);
			this.Controls.Add(this.btnSet);
			this.Controls.Add(this.lblFound);
			this.Controls.Add(this.btnFindNext);
			this.Controls.Add(this.btnFindFirst);
			this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
			this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
			this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
			this.MaximumSize = ((System.Drawing.Size)(resources.GetObject("$this.MaximumSize")));
			this.MinimumSize = ((System.Drawing.Size)(resources.GetObject("$this.MinimumSize")));
			this.Name = "MemDisplay";
			this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = ((System.Windows.Forms.FormStartPosition)(resources.GetObject("$this.StartPosition")));
			this.Text = resources.GetString("$this.Text");
			this.Closed += new System.EventHandler(this.MemDisplay_Closed);
			this.ResumeLayout(false);

		}
		#endregion

		private object getValue(string s)
		{
			try
			{
				return Convert.ChangeType(s, m_searchType);
			}
			catch
			{
				return null;
			}
		}

		private void btnFindFirst_Click(object sender, System.EventArgs e)
		{
			DisableUpdate();

			FindFirst ff = new FindFirst();
			if(ff.ShowDialog(this) != DialogResult.OK)
			{
				txtMessages.Text = Resources.FindFirstCanceled;
				goto Done;
			}

			if(ff.radioByte.Checked)
			{
				m_searchType = typeof(Byte);
				lblSearchSize.Text = Resources.SearchSize1Byte;
			}
			else if(ff.radioShort.Checked)
			{
				m_searchType = typeof(UInt16);
				lblSearchSize.Text = Resources.SearchSize2Bytes;
			}
			else if(ff.radioInt.Checked)
			{
				m_searchType = typeof(UInt32);
				lblSearchSize.Text = Resources.SearchSize4Bytes;
			}
			else if(ff.radioLong.Checked)
			{
				m_searchType = typeof(UInt64);
				lblSearchSize.Text = Resources.SearchSize8Bytes;
			}
			else
				m_searchType = null;
			lock(m_lockobj)
			{
				object obj = getValue(ff.textValue.Text);
				if(obj == null)
				{
					txtMessages.Text = Resources.ErrorFailedToConvert;
					goto Done;
				}
				ProgressBar pb = new ProgressBar();
				pb.Owner = this;
				pb.Show();
				txtMessages.Text = String.Format(Resources.SearchingForFormatString, ff.textValue.Text);
				try
				{
					UInt64 count = m_pm.FindFirst(obj, pb);
					txtMessages.Text = String.Format(Resources.FindFirstFoundFormatString, count);
				}
				catch(System.Exception f)
				{
					txtMessages.Text = String.Format(Resources.FindFirstFailedFormatString, f.Message);
				}

				System.Threading.Thread.Sleep(500);
				pb.Hide();
				pb.Close();
				pb = null;
				m_last = null;
				lstAddresses.Items.Clear();
				UpdateList();
			}
			Done:
				EnableUpdate();
		}

		private void btnFindNext_Click(object sender, System.EventArgs e)
		{
			DisableUpdate();

			SetNext sn = new SetNext(true, 0, 0);
			if(sn.ShowDialog(this) == DialogResult.OK)
			{
				lock(m_lockobj)
				{
					object obj = getValue(sn.Value);
					if(obj == null)
					{
						txtMessages.Text = Resources.ErrorFailedToConvert;
						goto Done;
					}
					txtMessages.Text = String.Format(Resources.SearchingForFormatString, sn.Value);
					ProgressBar pb = new ProgressBar();
					pb.Owner = this;
					pb.Show();
					try
					{
						UInt64 count = m_pm.FindNext(obj, pb);
						txtMessages.Text = String.Format(Resources.FindNextFoundFormatString, count);
					}
					catch(System.Exception f)
					{
						txtMessages.Text = String.Format(Resources.FindNextFailedFormatString, f.Message);
					}
					System.Threading.Thread.Sleep(500);
					pb.Hide();
					pb.Close();
					pb = null;
					UpdateList();
				}
			}
			else
			{
				txtMessages.Text = Resources.FindNextCanceled;
			}

			Done:
				EnableUpdate();
		}

		private void btnSet_Click(object sender, System.EventArgs e)
		{
			if(lstAddresses.SelectedItems.Count == 0)
				return;
			uint addr = 0;
			ulong val = 0;
			lock(m_lockobj)
			{
				addr = Convert.ToUInt32(lstAddresses.SelectedItems[0].Text.Substring(2), 16);
				val = Convert.ToUInt64(lstAddresses.SelectedItems[0].SubItems[1].Text);
			}
			if(addr == 0)
			{
				txtMessages.Text = String.Format(Resources.ErrorInvalidAddressFormatString, lstAddresses.SelectedItems[0].Text);;
				return;
			}
			SetNext sn = new SetNext(false, addr, val);
			if(sn.ShowDialog(this) == DialogResult.OK)
			{
				lock(m_lockobj)
				{
					object obj = getValue(sn.Value);
					if(obj == null)
					{
						txtMessages.Text = Resources.ErrorFailedToConvert;
						return;
					}
					try
					{
						if(m_pm.SetValue(addr, obj))
							txtMessages.Text = String.Format(Resources.ValueAtAddressSetFormatString, addr.ToString("X8"), sn.Value);
						else
							txtMessages.Text = String.Format(Resources.ErrorValueNotSetFormatString, m_pm.LastError);
						UpdateList();
					}
					catch(System.Exception f)
					{
						txtMessages.Text = String.Format(Resources.ErrorSetValueFailedFormatString, f.Message);
					}
				}
			}
			else
			{
				txtMessages.Text = Resources.SetValueCanceled;
			}
		}

		private void lstAddresses_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			CheckButtons();
		}

		private void DisableUpdate()
		{
			m_timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
		}

		private void EnableUpdate()
		{
			m_timer.Change(2000, 2000);
		}

		private void MemDisplay_Closed(object sender, System.EventArgs e)
		{
			DisableUpdate();
			m_pm.Close();
		}

		private void lstAddresses_DoubleClick(object sender, EventArgs e)
		{
			btnSet_Click(null, null);
		}
	}
}
