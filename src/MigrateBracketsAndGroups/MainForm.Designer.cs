﻿partial class MainForm
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
        this.label1 = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // label1
        // 
        this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.label1.Location = new System.Drawing.Point(0, 0);
        this.label1.Name = "label1";
        this.label1.Padding = new System.Windows.Forms.Padding(20);
        this.label1.Size = new System.Drawing.Size(356, 245);
        this.label1.TabIndex = 0;
        this.label1.Text = "Drag in a Liquipedia link to try and migrate.";
        this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        // 
        // MainForm
        // 
        this.AllowDrop = true;
        this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.White;
        this.ClientSize = new System.Drawing.Size(356, 245);
        this.Controls.Add(this.label1);
        this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
        this.Name = "MainForm";
        this.Text = "Migrate Brackets";
        this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
        this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label label1;
}
