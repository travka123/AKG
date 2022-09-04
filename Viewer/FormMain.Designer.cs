namespace Viewer
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbMeshes = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // cbMeshes
            // 
            this.cbMeshes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMeshes.FormattingEnabled = true;
            this.cbMeshes.Location = new System.Drawing.Point(491, 12);
            this.cbMeshes.Name = "cbMeshes";
            this.cbMeshes.Size = new System.Drawing.Size(121, 23);
            this.cbMeshes.TabIndex = 0;
            this.cbMeshes.TabStop = false;
            this.cbMeshes.SelectedIndexChanged += new System.EventHandler(this.cbMeshes_SelectedIndexChanged);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 601);
            this.Controls.Add(this.cbMeshes);
            this.KeyPreview = true;
            this.Name = "FormMain";
            this.Text = "Hello world";
            this.ResumeLayout(false);

        }

        #endregion

        private ComboBox cbMeshes;
    }
}