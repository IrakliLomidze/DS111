using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ILG.DS.Dialogs
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            ultraPictureBox1.Top = 0;
            ultraPictureBox1.Left = 0;
            this.ClientSize = new Size(ultraPictureBox1.Size.Width,ultraPictureBox1.Size.Height);
        }
    }
}