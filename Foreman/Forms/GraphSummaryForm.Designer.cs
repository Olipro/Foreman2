﻿
namespace Foreman
{
	partial class GraphSummaryForm
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
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.BuildingsTabPage = new System.Windows.Forms.TabPage();
			this.BuildingsTable = new System.Windows.Forms.TableLayoutPanel();
			this.label4 = new System.Windows.Forms.Label();
			this.BuildingsTabControl = new System.Windows.Forms.TabControl();
			this.AssemblersPage = new System.Windows.Forms.TabPage();
			this.AssemblerListView = new System.Windows.Forms.ListView();
			this.AssemblersHeaderCounter = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.AssemblersHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.AssemblersHeaderPower = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.AssemblersHeaderPowerB = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.IconList = new System.Windows.Forms.ImageList(this.components);
			this.MinersPage = new System.Windows.Forms.TabPage();
			this.MinerListView = new System.Windows.Forms.ListView();
			this.MinersHeaderCounter = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MinerHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MinersHeaderPower = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MinersHeaderPowerB = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PowersPage = new System.Windows.Forms.TabPage();
			this.PowerListView = new System.Windows.Forms.ListView();
			this.PowerHeaderCounter = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PowerHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PowerHeaderPower = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PowerHeaderPowerB = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.BeaconsPage = new System.Windows.Forms.TabPage();
			this.BeaconListView = new System.Windows.Forms.ListView();
			this.BeaconsHeaderCounter = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.BeaconsHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.BeaconsHeaderPower = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.PowerConsumptionLabel = new System.Windows.Forms.Label();
			this.PowerProductionLabel = new System.Windows.Forms.Label();
			this.BuildingsExportButton = new System.Windows.Forms.Button();
			this.ItemsTabPage = new System.Windows.Forms.TabPage();
			this.ItemsTable = new System.Windows.Forms.TableLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.ItemsTabControl = new System.Windows.Forms.TabControl();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.ItemsListView = new System.Windows.Forms.ListView();
			this.ItemsHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderIn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderInUL = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderOut = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderOutUL = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderOverproduced = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderProduced = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsHeaderConsumed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tabPage5 = new System.Windows.Forms.TabPage();
			this.FluidsListView = new System.Windows.Forms.ListView();
			this.FluidsHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderIn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderInUL = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderOut = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderOutUL = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderOverproduced = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderProduced = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FluidsHeaderConsumed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ItemsFilterTextBox = new System.Windows.Forms.TextBox();
			this.ItemsExportButton = new System.Windows.Forms.Button();
			this.ItemsFilterCBTable = new System.Windows.Forms.TableLayoutPanel();
			this.ItemFilterConsumptionCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterOutputOverproducedCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterOutputUnlinkedCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterOutputCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterInputUnlinkedCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterInputCheckBox = new System.Windows.Forms.CheckBox();
			this.ItemFilterProductionCheckBox = new System.Windows.Forms.CheckBox();
			this.KeyNodesTabPage = new System.Windows.Forms.TabPage();
			this.KeyNodesTable = new System.Windows.Forms.TableLayoutPanel();
			this.KeyNodesListView = new System.Windows.Forms.ListView();
			this.KeyNodesHeaderType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.KeyNodesHeaderDetails = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.KeyNodesHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.KeyNodesHeaderFlow = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.KeyNodesHeaderBuildings = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.label2 = new System.Windows.Forms.Label();
			this.KeyNodesFilterTextBox = new System.Windows.Forms.TextBox();
			this.keyNodesExportButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.PassthroughNodeFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.ConsumerNodeFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.SupplierNodeFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.RecipeNodeFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.BuildingCountLabel = new System.Windows.Forms.Label();
			this.BeaconCountLabel = new System.Windows.Forms.Label();
			this.BuildingsFilterTextBox = new System.Windows.Forms.TextBox();
			this.MainTabControl.SuspendLayout();
			this.BuildingsTabPage.SuspendLayout();
			this.BuildingsTable.SuspendLayout();
			this.BuildingsTabControl.SuspendLayout();
			this.AssemblersPage.SuspendLayout();
			this.MinersPage.SuspendLayout();
			this.PowersPage.SuspendLayout();
			this.BeaconsPage.SuspendLayout();
			this.ItemsTabPage.SuspendLayout();
			this.ItemsTable.SuspendLayout();
			this.ItemsTabControl.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage5.SuspendLayout();
			this.ItemsFilterCBTable.SuspendLayout();
			this.KeyNodesTabPage.SuspendLayout();
			this.KeyNodesTable.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.BuildingsTabPage);
			this.MainTabControl.Controls.Add(this.ItemsTabPage);
			this.MainTabControl.Controls.Add(this.KeyNodesTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
			this.MainTabControl.Location = new System.Drawing.Point(3, 3);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.Padding = new System.Drawing.Point(24, 3);
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(758, 495);
			this.MainTabControl.TabIndex = 0;
			// 
			// BuildingsTabPage
			// 
			this.BuildingsTabPage.Controls.Add(this.BuildingsTable);
			this.BuildingsTabPage.Location = new System.Drawing.Point(4, 29);
			this.BuildingsTabPage.Name = "BuildingsTabPage";
			this.BuildingsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.BuildingsTabPage.Size = new System.Drawing.Size(750, 462);
			this.BuildingsTabPage.TabIndex = 0;
			this.BuildingsTabPage.Text = "Buildings";
			this.BuildingsTabPage.UseVisualStyleBackColor = true;
			// 
			// BuildingsTable
			// 
			this.BuildingsTable.AutoSize = true;
			this.BuildingsTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.BuildingsTable.ColumnCount = 3;
			this.BuildingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.BuildingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.BuildingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 141F));
			this.BuildingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.BuildingsTable.Controls.Add(this.BuildingsFilterTextBox, 1, 1);
			this.BuildingsTable.Controls.Add(this.label4, 0, 1);
			this.BuildingsTable.Controls.Add(this.BuildingsTabControl, 0, 2);
			this.BuildingsTable.Controls.Add(this.BuildingsExportButton, 2, 1);
			this.BuildingsTable.Controls.Add(this.tableLayoutPanel2, 1, 0);
			this.BuildingsTable.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BuildingsTable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.BuildingsTable.Location = new System.Drawing.Point(3, 3);
			this.BuildingsTable.Name = "BuildingsTable";
			this.BuildingsTable.RowCount = 3;
			this.BuildingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.BuildingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.BuildingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.BuildingsTable.Size = new System.Drawing.Size(744, 456);
			this.BuildingsTable.TabIndex = 1;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label4.Location = new System.Drawing.Point(7, 31);
			this.label4.Margin = new System.Windows.Forms.Padding(7, 0, 2, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(32, 24);
			this.label4.TabIndex = 30;
			this.label4.Text = "Filter:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// BuildingsTabControl
			// 
			this.BuildingsTable.SetColumnSpan(this.BuildingsTabControl, 3);
			this.BuildingsTabControl.Controls.Add(this.AssemblersPage);
			this.BuildingsTabControl.Controls.Add(this.MinersPage);
			this.BuildingsTabControl.Controls.Add(this.PowersPage);
			this.BuildingsTabControl.Controls.Add(this.BeaconsPage);
			this.BuildingsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BuildingsTabControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
			this.BuildingsTabControl.Location = new System.Drawing.Point(4, 59);
			this.BuildingsTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.BuildingsTabControl.Multiline = true;
			this.BuildingsTabControl.Name = "BuildingsTabControl";
			this.BuildingsTabControl.Padding = new System.Drawing.Point(12, 3);
			this.BuildingsTabControl.SelectedIndex = 0;
			this.BuildingsTabControl.Size = new System.Drawing.Size(736, 393);
			this.BuildingsTabControl.TabIndex = 27;
			// 
			// AssemblersPage
			// 
			this.AssemblersPage.Controls.Add(this.AssemblerListView);
			this.AssemblersPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
			this.AssemblersPage.Location = new System.Drawing.Point(4, 24);
			this.AssemblersPage.Margin = new System.Windows.Forms.Padding(2);
			this.AssemblersPage.Name = "AssemblersPage";
			this.AssemblersPage.Size = new System.Drawing.Size(728, 365);
			this.AssemblersPage.TabIndex = 0;
			this.AssemblersPage.Text = "Assemblers";
			this.AssemblersPage.UseVisualStyleBackColor = true;
			// 
			// AssemblerListView
			// 
			this.AssemblerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.AssemblersHeaderCounter,
            this.AssemblersHeaderName,
            this.AssemblersHeaderPower,
            this.AssemblersHeaderPowerB});
			this.AssemblerListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AssemblerListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.AssemblerListView.FullRowSelect = true;
			this.AssemblerListView.GridLines = true;
			this.AssemblerListView.HideSelection = false;
			this.AssemblerListView.LabelWrap = false;
			this.AssemblerListView.Location = new System.Drawing.Point(0, 0);
			this.AssemblerListView.MultiSelect = false;
			this.AssemblerListView.Name = "AssemblerListView";
			this.AssemblerListView.Size = new System.Drawing.Size(728, 365);
			this.AssemblerListView.SmallImageList = this.IconList;
			this.AssemblerListView.TabIndex = 17;
			this.AssemblerListView.UseCompatibleStateImageBehavior = false;
			this.AssemblerListView.View = System.Windows.Forms.View.Details;
			this.AssemblerListView.VirtualMode = true;
			this.AssemblerListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.AssemblerListView_ColumnClick);
			this.AssemblerListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.AssemblerListView_RetrieveVirtualItem);
			// 
			// AssemblersHeaderCounter
			// 
			this.AssemblersHeaderCounter.Text = "#";
			this.AssemblersHeaderCounter.Width = 100;
			// 
			// AssemblersHeaderName
			// 
			this.AssemblersHeaderName.Text = "Name";
			this.AssemblersHeaderName.Width = 250;
			// 
			// AssemblersHeaderPower
			// 
			this.AssemblersHeaderPower.Text = "Power (Assembler)";
			this.AssemblersHeaderPower.Width = 100;
			// 
			// AssemblersHeaderPowerB
			// 
			this.AssemblersHeaderPowerB.Text = "Power (Beacons)";
			this.AssemblersHeaderPowerB.Width = 100;
			// 
			// IconList
			// 
			this.IconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.IconList.ImageSize = new System.Drawing.Size(24, 24);
			this.IconList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// MinersPage
			// 
			this.MinersPage.Controls.Add(this.MinerListView);
			this.MinersPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
			this.MinersPage.Location = new System.Drawing.Point(4, 24);
			this.MinersPage.Margin = new System.Windows.Forms.Padding(2);
			this.MinersPage.Name = "MinersPage";
			this.MinersPage.Size = new System.Drawing.Size(728, 369);
			this.MinersPage.TabIndex = 2;
			this.MinersPage.Text = "Miners";
			this.MinersPage.UseVisualStyleBackColor = true;
			// 
			// MinerListView
			// 
			this.MinerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.MinersHeaderCounter,
            this.MinerHeaderName,
            this.MinersHeaderPower,
            this.MinersHeaderPowerB});
			this.MinerListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MinerListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.MinerListView.FullRowSelect = true;
			this.MinerListView.GridLines = true;
			this.MinerListView.HideSelection = false;
			this.MinerListView.LabelWrap = false;
			this.MinerListView.Location = new System.Drawing.Point(0, 0);
			this.MinerListView.MultiSelect = false;
			this.MinerListView.Name = "MinerListView";
			this.MinerListView.Size = new System.Drawing.Size(728, 369);
			this.MinerListView.SmallImageList = this.IconList;
			this.MinerListView.TabIndex = 17;
			this.MinerListView.UseCompatibleStateImageBehavior = false;
			this.MinerListView.View = System.Windows.Forms.View.Details;
			this.MinerListView.VirtualMode = true;
			this.MinerListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.MinerListView_ColumnClick);
			this.MinerListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.MinerListView_RetrieveVirtualItem);
			// 
			// MinersHeaderCounter
			// 
			this.MinersHeaderCounter.Text = "#";
			this.MinersHeaderCounter.Width = 100;
			// 
			// MinerHeaderName
			// 
			this.MinerHeaderName.Text = "Name";
			this.MinerHeaderName.Width = 250;
			// 
			// MinersHeaderPower
			// 
			this.MinersHeaderPower.Text = "Power (Extractor)";
			this.MinersHeaderPower.Width = 100;
			// 
			// MinersHeaderPowerB
			// 
			this.MinersHeaderPowerB.Text = "Power (Beacons)";
			this.MinersHeaderPowerB.Width = 100;
			// 
			// PowersPage
			// 
			this.PowersPage.Controls.Add(this.PowerListView);
			this.PowersPage.Location = new System.Drawing.Point(4, 24);
			this.PowersPage.Name = "PowersPage";
			this.PowersPage.Size = new System.Drawing.Size(728, 369);
			this.PowersPage.TabIndex = 5;
			this.PowersPage.Text = "Power";
			this.PowersPage.UseVisualStyleBackColor = true;
			// 
			// PowerListView
			// 
			this.PowerListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.PowerHeaderCounter,
            this.PowerHeaderName,
            this.PowerHeaderPower,
            this.PowerHeaderPowerB});
			this.PowerListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PowerListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.PowerListView.FullRowSelect = true;
			this.PowerListView.GridLines = true;
			this.PowerListView.HideSelection = false;
			this.PowerListView.LabelWrap = false;
			this.PowerListView.Location = new System.Drawing.Point(0, 0);
			this.PowerListView.MultiSelect = false;
			this.PowerListView.Name = "PowerListView";
			this.PowerListView.Size = new System.Drawing.Size(728, 369);
			this.PowerListView.SmallImageList = this.IconList;
			this.PowerListView.TabIndex = 18;
			this.PowerListView.UseCompatibleStateImageBehavior = false;
			this.PowerListView.View = System.Windows.Forms.View.Details;
			this.PowerListView.VirtualMode = true;
			this.PowerListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.PowerListView_ColumnClick);
			this.PowerListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.PowerListView_RetrieveVirtualItem);
			// 
			// PowerHeaderCounter
			// 
			this.PowerHeaderCounter.Text = "#";
			this.PowerHeaderCounter.Width = 100;
			// 
			// PowerHeaderName
			// 
			this.PowerHeaderName.Text = "Name";
			this.PowerHeaderName.Width = 250;
			// 
			// PowerHeaderPower
			// 
			this.PowerHeaderPower.Text = "Power Generated";
			this.PowerHeaderPower.Width = 100;
			// 
			// PowerHeaderPowerB
			// 
			this.PowerHeaderPowerB.Text = "Power Consumed";
			this.PowerHeaderPowerB.Width = 100;
			// 
			// BeaconsPage
			// 
			this.BeaconsPage.Controls.Add(this.BeaconListView);
			this.BeaconsPage.Location = new System.Drawing.Point(4, 24);
			this.BeaconsPage.Name = "BeaconsPage";
			this.BeaconsPage.Size = new System.Drawing.Size(728, 369);
			this.BeaconsPage.TabIndex = 6;
			this.BeaconsPage.Text = "Beacons";
			this.BeaconsPage.UseVisualStyleBackColor = true;
			// 
			// BeaconListView
			// 
			this.BeaconListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.BeaconsHeaderCounter,
            this.BeaconsHeaderName,
            this.BeaconsHeaderPower});
			this.BeaconListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BeaconListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.BeaconListView.FullRowSelect = true;
			this.BeaconListView.GridLines = true;
			this.BeaconListView.HideSelection = false;
			this.BeaconListView.LabelWrap = false;
			this.BeaconListView.Location = new System.Drawing.Point(0, 0);
			this.BeaconListView.MultiSelect = false;
			this.BeaconListView.Name = "BeaconListView";
			this.BeaconListView.Size = new System.Drawing.Size(728, 369);
			this.BeaconListView.SmallImageList = this.IconList;
			this.BeaconListView.TabIndex = 19;
			this.BeaconListView.UseCompatibleStateImageBehavior = false;
			this.BeaconListView.View = System.Windows.Forms.View.Details;
			this.BeaconListView.VirtualMode = true;
			this.BeaconListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.BeaconListView_ColumnClick);
			this.BeaconListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.BeaconListView_RetrieveVirtualItem);
			// 
			// BeaconsHeaderCounter
			// 
			this.BeaconsHeaderCounter.Text = "#";
			this.BeaconsHeaderCounter.Width = 100;
			// 
			// BeaconsHeaderName
			// 
			this.BeaconsHeaderName.Text = "Name";
			this.BeaconsHeaderName.Width = 250;
			// 
			// BeaconsHeaderPower
			// 
			this.BeaconsHeaderPower.Text = "Power (Beacon)";
			this.BeaconsHeaderPower.Width = 100;
			// 
			// PowerConsumptionLabel
			// 
			this.PowerConsumptionLabel.AutoSize = true;
			this.PowerConsumptionLabel.Location = new System.Drawing.Point(190, 5);
			this.PowerConsumptionLabel.Margin = new System.Windows.Forms.Padding(7, 5, 15, 5);
			this.PowerConsumptionLabel.Name = "PowerConsumptionLabel";
			this.PowerConsumptionLabel.Size = new System.Drawing.Size(124, 15);
			this.PowerConsumptionLabel.TabIndex = 31;
			this.PowerConsumptionLabel.Text = "Power Consumption: ";
			// 
			// PowerProductionLabel
			// 
			this.PowerProductionLabel.AutoSize = true;
			this.PowerProductionLabel.Location = new System.Drawing.Point(336, 5);
			this.PowerProductionLabel.Margin = new System.Windows.Forms.Padding(7, 5, 15, 5);
			this.PowerProductionLabel.Name = "PowerProductionLabel";
			this.PowerProductionLabel.Size = new System.Drawing.Size(110, 15);
			this.PowerProductionLabel.TabIndex = 32;
			this.PowerProductionLabel.Text = "Power Production: ";
			// 
			// BuildingsExportButton
			// 
			this.BuildingsExportButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BuildingsExportButton.Location = new System.Drawing.Point(603, 31);
			this.BuildingsExportButton.Margin = new System.Windows.Forms.Padding(0, 0, 7, 0);
			this.BuildingsExportButton.Name = "BuildingsExportButton";
			this.BuildingsExportButton.Size = new System.Drawing.Size(134, 24);
			this.BuildingsExportButton.TabIndex = 33;
			this.BuildingsExportButton.Text = "Export CSV";
			this.BuildingsExportButton.UseVisualStyleBackColor = true;
			this.BuildingsExportButton.Click += new System.EventHandler(this.BuildingsExportButton_Click);
			// 
			// ItemsTabPage
			// 
			this.ItemsTabPage.Controls.Add(this.ItemsTable);
			this.ItemsTabPage.Location = new System.Drawing.Point(4, 29);
			this.ItemsTabPage.Name = "ItemsTabPage";
			this.ItemsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.ItemsTabPage.Size = new System.Drawing.Size(750, 462);
			this.ItemsTabPage.TabIndex = 1;
			this.ItemsTabPage.Text = "Items/Fluids";
			this.ItemsTabPage.UseVisualStyleBackColor = true;
			// 
			// ItemsTable
			// 
			this.ItemsTable.AutoSize = true;
			this.ItemsTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ItemsTable.ColumnCount = 3;
			this.ItemsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ItemsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
			this.ItemsTable.Controls.Add(this.label1, 0, 0);
			this.ItemsTable.Controls.Add(this.ItemsTabControl, 0, 2);
			this.ItemsTable.Controls.Add(this.ItemsFilterTextBox, 1, 0);
			this.ItemsTable.Controls.Add(this.ItemsExportButton, 2, 0);
			this.ItemsTable.Controls.Add(this.ItemsFilterCBTable, 0, 1);
			this.ItemsTable.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsTable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.ItemsTable.Location = new System.Drawing.Point(3, 3);
			this.ItemsTable.Name = "ItemsTable";
			this.ItemsTable.RowCount = 3;
			this.ItemsTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ItemsTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ItemsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ItemsTable.Size = new System.Drawing.Size(744, 456);
			this.ItemsTable.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label1.Location = new System.Drawing.Point(7, 0);
			this.label1.Margin = new System.Windows.Forms.Padding(7, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 24);
			this.label1.TabIndex = 30;
			this.label1.Text = "Filter:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// ItemsTabControl
			// 
			this.ItemsTable.SetColumnSpan(this.ItemsTabControl, 3);
			this.ItemsTabControl.Controls.Add(this.tabPage4);
			this.ItemsTabControl.Controls.Add(this.tabPage5);
			this.ItemsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsTabControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
			this.ItemsTabControl.Location = new System.Drawing.Point(4, 57);
			this.ItemsTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.ItemsTabControl.Multiline = true;
			this.ItemsTabControl.Name = "ItemsTabControl";
			this.ItemsTabControl.Padding = new System.Drawing.Point(12, 3);
			this.ItemsTabControl.SelectedIndex = 0;
			this.ItemsTabControl.Size = new System.Drawing.Size(736, 395);
			this.ItemsTabControl.TabIndex = 27;
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add(this.ItemsListView);
			this.tabPage4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
			this.tabPage4.Location = new System.Drawing.Point(4, 24);
			this.tabPage4.Margin = new System.Windows.Forms.Padding(2);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Size = new System.Drawing.Size(728, 367);
			this.tabPage4.TabIndex = 0;
			this.tabPage4.Text = "Items";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// ItemsListView
			// 
			this.ItemsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ItemsHeaderName,
            this.ItemsHeaderIn,
            this.ItemsHeaderInUL,
            this.ItemsHeaderOut,
            this.ItemsHeaderOutUL,
            this.ItemsHeaderOverproduced,
            this.ItemsHeaderProduced,
            this.ItemsHeaderConsumed});
			this.ItemsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.ItemsListView.FullRowSelect = true;
			this.ItemsListView.GridLines = true;
			this.ItemsListView.HideSelection = false;
			this.ItemsListView.LabelWrap = false;
			this.ItemsListView.Location = new System.Drawing.Point(0, 0);
			this.ItemsListView.MultiSelect = false;
			this.ItemsListView.Name = "ItemsListView";
			this.ItemsListView.Size = new System.Drawing.Size(728, 367);
			this.ItemsListView.SmallImageList = this.IconList;
			this.ItemsListView.TabIndex = 17;
			this.ItemsListView.UseCompatibleStateImageBehavior = false;
			this.ItemsListView.View = System.Windows.Forms.View.Details;
			this.ItemsListView.VirtualMode = true;
			this.ItemsListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ItemsListView_ColumnClick);
			this.ItemsListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.ItemsListView_RetrieveVirtualItem);
			// 
			// ItemsHeaderName
			// 
			this.ItemsHeaderName.Text = "Item";
			this.ItemsHeaderName.Width = 150;
			// 
			// ItemsHeaderIn
			// 
			this.ItemsHeaderIn.Text = "In";
			this.ItemsHeaderIn.Width = 75;
			// 
			// ItemsHeaderInUL
			// 
			this.ItemsHeaderInUL.Text = "In (x link)";
			this.ItemsHeaderInUL.Width = 75;
			// 
			// ItemsHeaderOut
			// 
			this.ItemsHeaderOut.Text = "Out";
			this.ItemsHeaderOut.Width = 75;
			// 
			// ItemsHeaderOutUL
			// 
			this.ItemsHeaderOutUL.Text = "Out (x link)";
			this.ItemsHeaderOutUL.Width = 75;
			// 
			// ItemsHeaderOverproduced
			// 
			this.ItemsHeaderOverproduced.Text = "Overprod.";
			this.ItemsHeaderOverproduced.Width = 75;
			// 
			// ItemsHeaderProduced
			// 
			this.ItemsHeaderProduced.Text = "Produced";
			this.ItemsHeaderProduced.Width = 75;
			// 
			// ItemsHeaderConsumed
			// 
			this.ItemsHeaderConsumed.Text = "Consumed";
			this.ItemsHeaderConsumed.Width = 75;
			// 
			// tabPage5
			// 
			this.tabPage5.Controls.Add(this.FluidsListView);
			this.tabPage5.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
			this.tabPage5.Location = new System.Drawing.Point(4, 24);
			this.tabPage5.Margin = new System.Windows.Forms.Padding(2);
			this.tabPage5.Name = "tabPage5";
			this.tabPage5.Size = new System.Drawing.Size(728, 367);
			this.tabPage5.TabIndex = 2;
			this.tabPage5.Text = "Fluids";
			this.tabPage5.UseVisualStyleBackColor = true;
			// 
			// FluidsListView
			// 
			this.FluidsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FluidsHeaderName,
            this.FluidsHeaderIn,
            this.FluidsHeaderInUL,
            this.FluidsHeaderOut,
            this.FluidsHeaderOutUL,
            this.FluidsHeaderOverproduced,
            this.FluidsHeaderProduced,
            this.FluidsHeaderConsumed});
			this.FluidsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FluidsListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.FluidsListView.FullRowSelect = true;
			this.FluidsListView.GridLines = true;
			this.FluidsListView.HideSelection = false;
			this.FluidsListView.LabelWrap = false;
			this.FluidsListView.Location = new System.Drawing.Point(0, 0);
			this.FluidsListView.MultiSelect = false;
			this.FluidsListView.Name = "FluidsListView";
			this.FluidsListView.Size = new System.Drawing.Size(728, 367);
			this.FluidsListView.SmallImageList = this.IconList;
			this.FluidsListView.TabIndex = 18;
			this.FluidsListView.UseCompatibleStateImageBehavior = false;
			this.FluidsListView.View = System.Windows.Forms.View.Details;
			this.FluidsListView.VirtualMode = true;
			this.FluidsListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FluidsListView_ColumnClick);
			this.FluidsListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.FluidsListView_RetrieveVirtualItem);
			// 
			// FluidsHeaderName
			// 
			this.FluidsHeaderName.Text = "Fluid";
			this.FluidsHeaderName.Width = 150;
			// 
			// FluidsHeaderIn
			// 
			this.FluidsHeaderIn.Text = "In";
			this.FluidsHeaderIn.Width = 75;
			// 
			// FluidsHeaderInUL
			// 
			this.FluidsHeaderInUL.Text = "In (x link)";
			this.FluidsHeaderInUL.Width = 75;
			// 
			// FluidsHeaderOut
			// 
			this.FluidsHeaderOut.Text = "Out";
			this.FluidsHeaderOut.Width = 75;
			// 
			// FluidsHeaderOutUL
			// 
			this.FluidsHeaderOutUL.Text = "Out (x link)";
			this.FluidsHeaderOutUL.Width = 75;
			// 
			// FluidsHeaderOverproduced
			// 
			this.FluidsHeaderOverproduced.Text = "Overprod.";
			this.FluidsHeaderOverproduced.Width = 75;
			// 
			// FluidsHeaderProduced
			// 
			this.FluidsHeaderProduced.Text = "Produced";
			this.FluidsHeaderProduced.Width = 75;
			// 
			// FluidsHeaderConsumed
			// 
			this.FluidsHeaderConsumed.Text = "Consumed";
			this.FluidsHeaderConsumed.Width = 75;
			// 
			// ItemsFilterTextBox
			// 
			this.ItemsFilterTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsFilterTextBox.Location = new System.Drawing.Point(43, 2);
			this.ItemsFilterTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 7, 2);
			this.ItemsFilterTextBox.Name = "ItemsFilterTextBox";
			this.ItemsFilterTextBox.Size = new System.Drawing.Size(554, 20);
			this.ItemsFilterTextBox.TabIndex = 29;
			this.ItemsFilterTextBox.TextChanged += new System.EventHandler(this.ItemsFilterTextBox_TextChanged);
			// 
			// ItemsExportButton
			// 
			this.ItemsExportButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsExportButton.Location = new System.Drawing.Point(604, 0);
			this.ItemsExportButton.Margin = new System.Windows.Forms.Padding(0, 0, 7, 0);
			this.ItemsExportButton.Name = "ItemsExportButton";
			this.ItemsExportButton.Size = new System.Drawing.Size(133, 24);
			this.ItemsExportButton.TabIndex = 31;
			this.ItemsExportButton.Text = "Export CSV";
			this.ItemsExportButton.UseVisualStyleBackColor = true;
			this.ItemsExportButton.Click += new System.EventHandler(this.ItemsExportButton_Click);
			// 
			// ItemsFilterCBTable
			// 
			this.ItemsFilterCBTable.AutoSize = true;
			this.ItemsFilterCBTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ItemsFilterCBTable.ColumnCount = 7;
			this.ItemsTable.SetColumnSpan(this.ItemsFilterCBTable, 3);
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ItemsFilterCBTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 142F));
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterConsumptionCheckBox, 6, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterOutputOverproducedCheckBox, 4, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterOutputUnlinkedCheckBox, 3, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterOutputCheckBox, 2, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterInputUnlinkedCheckBox, 1, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterInputCheckBox, 0, 0);
			this.ItemsFilterCBTable.Controls.Add(this.ItemFilterProductionCheckBox, 5, 0);
			this.ItemsFilterCBTable.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemsFilterCBTable.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
			this.ItemsFilterCBTable.Location = new System.Drawing.Point(3, 27);
			this.ItemsFilterCBTable.Name = "ItemsFilterCBTable";
			this.ItemsFilterCBTable.RowCount = 1;
			this.ItemsFilterCBTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ItemsFilterCBTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
			this.ItemsFilterCBTable.Size = new System.Drawing.Size(738, 23);
			this.ItemsFilterCBTable.TabIndex = 32;
			// 
			// ItemFilterConsumptionCheckBox
			// 
			this.ItemFilterConsumptionCheckBox.AutoSize = true;
			this.ItemFilterConsumptionCheckBox.Checked = true;
			this.ItemFilterConsumptionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterConsumptionCheckBox.Location = new System.Drawing.Point(602, 3);
			this.ItemFilterConsumptionCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterConsumptionCheckBox.Name = "ItemFilterConsumptionCheckBox";
			this.ItemFilterConsumptionCheckBox.Size = new System.Drawing.Size(87, 17);
			this.ItemFilterConsumptionCheckBox.TabIndex = 6;
			this.ItemFilterConsumptionCheckBox.Text = "Consumption";
			this.ItemFilterConsumptionCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterConsumptionCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterOutputOverproducedCheckBox
			// 
			this.ItemFilterOutputOverproducedCheckBox.AutoSize = true;
			this.ItemFilterOutputOverproducedCheckBox.Checked = true;
			this.ItemFilterOutputOverproducedCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterOutputOverproducedCheckBox.Location = new System.Drawing.Point(368, 3);
			this.ItemFilterOutputOverproducedCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterOutputOverproducedCheckBox.Name = "ItemFilterOutputOverproducedCheckBox";
			this.ItemFilterOutputOverproducedCheckBox.Size = new System.Drawing.Size(133, 17);
			this.ItemFilterOutputOverproducedCheckBox.TabIndex = 5;
			this.ItemFilterOutputOverproducedCheckBox.Text = "Output (overproduced)";
			this.ItemFilterOutputOverproducedCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterOutputOverproducedCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterOutputUnlinkedCheckBox
			// 
			this.ItemFilterOutputUnlinkedCheckBox.AutoSize = true;
			this.ItemFilterOutputUnlinkedCheckBox.Checked = true;
			this.ItemFilterOutputUnlinkedCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterOutputUnlinkedCheckBox.Location = new System.Drawing.Point(249, 3);
			this.ItemFilterOutputUnlinkedCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterOutputUnlinkedCheckBox.Name = "ItemFilterOutputUnlinkedCheckBox";
			this.ItemFilterOutputUnlinkedCheckBox.Size = new System.Drawing.Size(107, 17);
			this.ItemFilterOutputUnlinkedCheckBox.TabIndex = 4;
			this.ItemFilterOutputUnlinkedCheckBox.Text = "Output (unlinked)";
			this.ItemFilterOutputUnlinkedCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterOutputUnlinkedCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterOutputCheckBox
			// 
			this.ItemFilterOutputCheckBox.AutoSize = true;
			this.ItemFilterOutputCheckBox.Checked = true;
			this.ItemFilterOutputCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterOutputCheckBox.Location = new System.Drawing.Point(179, 3);
			this.ItemFilterOutputCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterOutputCheckBox.Name = "ItemFilterOutputCheckBox";
			this.ItemFilterOutputCheckBox.Size = new System.Drawing.Size(58, 17);
			this.ItemFilterOutputCheckBox.TabIndex = 3;
			this.ItemFilterOutputCheckBox.Text = "Output";
			this.ItemFilterOutputCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterOutputCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterInputUnlinkedCheckBox
			// 
			this.ItemFilterInputUnlinkedCheckBox.AutoSize = true;
			this.ItemFilterInputUnlinkedCheckBox.Checked = true;
			this.ItemFilterInputUnlinkedCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterInputUnlinkedCheckBox.Location = new System.Drawing.Point(68, 3);
			this.ItemFilterInputUnlinkedCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterInputUnlinkedCheckBox.Name = "ItemFilterInputUnlinkedCheckBox";
			this.ItemFilterInputUnlinkedCheckBox.Size = new System.Drawing.Size(99, 17);
			this.ItemFilterInputUnlinkedCheckBox.TabIndex = 2;
			this.ItemFilterInputUnlinkedCheckBox.Text = "Input (unlinked)";
			this.ItemFilterInputUnlinkedCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterInputUnlinkedCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterInputCheckBox
			// 
			this.ItemFilterInputCheckBox.AutoSize = true;
			this.ItemFilterInputCheckBox.Checked = true;
			this.ItemFilterInputCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterInputCheckBox.Location = new System.Drawing.Point(6, 3);
			this.ItemFilterInputCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterInputCheckBox.Name = "ItemFilterInputCheckBox";
			this.ItemFilterInputCheckBox.Size = new System.Drawing.Size(50, 17);
			this.ItemFilterInputCheckBox.TabIndex = 1;
			this.ItemFilterInputCheckBox.Text = "Input";
			this.ItemFilterInputCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterInputCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// ItemFilterProductionCheckBox
			// 
			this.ItemFilterProductionCheckBox.AutoSize = true;
			this.ItemFilterProductionCheckBox.Checked = true;
			this.ItemFilterProductionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ItemFilterProductionCheckBox.Location = new System.Drawing.Point(513, 3);
			this.ItemFilterProductionCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ItemFilterProductionCheckBox.Name = "ItemFilterProductionCheckBox";
			this.ItemFilterProductionCheckBox.Size = new System.Drawing.Size(77, 17);
			this.ItemFilterProductionCheckBox.TabIndex = 0;
			this.ItemFilterProductionCheckBox.Text = "Production";
			this.ItemFilterProductionCheckBox.UseVisualStyleBackColor = true;
			this.ItemFilterProductionCheckBox.CheckedChanged += new System.EventHandler(this.ItemFilterCheckBox_CheckedChanged);
			// 
			// KeyNodesTabPage
			// 
			this.KeyNodesTabPage.Controls.Add(this.KeyNodesTable);
			this.KeyNodesTabPage.Location = new System.Drawing.Point(4, 29);
			this.KeyNodesTabPage.Name = "KeyNodesTabPage";
			this.KeyNodesTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.KeyNodesTabPage.Size = new System.Drawing.Size(750, 462);
			this.KeyNodesTabPage.TabIndex = 2;
			this.KeyNodesTabPage.Text = "Key Nodes";
			this.KeyNodesTabPage.UseVisualStyleBackColor = true;
			// 
			// KeyNodesTable
			// 
			this.KeyNodesTable.AutoSize = true;
			this.KeyNodesTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.KeyNodesTable.ColumnCount = 3;
			this.KeyNodesTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.KeyNodesTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.KeyNodesTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
			this.KeyNodesTable.Controls.Add(this.KeyNodesListView, 0, 2);
			this.KeyNodesTable.Controls.Add(this.label2, 0, 0);
			this.KeyNodesTable.Controls.Add(this.KeyNodesFilterTextBox, 1, 0);
			this.KeyNodesTable.Controls.Add(this.keyNodesExportButton, 2, 0);
			this.KeyNodesTable.Controls.Add(this.tableLayoutPanel1, 0, 1);
			this.KeyNodesTable.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KeyNodesTable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.KeyNodesTable.Location = new System.Drawing.Point(3, 3);
			this.KeyNodesTable.Name = "KeyNodesTable";
			this.KeyNodesTable.RowCount = 3;
			this.KeyNodesTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.KeyNodesTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.KeyNodesTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.KeyNodesTable.Size = new System.Drawing.Size(744, 456);
			this.KeyNodesTable.TabIndex = 3;
			// 
			// KeyNodesListView
			// 
			this.KeyNodesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.KeyNodesHeaderType,
            this.KeyNodesHeaderDetails,
            this.KeyNodesHeaderTitle,
            this.KeyNodesHeaderFlow,
            this.KeyNodesHeaderBuildings});
			this.KeyNodesTable.SetColumnSpan(this.KeyNodesListView, 3);
			this.KeyNodesListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KeyNodesListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.KeyNodesListView.FullRowSelect = true;
			this.KeyNodesListView.GridLines = true;
			this.KeyNodesListView.HideSelection = false;
			this.KeyNodesListView.LabelWrap = false;
			this.KeyNodesListView.Location = new System.Drawing.Point(7, 60);
			this.KeyNodesListView.Margin = new System.Windows.Forms.Padding(7);
			this.KeyNodesListView.MultiSelect = false;
			this.KeyNodesListView.Name = "KeyNodesListView";
			this.KeyNodesListView.Size = new System.Drawing.Size(730, 389);
			this.KeyNodesListView.SmallImageList = this.IconList;
			this.KeyNodesListView.TabIndex = 32;
			this.KeyNodesListView.UseCompatibleStateImageBehavior = false;
			this.KeyNodesListView.View = System.Windows.Forms.View.Details;
			this.KeyNodesListView.VirtualMode = true;
			this.KeyNodesListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.KeyNodesListView_ColumnClick);
			this.KeyNodesListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.KeyNodesListView_RetrieveVirtualItem);
			// 
			// KeyNodesHeaderType
			// 
			this.KeyNodesHeaderType.Text = "Node Type";
			this.KeyNodesHeaderType.Width = 100;
			// 
			// KeyNodesHeaderDetails
			// 
			this.KeyNodesHeaderDetails.Text = "Node Details";
			this.KeyNodesHeaderDetails.Width = 200;
			// 
			// KeyNodesHeaderTitle
			// 
			this.KeyNodesHeaderTitle.Text = "Node Title";
			this.KeyNodesHeaderTitle.Width = 200;
			// 
			// KeyNodesHeaderFlow
			// 
			this.KeyNodesHeaderFlow.Text = "Throughput";
			this.KeyNodesHeaderFlow.Width = 80;
			// 
			// KeyNodesHeaderBuildings
			// 
			this.KeyNodesHeaderBuildings.Text = "Factories";
			this.KeyNodesHeaderBuildings.Width = 80;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label2.Location = new System.Drawing.Point(7, 0);
			this.label2.Margin = new System.Windows.Forms.Padding(7, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(32, 24);
			this.label2.TabIndex = 30;
			this.label2.Text = "Filter:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// KeyNodesFilterTextBox
			// 
			this.KeyNodesFilterTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KeyNodesFilterTextBox.Location = new System.Drawing.Point(43, 2);
			this.KeyNodesFilterTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 7, 2);
			this.KeyNodesFilterTextBox.Name = "KeyNodesFilterTextBox";
			this.KeyNodesFilterTextBox.Size = new System.Drawing.Size(554, 20);
			this.KeyNodesFilterTextBox.TabIndex = 29;
			this.KeyNodesFilterTextBox.TextChanged += new System.EventHandler(this.KeyNodesFilterTextBox_TextChanged);
			// 
			// keyNodesExportButton
			// 
			this.keyNodesExportButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.keyNodesExportButton.Location = new System.Drawing.Point(604, 0);
			this.keyNodesExportButton.Margin = new System.Windows.Forms.Padding(0, 0, 7, 0);
			this.keyNodesExportButton.Name = "keyNodesExportButton";
			this.keyNodesExportButton.Size = new System.Drawing.Size(133, 24);
			this.keyNodesExportButton.TabIndex = 31;
			this.keyNodesExportButton.Text = "Export CSV";
			this.keyNodesExportButton.UseVisualStyleBackColor = true;
			this.keyNodesExportButton.Click += new System.EventHandler(this.KeyNodesExportButton_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel1.ColumnCount = 4;
			this.KeyNodesTable.SetColumnSpan(this.tableLayoutPanel1, 3);
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.PassthroughNodeFilterCheckBox, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.ConsumerNodeFilterCheckBox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.SupplierNodeFilterCheckBox, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.RecipeNodeFilterCheckBox, 3, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 27);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(738, 23);
			this.tableLayoutPanel1.TabIndex = 33;
			// 
			// PassthroughNodeFilterCheckBox
			// 
			this.PassthroughNodeFilterCheckBox.AutoSize = true;
			this.PassthroughNodeFilterCheckBox.Checked = true;
			this.PassthroughNodeFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.PassthroughNodeFilterCheckBox.Location = new System.Drawing.Point(235, 3);
			this.PassthroughNodeFilterCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.PassthroughNodeFilterCheckBox.Name = "PassthroughNodeFilterCheckBox";
			this.PassthroughNodeFilterCheckBox.Size = new System.Drawing.Size(119, 17);
			this.PassthroughNodeFilterCheckBox.TabIndex = 3;
			this.PassthroughNodeFilterCheckBox.Text = "Passthrough Nodes";
			this.PassthroughNodeFilterCheckBox.UseVisualStyleBackColor = true;
			this.PassthroughNodeFilterCheckBox.CheckedChanged += new System.EventHandler(this.KeyNodesFilterCheckBox_CheckedChanged);
			// 
			// ConsumerNodeFilterCheckBox
			// 
			this.ConsumerNodeFilterCheckBox.AutoSize = true;
			this.ConsumerNodeFilterCheckBox.Checked = true;
			this.ConsumerNodeFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ConsumerNodeFilterCheckBox.Location = new System.Drawing.Point(116, 3);
			this.ConsumerNodeFilterCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ConsumerNodeFilterCheckBox.Name = "ConsumerNodeFilterCheckBox";
			this.ConsumerNodeFilterCheckBox.Size = new System.Drawing.Size(107, 17);
			this.ConsumerNodeFilterCheckBox.TabIndex = 2;
			this.ConsumerNodeFilterCheckBox.Text = "Consumer Nodes";
			this.ConsumerNodeFilterCheckBox.UseVisualStyleBackColor = true;
			this.ConsumerNodeFilterCheckBox.CheckedChanged += new System.EventHandler(this.KeyNodesFilterCheckBox_CheckedChanged);
			// 
			// SupplierNodeFilterCheckBox
			// 
			this.SupplierNodeFilterCheckBox.AutoSize = true;
			this.SupplierNodeFilterCheckBox.Checked = true;
			this.SupplierNodeFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.SupplierNodeFilterCheckBox.Location = new System.Drawing.Point(6, 3);
			this.SupplierNodeFilterCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.SupplierNodeFilterCheckBox.Name = "SupplierNodeFilterCheckBox";
			this.SupplierNodeFilterCheckBox.Size = new System.Drawing.Size(98, 17);
			this.SupplierNodeFilterCheckBox.TabIndex = 1;
			this.SupplierNodeFilterCheckBox.Text = "Supplier Nodes";
			this.SupplierNodeFilterCheckBox.UseVisualStyleBackColor = true;
			this.SupplierNodeFilterCheckBox.CheckedChanged += new System.EventHandler(this.KeyNodesFilterCheckBox_CheckedChanged);
			// 
			// RecipeNodeFilterCheckBox
			// 
			this.RecipeNodeFilterCheckBox.AutoSize = true;
			this.RecipeNodeFilterCheckBox.Checked = true;
			this.RecipeNodeFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.RecipeNodeFilterCheckBox.Location = new System.Drawing.Point(366, 3);
			this.RecipeNodeFilterCheckBox.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.RecipeNodeFilterCheckBox.Name = "RecipeNodeFilterCheckBox";
			this.RecipeNodeFilterCheckBox.Size = new System.Drawing.Size(94, 17);
			this.RecipeNodeFilterCheckBox.TabIndex = 0;
			this.RecipeNodeFilterCheckBox.Text = "Recipe Nodes";
			this.RecipeNodeFilterCheckBox.UseVisualStyleBackColor = true;
			this.RecipeNodeFilterCheckBox.CheckedChanged += new System.EventHandler(this.KeyNodesFilterCheckBox_CheckedChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.DisplayIndex = 0;
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 290;
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.AutoSize = true;
			this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel2.ColumnCount = 4;
			this.BuildingsTable.SetColumnSpan(this.tableLayoutPanel2, 2);
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.Controls.Add(this.BeaconCountLabel, 1, 0);
			this.tableLayoutPanel2.Controls.Add(this.BuildingCountLabel, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.PowerConsumptionLabel, 2, 0);
			this.tableLayoutPanel2.Controls.Add(this.PowerProductionLabel, 3, 0);
			this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
			this.tableLayoutPanel2.Location = new System.Drawing.Point(44, 3);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 1;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel2.Size = new System.Drawing.Size(697, 25);
			this.tableLayoutPanel2.TabIndex = 34;
			// 
			// BuildingCountLabel
			// 
			this.BuildingCountLabel.AutoSize = true;
			this.BuildingCountLabel.Location = new System.Drawing.Point(7, 5);
			this.BuildingCountLabel.Margin = new System.Windows.Forms.Padding(7, 5, 15, 5);
			this.BuildingCountLabel.Name = "BuildingCountLabel";
			this.BuildingCountLabel.Size = new System.Drawing.Size(71, 15);
			this.BuildingCountLabel.TabIndex = 33;
			this.BuildingCountLabel.Text = "#Buildings: ";
			// 
			// BeaconCountLabel
			// 
			this.BeaconCountLabel.AutoSize = true;
			this.BeaconCountLabel.Location = new System.Drawing.Point(100, 5);
			this.BeaconCountLabel.Margin = new System.Windows.Forms.Padding(7, 5, 15, 5);
			this.BeaconCountLabel.Name = "BeaconCountLabel";
			this.BeaconCountLabel.Size = new System.Drawing.Size(68, 15);
			this.BeaconCountLabel.TabIndex = 34;
			this.BeaconCountLabel.Text = "#Beacons: ";
			// 
			// BuildingsFilterTextBox
			// 
			this.BuildingsFilterTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BuildingsFilterTextBox.Location = new System.Drawing.Point(43, 33);
			this.BuildingsFilterTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 7, 2);
			this.BuildingsFilterTextBox.Name = "BuildingsFilterTextBox";
			this.BuildingsFilterTextBox.Size = new System.Drawing.Size(553, 20);
			this.BuildingsFilterTextBox.TabIndex = 35;
			this.BuildingsFilterTextBox.TextChanged += new System.EventHandler(this.BuildingsFilterTextBox_TextChanged);
			// 
			// GraphSummaryForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(764, 501);
			this.Controls.Add(this.MainTabControl);
			this.DoubleBuffered = true;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(780, 540);
			this.Name = "GraphSummaryForm";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Factory summary:";
			this.MainTabControl.ResumeLayout(false);
			this.BuildingsTabPage.ResumeLayout(false);
			this.BuildingsTabPage.PerformLayout();
			this.BuildingsTable.ResumeLayout(false);
			this.BuildingsTable.PerformLayout();
			this.BuildingsTabControl.ResumeLayout(false);
			this.AssemblersPage.ResumeLayout(false);
			this.MinersPage.ResumeLayout(false);
			this.PowersPage.ResumeLayout(false);
			this.BeaconsPage.ResumeLayout(false);
			this.ItemsTabPage.ResumeLayout(false);
			this.ItemsTabPage.PerformLayout();
			this.ItemsTable.ResumeLayout(false);
			this.ItemsTable.PerformLayout();
			this.ItemsTabControl.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.tabPage5.ResumeLayout(false);
			this.ItemsFilterCBTable.ResumeLayout(false);
			this.ItemsFilterCBTable.PerformLayout();
			this.KeyNodesTabPage.ResumeLayout(false);
			this.KeyNodesTabPage.PerformLayout();
			this.KeyNodesTable.ResumeLayout(false);
			this.KeyNodesTable.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage BuildingsTabPage;
		private System.Windows.Forms.TabPage ItemsTabPage;
		private System.Windows.Forms.TabPage KeyNodesTabPage;
		private System.Windows.Forms.TableLayoutPanel BuildingsTable;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TabControl BuildingsTabControl;
		private System.Windows.Forms.TabPage AssemblersPage;
		private System.Windows.Forms.ListView AssemblerListView;
		private System.Windows.Forms.ColumnHeader AssemblersHeaderName;
		private System.Windows.Forms.TabPage MinersPage;
		private System.Windows.Forms.ListView MinerListView;
		private System.Windows.Forms.ColumnHeader MinerHeaderName;
		private System.Windows.Forms.TabPage PowersPage;
		private System.Windows.Forms.ListView PowerListView;
		private System.Windows.Forms.ColumnHeader PowerHeaderName;
		private System.Windows.Forms.TabPage BeaconsPage;
		private System.Windows.Forms.ListView BeaconListView;
		private System.Windows.Forms.ColumnHeader BeaconsHeaderName;
		private System.Windows.Forms.Label PowerConsumptionLabel;
		private System.Windows.Forms.Label PowerProductionLabel;
		private System.Windows.Forms.TableLayoutPanel ItemsTable;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TabControl ItemsTabControl;
		private System.Windows.Forms.TabPage tabPage5;
		private System.Windows.Forms.TextBox ItemsFilterTextBox;
		private System.Windows.Forms.Button BuildingsExportButton;
		private System.Windows.Forms.Button ItemsExportButton;
		private System.Windows.Forms.TableLayoutPanel KeyNodesTable;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox KeyNodesFilterTextBox;
		private System.Windows.Forms.Button keyNodesExportButton;
		private System.Windows.Forms.ListView KeyNodesListView;
		private System.Windows.Forms.ColumnHeader KeyNodesHeaderType;
		private System.Windows.Forms.ImageList IconList;
		private System.Windows.Forms.TableLayoutPanel ItemsFilterCBTable;
		private System.Windows.Forms.CheckBox ItemFilterOutputOverproducedCheckBox;
		private System.Windows.Forms.CheckBox ItemFilterOutputUnlinkedCheckBox;
		private System.Windows.Forms.CheckBox ItemFilterOutputCheckBox;
		private System.Windows.Forms.CheckBox ItemFilterInputUnlinkedCheckBox;
		private System.Windows.Forms.CheckBox ItemFilterInputCheckBox;
		private System.Windows.Forms.CheckBox ItemFilterProductionCheckBox;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.ListView ItemsListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.CheckBox ItemFilterConsumptionCheckBox;
		private System.Windows.Forms.ColumnHeader AssemblersHeaderPower;
		private System.Windows.Forms.ColumnHeader AssemblersHeaderPowerB;
		private System.Windows.Forms.ColumnHeader MinersHeaderPower;
		private System.Windows.Forms.ColumnHeader MinersHeaderPowerB;
		private System.Windows.Forms.ColumnHeader PowerHeaderPower;
		private System.Windows.Forms.ColumnHeader PowerHeaderPowerB;
		private System.Windows.Forms.ColumnHeader BeaconsHeaderPower;
		private System.Windows.Forms.ColumnHeader AssemblersHeaderCounter;
		private System.Windows.Forms.ColumnHeader MinersHeaderCounter;
		private System.Windows.Forms.ColumnHeader PowerHeaderCounter;
		private System.Windows.Forms.ColumnHeader BeaconsHeaderCounter;
		private System.Windows.Forms.ColumnHeader ItemsHeaderName;
		private System.Windows.Forms.ColumnHeader ItemsHeaderIn;
		private System.Windows.Forms.ColumnHeader ItemsHeaderInUL;
		private System.Windows.Forms.ColumnHeader ItemsHeaderOut;
		private System.Windows.Forms.ColumnHeader ItemsHeaderOutUL;
		private System.Windows.Forms.ColumnHeader ItemsHeaderOverproduced;
		private System.Windows.Forms.ColumnHeader ItemsHeaderProduced;
		private System.Windows.Forms.ColumnHeader ItemsHeaderConsumed;
		private System.Windows.Forms.ListView FluidsListView;
		private System.Windows.Forms.ColumnHeader FluidsHeaderName;
		private System.Windows.Forms.ColumnHeader FluidsHeaderIn;
		private System.Windows.Forms.ColumnHeader FluidsHeaderInUL;
		private System.Windows.Forms.ColumnHeader FluidsHeaderOut;
		private System.Windows.Forms.ColumnHeader FluidsHeaderOutUL;
		private System.Windows.Forms.ColumnHeader FluidsHeaderOverproduced;
		private System.Windows.Forms.ColumnHeader FluidsHeaderProduced;
		private System.Windows.Forms.ColumnHeader FluidsHeaderConsumed;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.CheckBox PassthroughNodeFilterCheckBox;
		private System.Windows.Forms.CheckBox ConsumerNodeFilterCheckBox;
		private System.Windows.Forms.CheckBox SupplierNodeFilterCheckBox;
		private System.Windows.Forms.CheckBox RecipeNodeFilterCheckBox;
		private System.Windows.Forms.ColumnHeader KeyNodesHeaderTitle;
		private System.Windows.Forms.ColumnHeader KeyNodesHeaderFlow;
		private System.Windows.Forms.ColumnHeader KeyNodesHeaderDetails;
		private System.Windows.Forms.ColumnHeader KeyNodesHeaderBuildings;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.Label BeaconCountLabel;
		private System.Windows.Forms.Label BuildingCountLabel;
		private System.Windows.Forms.TextBox BuildingsFilterTextBox;
	}
}