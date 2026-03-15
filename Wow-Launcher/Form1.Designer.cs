namespace Wow_Launcher
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.pnlHeaderDivider = new System.Windows.Forms.Panel();
            this.lblRemoteVersionValue = new System.Windows.Forms.Label();
            this.lblRemoteVersion = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.btnClearCache = new System.Windows.Forms.Button();
            this.btnLaunchGame = new System.Windows.Forms.Button();
            this.btnUpdateClient = new System.Windows.Forms.Button();
            this.btnCheckUpdates = new System.Windows.Forms.Button();
            this.btnBrowseClient = new System.Windows.Forms.Button();
            this.cmbLanguage = new System.Windows.Forms.ComboBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.txtClientPath = new System.Windows.Forms.TextBox();
            this.lblOnlinePlayerCountValue = new System.Windows.Forms.Label();
            this.lblOnlinePlayerCount = new System.Windows.Forms.Label();
            this.lblRealmStatusIndicatorValue = new System.Windows.Forms.Label();
            this.lblRealmStatusIndicator = new System.Windows.Forms.Label();
            this.lblClientPath = new System.Windows.Forms.Label();
            this.lblClientHint = new System.Windows.Forms.Label();
            this.grpNews = new System.Windows.Forms.GroupBox();
            this.txtNews = new System.Windows.Forms.RichTextBox();
            this.lblNewsHint = new System.Windows.Forms.Label();
            this.grpUpdates = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblLog = new System.Windows.Forms.Label();
            this.lstFilesToUpdate = new System.Windows.Forms.ListBox();
            this.lblFilesToUpdate = new System.Windows.Forms.Label();
            this.lblUpdatesHint = new System.Windows.Forms.Label();
            this.progressUpdate = new System.Windows.Forms.ProgressBar();
            this.pnlHeader.SuspendLayout();
            this.grpConnection.SuspendLayout();
            this.grpNews.SuspendLayout();
            this.grpUpdates.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(22)))), ((int)(((byte)(18)))));
            this.pnlHeader.Controls.Add(this.pnlHeaderDivider);
            this.pnlHeader.Controls.Add(this.lblRemoteVersionValue);
            this.pnlHeader.Controls.Add(this.lblRemoteVersion);
            this.pnlHeader.Controls.Add(this.lblStatusValue);
            this.pnlHeader.Controls.Add(this.lblStatus);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1224, 126);
            this.pnlHeader.TabIndex = 0;
            // 
            // pnlHeaderDivider
            // 
            this.pnlHeaderDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(104)))), ((int)(((byte)(44)))));
            this.pnlHeaderDivider.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlHeaderDivider.Location = new System.Drawing.Point(0, 124);
            this.pnlHeaderDivider.Name = "pnlHeaderDivider";
            this.pnlHeaderDivider.Size = new System.Drawing.Size(1224, 2);
            this.pnlHeaderDivider.TabIndex = 6;
            // 
            // lblRemoteVersionValue
            // 
            this.lblRemoteVersionValue.AutoSize = true;
            this.lblRemoteVersionValue.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRemoteVersionValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(203)))), ((int)(((byte)(110)))));
            this.lblRemoteVersionValue.Location = new System.Drawing.Point(1010, 56);
            this.lblRemoteVersionValue.Name = "lblRemoteVersionValue";
            this.lblRemoteVersionValue.Size = new System.Drawing.Size(20, 30);
            this.lblRemoteVersionValue.TabIndex = 5;
            this.lblRemoteVersionValue.Text = "-";
            // 
            // lblRemoteVersion
            // 
            this.lblRemoteVersion.AutoSize = true;
            this.lblRemoteVersion.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRemoteVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(158)))), ((int)(((byte)(139)))));
            this.lblRemoteVersion.Location = new System.Drawing.Point(1011, 34);
            this.lblRemoteVersion.Name = "lblRemoteVersion";
            this.lblRemoteVersion.Size = new System.Drawing.Size(97, 15);
            this.lblRemoteVersion.TabIndex = 4;
            this.lblRemoteVersion.Text = "REALM VERSION";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatusValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(208)))), ((int)(((byte)(124)))));
            this.lblStatusValue.Location = new System.Drawing.Point(793, 56);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(72, 30);
            this.lblStatusValue.TabIndex = 3;
            this.lblStatusValue.Text = "Ready";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(158)))), ((int)(((byte)(139)))));
            this.lblStatus.Location = new System.Drawing.Point(794, 34);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(49, 15);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "STATUS";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(198)))), ((int)(((byte)(178)))));
            this.lblSubtitle.Location = new System.Drawing.Point(24, 68);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(465, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "A polished gateway for patches, realm news, and your next return to Northrend.";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Georgia", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(184)))), ((int)(((byte)(93)))));
            this.lblTitle.Location = new System.Drawing.Point(18, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(509, 41);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "World of Warcraft Launcher";
            // 
            // grpConnection
            // 
            this.grpConnection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpConnection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(27)))), ((int)(((byte)(39)))));
            this.grpConnection.Controls.Add(this.btnClearCache);
            this.grpConnection.Controls.Add(this.btnLaunchGame);
            this.grpConnection.Controls.Add(this.btnUpdateClient);
            this.grpConnection.Controls.Add(this.btnCheckUpdates);
            this.grpConnection.Controls.Add(this.btnBrowseClient);
            this.grpConnection.Controls.Add(this.cmbLanguage);
            this.grpConnection.Controls.Add(this.lblLanguage);
            this.grpConnection.Controls.Add(this.txtClientPath);
            this.grpConnection.Controls.Add(this.lblOnlinePlayerCountValue);
            this.grpConnection.Controls.Add(this.lblOnlinePlayerCount);
            this.grpConnection.Controls.Add(this.lblRealmStatusIndicatorValue);
            this.grpConnection.Controls.Add(this.lblRealmStatusIndicator);
            this.grpConnection.Controls.Add(this.lblClientPath);
            this.grpConnection.Controls.Add(this.lblClientHint);
            this.grpConnection.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpConnection.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(184)))), ((int)(((byte)(93)))));
            this.grpConnection.Location = new System.Drawing.Point(18, 144);
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.Size = new System.Drawing.Size(776, 168);
            this.grpConnection.TabIndex = 1;
            this.grpConnection.TabStop = false;
            this.grpConnection.Text = "Adventure Setup";
            // 
            // btnClearCache
            // 
            this.btnClearCache.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(92)))), ((int)(((byte)(62)))));
            this.btnClearCache.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(92)))), ((int)(((byte)(62)))), ((int)(((byte)(40)))));
            this.btnClearCache.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearCache.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClearCache.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(235)))), ((int)(((byte)(220)))));
            this.btnClearCache.Location = new System.Drawing.Point(476, 119);
            this.btnClearCache.Name = "btnClearCache";
            this.btnClearCache.Size = new System.Drawing.Size(146, 34);
            this.btnClearCache.TabIndex = 8;
            this.btnClearCache.Text = "Clear Cache";
            this.btnClearCache.UseVisualStyleBackColor = false;
            this.btnClearCache.Click += new System.EventHandler(this.btnClearCache_Click);
            // 
            // btnLaunchGame
            // 
            this.btnLaunchGame.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(118)))), ((int)(((byte)(154)))), ((int)(((byte)(82)))));
            this.btnLaunchGame.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.btnLaunchGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchGame.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLaunchGame.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(24)))), ((int)(((byte)(18)))));
            this.btnLaunchGame.Location = new System.Drawing.Point(323, 119);
            this.btnLaunchGame.Name = "btnLaunchGame";
            this.btnLaunchGame.Size = new System.Drawing.Size(146, 34);
            this.btnLaunchGame.TabIndex = 7;
            this.btnLaunchGame.Text = "Enter World";
            this.btnLaunchGame.UseVisualStyleBackColor = false;
            this.btnLaunchGame.Click += new System.EventHandler(this.btnLaunchGame_Click);
            // 
            // btnUpdateClient
            // 
            this.btnUpdateClient.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(162)))), ((int)(((byte)(84)))));
            this.btnUpdateClient.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(126)))), ((int)(((byte)(93)))), ((int)(((byte)(42)))));
            this.btnUpdateClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdateClient.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUpdateClient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(23)))), ((int)(((byte)(18)))));
            this.btnUpdateClient.Location = new System.Drawing.Point(170, 119);
            this.btnUpdateClient.Name = "btnUpdateClient";
            this.btnUpdateClient.Size = new System.Drawing.Size(146, 34);
            this.btnUpdateClient.TabIndex = 6;
            this.btnUpdateClient.Text = "Update Client";
            this.btnUpdateClient.UseVisualStyleBackColor = false;
            this.btnUpdateClient.Click += new System.EventHandler(this.btnUpdateClient_Click);
            // 
            // btnCheckUpdates
            // 
            this.btnCheckUpdates.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(162)))), ((int)(((byte)(84)))));
            this.btnCheckUpdates.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(126)))), ((int)(((byte)(93)))), ((int)(((byte)(42)))));
            this.btnCheckUpdates.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheckUpdates.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCheckUpdates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(23)))), ((int)(((byte)(18)))));
            this.btnCheckUpdates.Location = new System.Drawing.Point(17, 119);
            this.btnCheckUpdates.Name = "btnCheckUpdates";
            this.btnCheckUpdates.Size = new System.Drawing.Size(146, 34);
            this.btnCheckUpdates.TabIndex = 5;
            this.btnCheckUpdates.Text = "Check Updates";
            this.btnCheckUpdates.UseVisualStyleBackColor = false;
            this.btnCheckUpdates.Click += new System.EventHandler(this.btnCheckUpdates_Click);
            // 
            // btnBrowseClient
            // 
            this.btnBrowseClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseClient.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(162)))), ((int)(((byte)(84)))));
            this.btnBrowseClient.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(126)))), ((int)(((byte)(93)))), ((int)(((byte)(42)))));
            this.btnBrowseClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseClient.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseClient.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(23)))), ((int)(((byte)(18)))));
            this.btnBrowseClient.Location = new System.Drawing.Point(657, 82);
            this.btnBrowseClient.Name = "btnBrowseClient";
            this.btnBrowseClient.Size = new System.Drawing.Size(101, 28);
            this.btnBrowseClient.TabIndex = 4;
            this.btnBrowseClient.Text = "Browse...";
            this.btnBrowseClient.UseVisualStyleBackColor = false;
            this.btnBrowseClient.Click += new System.EventHandler(this.btnBrowseClient_Click);
            // 
            // cmbLanguage
            // 
            this.cmbLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbLanguage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(20)))), ((int)(((byte)(31)))));
            this.cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLanguage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbLanguage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbLanguage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(225)))), ((int)(((byte)(214)))));
            this.cmbLanguage.FormattingEnabled = true;
            this.cmbLanguage.Location = new System.Drawing.Point(509, 85);
            this.cmbLanguage.Name = "cmbLanguage";
            this.cmbLanguage.Size = new System.Drawing.Size(142, 23);
            this.cmbLanguage.TabIndex = 13;
            this.cmbLanguage.SelectedIndexChanged += new System.EventHandler(this.cmbLanguage_SelectedIndexChanged);
            // 
            // lblLanguage
            // 
            this.lblLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLanguage.AutoSize = true;
            this.lblLanguage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLanguage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(202)))), ((int)(((byte)(192)))));
            this.lblLanguage.Location = new System.Drawing.Point(506, 66);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(59, 15);
            this.lblLanguage.TabIndex = 14;
            this.lblLanguage.Text = "Language";
            // 
            // txtClientPath
            // 
            this.txtClientPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtClientPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(20)))), ((int)(((byte)(31)))));
            this.txtClientPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtClientPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtClientPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(225)))), ((int)(((byte)(214)))));
            this.txtClientPath.Location = new System.Drawing.Point(17, 86);
            this.txtClientPath.Name = "txtClientPath";
            this.txtClientPath.Size = new System.Drawing.Size(486, 23);
            this.txtClientPath.TabIndex = 3;
            this.txtClientPath.TextChanged += new System.EventHandler(this.txtClientPath_TextChanged);
            // 
            // lblOnlinePlayerCountValue
            // 
            this.lblOnlinePlayerCountValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOnlinePlayerCountValue.AutoSize = true;
            this.lblOnlinePlayerCountValue.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnlinePlayerCountValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(203)))), ((int)(((byte)(110)))));
            this.lblOnlinePlayerCountValue.Location = new System.Drawing.Point(630, 50);
            this.lblOnlinePlayerCountValue.Name = "lblOnlinePlayerCountValue";
            this.lblOnlinePlayerCountValue.Size = new System.Drawing.Size(14, 20);
            this.lblOnlinePlayerCountValue.TabIndex = 12;
            this.lblOnlinePlayerCountValue.Text = "-";
            // 
            // lblOnlinePlayerCount
            // 
            this.lblOnlinePlayerCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblOnlinePlayerCount.AutoSize = true;
            this.lblOnlinePlayerCount.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnlinePlayerCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(158)))), ((int)(((byte)(139)))));
            this.lblOnlinePlayerCount.Location = new System.Drawing.Point(631, 29);
            this.lblOnlinePlayerCount.Name = "lblOnlinePlayerCount";
            this.lblOnlinePlayerCount.Size = new System.Drawing.Size(100, 15);
            this.lblOnlinePlayerCount.TabIndex = 11;
            this.lblOnlinePlayerCount.Text = "ONLINE PLAYERS";
            // 
            // lblRealmStatusIndicatorValue
            // 
            this.lblRealmStatusIndicatorValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRealmStatusIndicatorValue.AutoSize = true;
            this.lblRealmStatusIndicatorValue.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRealmStatusIndicatorValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(203)))), ((int)(((byte)(110)))));
            this.lblRealmStatusIndicatorValue.Location = new System.Drawing.Point(493, 50);
            this.lblRealmStatusIndicatorValue.Name = "lblRealmStatusIndicatorValue";
            this.lblRealmStatusIndicatorValue.Size = new System.Drawing.Size(73, 20);
            this.lblRealmStatusIndicatorValue.TabIndex = 10;
            this.lblRealmStatusIndicatorValue.Text = "Checking";
            // 
            // lblRealmStatusIndicator
            // 
            this.lblRealmStatusIndicator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRealmStatusIndicator.AutoSize = true;
            this.lblRealmStatusIndicator.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRealmStatusIndicator.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(158)))), ((int)(((byte)(139)))));
            this.lblRealmStatusIndicator.Location = new System.Drawing.Point(494, 29);
            this.lblRealmStatusIndicator.Name = "lblRealmStatusIndicator";
            this.lblRealmStatusIndicator.Size = new System.Drawing.Size(90, 15);
            this.lblRealmStatusIndicator.TabIndex = 9;
            this.lblRealmStatusIndicator.Text = "REALM STATUS";
            // 
            // lblClientPath
            // 
            this.lblClientPath.AutoSize = true;
            this.lblClientPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClientPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(202)))), ((int)(((byte)(192)))));
            this.lblClientPath.Location = new System.Drawing.Point(14, 66);
            this.lblClientPath.Name = "lblClientPath";
            this.lblClientPath.Size = new System.Drawing.Size(112, 15);
            this.lblClientPath.TabIndex = 2;
            this.lblClientPath.Text = "World of Warcraft path";
            // 
            // lblClientHint
            // 
            this.lblClientHint.AutoSize = true;
            this.lblClientHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClientHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(158)))), ((int)(((byte)(172)))));
            this.lblClientHint.Location = new System.Drawing.Point(14, 29);
            this.lblClientHint.Name = "lblClientHint";
            this.lblClientHint.Size = new System.Drawing.Size(422, 15);
            this.lblClientHint.TabIndex = 1;
            this.lblClientHint.Text = "Choose your 3.3.5a client folder, check the latest patch state, then enter the re" +
    "alm.";
            // 
            // grpNews
            // 
            this.grpNews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right))));
            this.grpNews.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(27)))), ((int)(((byte)(39)))));
            this.grpNews.Controls.Add(this.txtNews);
            this.grpNews.Controls.Add(this.lblNewsHint);
            this.grpNews.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpNews.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(184)))), ((int)(((byte)(93)))));
            this.grpNews.Location = new System.Drawing.Point(810, 144);
            this.grpNews.Name = "grpNews";
            this.grpNews.Size = new System.Drawing.Size(396, 570);
            this.grpNews.TabIndex = 2;
            this.grpNews.TabStop = false;
            this.grpNews.Text = "Realm News";
            // 
            // txtNews
            // 
            this.txtNews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNews.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(20)))), ((int)(((byte)(31)))));
            this.txtNews.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNews.Font = new System.Drawing.Font("Segoe UI", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNews.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(226)))), ((int)(((byte)(213)))));
            this.txtNews.Location = new System.Drawing.Point(17, 58);
            this.txtNews.Name = "txtNews";
            this.txtNews.ReadOnly = true;
            this.txtNews.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtNews.Size = new System.Drawing.Size(362, 492);
            this.txtNews.TabIndex = 0;
            this.txtNews.Text = "";
            // 
            // lblNewsHint
            // 
            this.lblNewsHint.AutoSize = true;
            this.lblNewsHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNewsHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(158)))), ((int)(((byte)(172)))));
            this.lblNewsHint.Location = new System.Drawing.Point(14, 29);
            this.lblNewsHint.Name = "lblNewsHint";
            this.lblNewsHint.Size = new System.Drawing.Size(299, 15);
            this.lblNewsHint.TabIndex = 1;
            this.lblNewsHint.Text = "Breaking news and recent updates pulled from the realm feed.";
            // 
            // grpUpdates
            // 
            this.grpUpdates.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpUpdates.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(27)))), ((int)(((byte)(39)))));
            this.grpUpdates.Controls.Add(this.txtLog);
            this.grpUpdates.Controls.Add(this.lblLog);
            this.grpUpdates.Controls.Add(this.lstFilesToUpdate);
            this.grpUpdates.Controls.Add(this.lblFilesToUpdate);
            this.grpUpdates.Controls.Add(this.lblUpdatesHint);
            this.grpUpdates.Controls.Add(this.progressUpdate);
            this.grpUpdates.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpUpdates.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(184)))), ((int)(((byte)(93)))));
            this.grpUpdates.Location = new System.Drawing.Point(18, 330);
            this.grpUpdates.Name = "grpUpdates";
            this.grpUpdates.Size = new System.Drawing.Size(776, 384);
            this.grpUpdates.TabIndex = 3;
            this.grpUpdates.TabStop = false;
            this.grpUpdates.Text = "Battlefield Report";
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(20)))), ((int)(((byte)(31)))));
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            this.txtLog.Location = new System.Drawing.Point(392, 116);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(366, 250);
            this.txtLog.TabIndex = 8;
            // 
            // lblLog
            // 
            this.lblLog.AutoSize = true;
            this.lblLog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(202)))), ((int)(((byte)(192)))));
            this.lblLog.Location = new System.Drawing.Point(389, 96);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(66, 15);
            this.lblLog.TabIndex = 7;
            this.lblLog.Text = "Activity Log";
            // 
            // lstFilesToUpdate
            // 
            this.lstFilesToUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstFilesToUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(20)))), ((int)(((byte)(31)))));
            this.lstFilesToUpdate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstFilesToUpdate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstFilesToUpdate.ForeColor = System.Drawing.Color.Gainsboro;
            this.lstFilesToUpdate.FormattingEnabled = true;
            this.lstFilesToUpdate.HorizontalScrollbar = true;
            this.lstFilesToUpdate.ItemHeight = 15;
            this.lstFilesToUpdate.Location = new System.Drawing.Point(17, 116);
            this.lstFilesToUpdate.Name = "lstFilesToUpdate";
            this.lstFilesToUpdate.Size = new System.Drawing.Size(356, 250);
            this.lstFilesToUpdate.TabIndex = 6;
            // 
            // lblFilesToUpdate
            // 
            this.lblFilesToUpdate.AutoSize = true;
            this.lblFilesToUpdate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilesToUpdate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(202)))), ((int)(((byte)(192)))));
            this.lblFilesToUpdate.Location = new System.Drawing.Point(14, 96);
            this.lblFilesToUpdate.Name = "lblFilesToUpdate";
            this.lblFilesToUpdate.Size = new System.Drawing.Size(93, 15);
            this.lblFilesToUpdate.TabIndex = 5;
            this.lblFilesToUpdate.Text = "Pending Updates";
            // 
            // lblUpdatesHint
            // 
            this.lblUpdatesHint.AutoSize = true;
            this.lblUpdatesHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUpdatesHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(158)))), ((int)(((byte)(172)))));
            this.lblUpdatesHint.Location = new System.Drawing.Point(14, 29);
            this.lblUpdatesHint.Name = "lblUpdatesHint";
            this.lblUpdatesHint.Size = new System.Drawing.Size(322, 15);
            this.lblUpdatesHint.TabIndex = 1;
            this.lblUpdatesHint.Text = "Track download progress, required files, and live launcher activity.";
            // 
            // progressUpdate
            // 
            this.progressUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressUpdate.Location = new System.Drawing.Point(17, 59);
            this.progressUpdate.Name = "progressUpdate";
            this.progressUpdate.Size = new System.Drawing.Size(741, 20);
            this.progressUpdate.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(13)))), ((int)(((byte)(21)))));
            this.ClientSize = new System.Drawing.Size(1224, 734);
            this.Controls.Add(this.grpUpdates);
            this.Controls.Add(this.grpNews);
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Gainsboro;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1240, 773);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "World of Warcraft Launcher";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.grpNews.ResumeLayout(false);
            this.grpNews.PerformLayout();
            this.grpUpdates.ResumeLayout(false);
            this.grpUpdates.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnBrowseClient;
        private System.Windows.Forms.Button btnCheckUpdates;
        private System.Windows.Forms.Button btnClearCache;
        private System.Windows.Forms.Button btnLaunchGame;
        private System.Windows.Forms.Button btnUpdateClient;
        private System.Windows.Forms.ComboBox cmbLanguage;
        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.GroupBox grpNews;
        private System.Windows.Forms.GroupBox grpUpdates;
        private System.Windows.Forms.Label lblClientPath;
        private System.Windows.Forms.Label lblClientHint;
        private System.Windows.Forms.Label lblFilesToUpdate;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.Label lblNewsHint;
        private System.Windows.Forms.Label lblOnlinePlayerCount;
        private System.Windows.Forms.Label lblOnlinePlayerCountValue;
        private System.Windows.Forms.Label lblRealmStatusIndicator;
        private System.Windows.Forms.Label lblRealmStatusIndicatorValue;
        private System.Windows.Forms.Label lblRemoteVersion;
        private System.Windows.Forms.Label lblRemoteVersionValue;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblStatusValue;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblUpdatesHint;
        private System.Windows.Forms.ListBox lstFilesToUpdate;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlHeaderDivider;
        private System.Windows.Forms.ProgressBar progressUpdate;
        private System.Windows.Forms.TextBox txtClientPath;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.RichTextBox txtNews;
    }
}

