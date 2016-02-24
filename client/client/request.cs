using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace client
{
    public partial class request : Form
    {
        int sec = 10;
        public request()
        {
            InitializeComponent();
        }

        public request(string[] s)
        {
            InitializeComponent();
            label2.Text = "Voice Chat request From :\r\n";
            label2.Text += "Ip : " + s[1] + "\r\nNo : " + s[2] + "\r\nName : " + s[3];
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sec > 0)
            {
                sec--;
                label1.Text = sec + " seconds to reply";
            }
            else
            {
                this.Close();
            }
        }
    }
}
