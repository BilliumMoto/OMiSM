namespace OMtoSMConverter
{
    partial class Form2
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
            this.hsFrom = new System.Windows.Forms.ListBox();
            this.hsTo = new System.Windows.Forms.ListBox();
            this.doButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // hsFrom
            // 
            this.hsFrom.AllowDrop = true;
            this.hsFrom.FormattingEnabled = true;
            this.hsFrom.Location = new System.Drawing.Point(13, 13);
            this.hsFrom.Name = "hsFrom";
            this.hsFrom.Size = new System.Drawing.Size(580, 95);
            this.hsFrom.TabIndex = 0;
            this.hsFrom.DragDrop += new System.Windows.Forms.DragEventHandler(this.hsDragDrop);
            this.hsFrom.DragEnter += new System.Windows.Forms.DragEventHandler(this.fileDragEnter);
            // 
            // hsTo
            // 
            this.hsTo.AllowDrop = true;
            this.hsTo.FormattingEnabled = true;
            this.hsTo.Location = new System.Drawing.Point(13, 115);
            this.hsTo.Name = "hsTo";
            this.hsTo.Size = new System.Drawing.Size(580, 108);
            this.hsTo.TabIndex = 1;
            this.hsTo.DragDrop += new System.Windows.Forms.DragEventHandler(this.hsDragDrop);
            this.hsTo.DragEnter += new System.Windows.Forms.DragEventHandler(this.fileDragEnter);
            // 
            // doButton
            // 
            this.doButton.Location = new System.Drawing.Point(13, 230);
            this.doButton.Name = "doButton";
            this.doButton.Size = new System.Drawing.Size(580, 95);
            this.doButton.TabIndex = 2;
            this.doButton.Text = "Do";
            this.doButton.UseVisualStyleBackColor = true;
            this.doButton.Click += new System.EventHandler(this.doHSCopy);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(605, 337);
            this.Controls.Add(this.doButton);
            this.Controls.Add(this.hsTo);
            this.Controls.Add(this.hsFrom);
            this.Name = "Form2";
            this.Text = "Form2";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.hsClose);
            this.Load += new System.EventHandler(this.Form2_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.hsKeyPress);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox hsFrom;
        private System.Windows.Forms.ListBox hsTo;
        private System.Windows.Forms.Button doButton;
    }
}