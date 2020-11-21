namespace AIMS3.FrontEnd.Basic
{
    partial class DummyForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DummyForm));
            this.axARMClass = new AxARM.AxARMClass();
            ((System.ComponentModel.ISupportInitialize)(this.axARMClass)).BeginInit();
            this.SuspendLayout();
            // 
            // axARMClass
            // 
            this.axARMClass.Location = new System.Drawing.Point(360, 152);
            this.axARMClass.Name = "axARMClass";
            this.axARMClass.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axARMClass.OcxState")));
            this.axARMClass.Size = new System.Drawing.Size(255, 139);
            this.axARMClass.TabIndex = 0;
            // 
            // Dummy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.axARMClass);
            this.Name = "Dummy";
            this.Text = "Dummy";
            ((System.ComponentModel.ISupportInitialize)(this.axARMClass)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public AxARM.AxARMClass axARMClass;
    }
}