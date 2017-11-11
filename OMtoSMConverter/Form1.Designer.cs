namespace OMtoSMConverter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.outBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // outBox
            // 
            this.outBox.AllowDrop = true;
            this.outBox.FormattingEnabled = true;
            this.outBox.Location = new System.Drawing.Point(12, 12);
            this.outBox.Name = "outBox";
            this.outBox.Size = new System.Drawing.Size(800, 576);
            this.outBox.TabIndex = 0;
            this.outBox.SelectedIndexChanged += new System.EventHandler(this.outBox_SelectedIndexChanged);
            this.outBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.fileBeg_dragDrop);
            this.outBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.fileBeg_dragEnter);
            this.outBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.WinKeyPress);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 601);
            this.Controls.Add(this.outBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "BilliumMoto\'s OM to SM Converter";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.WinKeyPress);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox outBox;
    }
}

