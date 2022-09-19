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
            this.btnShow = new System.Windows.Forms.Button();
            this.cbModels = new System.Windows.Forms.ComboBox();
            this.cbSelectedMesh = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // cbMeshes
            // 
            this.cbMeshes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMeshes.FormattingEnabled = true;
            this.cbMeshes.Location = new System.Drawing.Point(543, 39);
            this.cbMeshes.Name = "cbMeshes";
            this.cbMeshes.Size = new System.Drawing.Size(123, 23);
            this.cbMeshes.TabIndex = 0;
            this.cbMeshes.TabStop = false;
            this.cbMeshes.SelectedIndexChanged += new System.EventHandler(this.cbMeshes_SelectedIndexChanged);
            this.cbMeshes.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbMeshes_KeyDown);
            // 
            // btnShow
            // 
            this.btnShow.Location = new System.Drawing.Point(543, 21);
            this.btnShow.Name = "btnShow";
            this.btnShow.Size = new System.Drawing.Size(122, 22);
            this.btnShow.TabIndex = 0;
            this.btnShow.TabStop = false;
            this.btnShow.Text = "SHOW";
            this.btnShow.UseVisualStyleBackColor = true;
            this.btnShow.Click += new System.EventHandler(this.button1_Click);
            this.btnShow.KeyDown += new System.Windows.Forms.KeyEventHandler(this.btnShow_KeyDown);
            // 
            // cbModels
            // 
            this.cbModels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbModels.FormattingEnabled = true;
            this.cbModels.Location = new System.Drawing.Point(543, 12);
            this.cbModels.Name = "cbModels";
            this.cbModels.Size = new System.Drawing.Size(123, 23);
            this.cbModels.TabIndex = 0;
            this.cbModels.TabStop = false;
            this.cbModels.SelectedIndexChanged += new System.EventHandler(this.cbModels_SelectedIndexChanged);
            this.cbModels.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbModels_KeyDown);
            // 
            // cbSelectedMesh
            // 
            this.cbSelectedMesh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSelectedMesh.FormattingEnabled = true;
            this.cbSelectedMesh.Location = new System.Drawing.Point(543, 66);
            this.cbSelectedMesh.Name = "cbSelectedMesh";
            this.cbSelectedMesh.Size = new System.Drawing.Size(123, 23);
            this.cbSelectedMesh.TabIndex = 0;
            this.cbSelectedMesh.TabStop = false;
            this.cbSelectedMesh.SelectedIndexChanged += new System.EventHandler(this.cbSelectedMesh_SelectedIndexChanged);
            this.cbSelectedMesh.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbSelectedMesh_KeyDown);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(676, 500);
            this.Controls.Add(this.cbSelectedMesh);
            this.Controls.Add(this.cbModels);
            this.Controls.Add(this.cbMeshes);
            this.Controls.Add(this.btnShow);
            this.KeyPreview = true;
            this.Name = "FormMain";
            this.Text = "Hello world";
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.FormMain_PreviewKeyDown);
            this.ResumeLayout(false);

        }

        #endregion
        private ComboBox cbMeshes;
        private Button btnShow;
        private ComboBox cbModels;
        private ComboBox cbSelectedMesh;
    }
}