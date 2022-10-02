namespace WallRooms
{
    partial class StartWindow
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.rbSelElems = new System.Windows.Forms.RadioButton();
            this.rbOnView = new System.Windows.Forms.RadioButton();
            this.rbAll = new System.Windows.Forms.RadioButton();
            this.cbCheckClash = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(366, 86);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Запуск";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(447, 86);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Отмена";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // rbSelElems
            // 
            this.rbSelElems.AutoSize = true;
            this.rbSelElems.Location = new System.Drawing.Point(13, 13);
            this.rbSelElems.Name = "rbSelElems";
            this.rbSelElems.Size = new System.Drawing.Size(257, 17);
            this.rbSelElems.TabIndex = 2;
            this.rbSelElems.TabStop = true;
            this.rbSelElems.Text = "Выбранные элементы (стены, полы, потолки)";
            this.rbSelElems.UseVisualStyleBackColor = true;
            // 
            // rbOnView
            // 
            this.rbOnView.AutoSize = true;
            this.rbOnView.Location = new System.Drawing.Point(13, 37);
            this.rbOnView.Name = "rbOnView";
            this.rbOnView.Size = new System.Drawing.Size(151, 17);
            this.rbOnView.TabIndex = 3;
            this.rbOnView.TabStop = true;
            this.rbOnView.Text = "Стены на активном виде";
            this.rbOnView.UseVisualStyleBackColor = true;
            // 
            // rbAll
            // 
            this.rbAll.AutoSize = true;
            this.rbAll.Location = new System.Drawing.Point(13, 61);
            this.rbAll.Name = "rbAll";
            this.rbAll.Size = new System.Drawing.Size(131, 17);
            this.rbAll.TabIndex = 4;
            this.rbAll.TabStop = true;
            this.rbAll.Text = "Все стены в проекте";
            this.rbAll.UseVisualStyleBackColor = true;
            // 
            // cbCheckClash
            // 
            this.cbCheckClash.AutoSize = true;
            this.cbCheckClash.Location = new System.Drawing.Point(12, 89);
            this.cbCheckClash.Name = "cbCheckClash";
            this.cbCheckClash.Size = new System.Drawing.Size(285, 17);
            this.cbCheckClash.TabIndex = 5;
            this.cbCheckClash.Text = "Проверить на дублирование выбранные элементы";
            this.cbCheckClash.UseVisualStyleBackColor = true;
            // 
            // StartWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 121);
            this.Controls.Add(this.cbCheckClash);
            this.Controls.Add(this.rbAll);
            this.Controls.Add(this.rbOnView);
            this.Controls.Add(this.rbSelElems);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "StartWindow";
            this.Text = "Запись помещений для стен";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        public System.Windows.Forms.RadioButton rbOnView;
        public System.Windows.Forms.RadioButton rbAll;
        public System.Windows.Forms.RadioButton rbSelElems;
        public System.Windows.Forms.CheckBox cbCheckClash;
    }
}