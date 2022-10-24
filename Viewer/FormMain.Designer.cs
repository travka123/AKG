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
            this.cdMain = new System.Windows.Forms.ColorDialog();
            this.btnAmbient = new System.Windows.Forms.Button();
            this.btnDiffuse = new System.Windows.Forms.Button();
            this.btnSpecular = new System.Windows.Forms.Button();
            this.lVertices = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbMeshes
            // 
            this.cbMeshes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMeshes.FormattingEnabled = true;
            this.cbMeshes.Location = new System.Drawing.Point(746, 65);
            this.cbMeshes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbMeshes.Name = "cbMeshes";
            this.cbMeshes.Size = new System.Drawing.Size(204, 33);
            this.cbMeshes.TabIndex = 0;
            this.cbMeshes.TabStop = false;
            this.cbMeshes.SelectedIndexChanged += new System.EventHandler(this.cbMeshes_SelectedIndexChanged);
            this.cbMeshes.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbMeshes_KeyDown);
            // 
            // btnShow
            // 
            this.btnShow.Location = new System.Drawing.Point(776, 35);
            this.btnShow.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnShow.Name = "btnShow";
            this.btnShow.Size = new System.Drawing.Size(174, 37);
            this.btnShow.TabIndex = 0;
            this.btnShow.TabStop = false;
            this.btnShow.Text = "SHOW";
            this.btnShow.UseVisualStyleBackColor = true;
            this.btnShow.Click += new System.EventHandler(this.button1_Click);
            this.btnShow.KeyDown += new System.Windows.Forms.KeyEventHandler(this.emptyBtnKeyDown);
            // 
            // cbModels
            // 
            this.cbModels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbModels.FormattingEnabled = true;
            this.cbModels.Location = new System.Drawing.Point(746, 20);
            this.cbModels.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbModels.Name = "cbModels";
            this.cbModels.Size = new System.Drawing.Size(204, 33);
            this.cbModels.TabIndex = 0;
            this.cbModels.TabStop = false;
            this.cbModels.SelectedIndexChanged += new System.EventHandler(this.cbModels_SelectedIndexChanged);
            this.cbModels.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbModels_KeyDown);
            // 
            // cbSelectedMesh
            // 
            this.cbSelectedMesh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSelectedMesh.FormattingEnabled = true;
            this.cbSelectedMesh.Location = new System.Drawing.Point(746, 110);
            this.cbSelectedMesh.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbSelectedMesh.Name = "cbSelectedMesh";
            this.cbSelectedMesh.Size = new System.Drawing.Size(204, 33);
            this.cbSelectedMesh.TabIndex = 0;
            this.cbSelectedMesh.TabStop = false;
            this.cbSelectedMesh.SelectedIndexChanged += new System.EventHandler(this.cbSelectedMesh_SelectedIndexChanged);
            this.cbSelectedMesh.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbSelectedMesh_KeyDown);
            // 
            // btnAmbient
            // 
            this.btnAmbient.Location = new System.Drawing.Point(841, 158);
            this.btnAmbient.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnAmbient.Name = "btnAmbient";
            this.btnAmbient.Size = new System.Drawing.Size(107, 38);
            this.btnAmbient.TabIndex = 0;
            this.btnAmbient.TabStop = false;
            this.btnAmbient.Text = "ambient";
            this.btnAmbient.UseVisualStyleBackColor = true;
            this.btnAmbient.Click += new System.EventHandler(this.btnAmbient_Click);
            this.btnAmbient.KeyDown += new System.Windows.Forms.KeyEventHandler(this.emptyBtnKeyDown);
            // 
            // btnDiffuse
            // 
            this.btnDiffuse.Location = new System.Drawing.Point(843, 207);
            this.btnDiffuse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnDiffuse.Name = "btnDiffuse";
            this.btnDiffuse.Size = new System.Drawing.Size(107, 38);
            this.btnDiffuse.TabIndex = 0;
            this.btnDiffuse.TabStop = false;
            this.btnDiffuse.Text = "diffuse";
            this.btnDiffuse.UseVisualStyleBackColor = true;
            this.btnDiffuse.Click += new System.EventHandler(this.btnDiffuse_Click);
            this.btnDiffuse.KeyDown += new System.Windows.Forms.KeyEventHandler(this.emptyBtnKeyDown);
            // 
            // btnSpecular
            // 
            this.btnSpecular.Location = new System.Drawing.Point(841, 255);
            this.btnSpecular.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSpecular.Name = "btnSpecular";
            this.btnSpecular.Size = new System.Drawing.Size(107, 38);
            this.btnSpecular.TabIndex = 0;
            this.btnSpecular.TabStop = false;
            this.btnSpecular.Text = "specular";
            this.btnSpecular.UseVisualStyleBackColor = true;
            this.btnSpecular.Click += new System.EventHandler(this.btnSpecular_Click);
            this.btnSpecular.KeyDown += new System.Windows.Forms.KeyEventHandler(this.emptyBtnKeyDown);
            // 
            // lVertices
            // 
            this.lVertices.AutoSize = true;
            this.lVertices.BackColor = System.Drawing.Color.MistyRose;
            this.lVertices.Location = new System.Drawing.Point(17, 900);
            this.lVertices.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lVertices.Name = "lVertices";
            this.lVertices.Size = new System.Drawing.Size(72, 25);
            this.lVertices.TabIndex = 2;
            this.lVertices.Text = "Vertices";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(971, 940);
            this.Controls.Add(this.lVertices);
            this.Controls.Add(this.btnSpecular);
            this.Controls.Add(this.btnDiffuse);
            this.Controls.Add(this.btnAmbient);
            this.Controls.Add(this.cbSelectedMesh);
            this.Controls.Add(this.cbModels);
            this.Controls.Add(this.cbMeshes);
            this.Controls.Add(this.btnShow);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "FormMain";
            this.Text = "Hello world";
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.FormMain_PreviewKeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ComboBox cbMeshes;
        private Button btnShow;
        private ComboBox cbModels;
        private ComboBox cbSelectedMesh;
        private ColorDialog cdMain;
        private Button btnAmbient;
        private Button btnDiffuse;
        private Button btnSpecular;
        private Label lVertices;
    }
}