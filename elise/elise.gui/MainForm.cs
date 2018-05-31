using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Elise.GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void openSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.splitContainer2.Panel2Collapsed = !this.splitContainer2.Panel2Collapsed;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripLabel3_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Source.Items.Add("WLC");
            FromBook.Items.Add("Genesis");
            FromChapter.Items.Add("1");
            FromVerse.Items.Add("1");
            ToBook.Items.Add("Revelation");
            ToChapter.Items.Add("22");
            ToVerse.Items.Add("21");
        }
    }
}
