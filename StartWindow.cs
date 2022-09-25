using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallRooms
{
    public partial class StartWindow : Form
    {

        public bool OkStart;
        public StartWindow()
        {
            InitializeComponent();
            //кнопка Пуск
            OkStart = false;
        }

        private void buttonОк_Click(object sender, EventArgs e)
        {
            OkStart = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            OkStart = false;
            this.Close();
        }
    }
}
