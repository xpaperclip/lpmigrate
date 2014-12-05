partial class MainForm
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
        this.txtLink = new System.Windows.Forms.TextBox();
        this.btnGoBrackets = new System.Windows.Forms.Button();
        this.label1 = new System.Windows.Forms.Label();
        this.btnFetch = new System.Windows.Forms.Button();
        this.txtWikicode = new System.Windows.Forms.TextBox();
        this.panel1 = new System.Windows.Forms.Panel();
        this.panel2 = new System.Windows.Forms.Panel();
        this.btnGoGroups = new System.Windows.Forms.Button();
        this.panel1.SuspendLayout();
        this.panel2.SuspendLayout();
        this.SuspendLayout();
        // 
        // txtLink
        // 
        this.txtLink.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.txtLink.Location = new System.Drawing.Point(6, 27);
        this.txtLink.Margin = new System.Windows.Forms.Padding(2);
        this.txtLink.Name = "txtLink";
        this.txtLink.Size = new System.Drawing.Size(564, 23);
        this.txtLink.TabIndex = 2;
        // 
        // btnGoBrackets
        // 
        this.btnGoBrackets.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.btnGoBrackets.Location = new System.Drawing.Point(492, 6);
        this.btnGoBrackets.Name = "btnGoBrackets";
        this.btnGoBrackets.Size = new System.Drawing.Size(75, 23);
        this.btnGoBrackets.TabIndex = 3;
        this.btnGoBrackets.Text = "Bracket";
        this.btnGoBrackets.UseVisualStyleBackColor = true;
        this.btnGoBrackets.Click += new System.EventHandler(this.btnGoBrackets_Click);
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(3, 7);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(278, 15);
        this.label1.TabIndex = 4;
        this.label1.Text = "Enter or drag in a Liquipedia link to try and migrate.";
        // 
        // btnFetch
        // 
        this.btnFetch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnFetch.Location = new System.Drawing.Point(573, 27);
        this.btnFetch.Name = "btnFetch";
        this.btnFetch.Size = new System.Drawing.Size(75, 23);
        this.btnFetch.TabIndex = 5;
        this.btnFetch.Text = "Fetch";
        this.btnFetch.UseVisualStyleBackColor = true;
        this.btnFetch.Click += new System.EventHandler(this.btnFetch_Click);
        // 
        // txtWikicode
        // 
        this.txtWikicode.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtWikicode.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.txtWikicode.Location = new System.Drawing.Point(0, 61);
        this.txtWikicode.Multiline = true;
        this.txtWikicode.Name = "txtWikicode";
        this.txtWikicode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.txtWikicode.Size = new System.Drawing.Size(657, 320);
        this.txtWikicode.TabIndex = 6;
        // 
        // panel1
        // 
        this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
        this.panel1.Controls.Add(this.label1);
        this.panel1.Controls.Add(this.txtLink);
        this.panel1.Controls.Add(this.btnFetch);
        this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
        this.panel1.Location = new System.Drawing.Point(0, 0);
        this.panel1.Name = "panel1";
        this.panel1.Size = new System.Drawing.Size(657, 61);
        this.panel1.TabIndex = 7;
        // 
        // panel2
        // 
        this.panel2.Controls.Add(this.btnGoGroups);
        this.panel2.Controls.Add(this.btnGoBrackets);
        this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panel2.Location = new System.Drawing.Point(0, 381);
        this.panel2.Name = "panel2";
        this.panel2.Size = new System.Drawing.Size(657, 35);
        this.panel2.TabIndex = 8;
        // 
        // btnGoGroups
        // 
        this.btnGoGroups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.btnGoGroups.Location = new System.Drawing.Point(573, 6);
        this.btnGoGroups.Name = "btnGoGroups";
        this.btnGoGroups.Size = new System.Drawing.Size(75, 23);
        this.btnGoGroups.TabIndex = 4;
        this.btnGoGroups.Text = "Groups";
        this.btnGoGroups.UseVisualStyleBackColor = true;
        this.btnGoGroups.Click += new System.EventHandler(this.btnGoGroups_Click);
        // 
        // MainForm
        // 
        this.AcceptButton = this.btnFetch;
        this.AllowDrop = true;
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = System.Drawing.SystemColors.Control;
        this.ClientSize = new System.Drawing.Size(657, 416);
        this.Controls.Add(this.txtWikicode);
        this.Controls.Add(this.panel2);
        this.Controls.Add(this.panel1);
        this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.Name = "MainForm";
        this.Text = "Migrate Brackets and Groups";
        this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
        this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
        this.panel1.ResumeLayout(false);
        this.panel1.PerformLayout();
        this.panel2.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox txtLink;
    private System.Windows.Forms.Button btnGoBrackets;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button btnFetch;
    private System.Windows.Forms.TextBox txtWikicode;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Panel panel2;
    private System.Windows.Forms.Button btnGoGroups;
}
