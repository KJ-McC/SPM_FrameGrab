using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PLCData
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }



        public void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        public void textBox5_TextChanged(object sender, EventArgs e)
        {

        }
        private void label9_Click(object sender, EventArgs e)
        {

        }

        public void Form1_Load(object sender, EventArgs e)
        {
            string variableName = "FRAMEGRAB_OUTPUT";
            int eventID = 101;
            variableCompolet1.Active = true;
            variableCompolet1.WindowHandle = this.Handle;
            variableCompolet1.SetEvent(variableName, eventID);

        }

        public void variableCompolet1_Changed(object sender, EventArgs e)
        {
            int eventID;
            string variableName;
            try
            {
                while (true)
                {
                    this.variableCompolet1.ReciveEvent(out variableName, out eventID, 0);
                    if (variableName == null || variableName == "" || variableName == "0")
                    {
                        return;  //return no variable name
                    }
                    else
                    {
                        //Read and Display Inputs from PLC
                        textBox1.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[0]")).ToString();
                        textBox2.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[1]")).ToString();
                        textBox3.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[2]")).ToString();
                        //Read and Display Outputs to PLC (not setup)
                        textBox5.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_INPUT[0]")).ToString();
                        textBox6.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_INPUT[1]")).ToString();
                        textBox7.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_INPUT[2]")).ToString();

                        //need to create file if not existing - should be deleted after read in GATFramegrabber
                        try
                        {
                            //Pass the filepath and filename to the StreamWriter Constructor
                            StreamWriter sw = new StreamWriter("C:\\Users\\kmcclintock\\Desktop\\FrameGrab_Out0.txt");
                            //Write a line of text
                            sw.WriteLine(variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[0]"));
                            //Close the file
                            sw.Close();
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                        }
                    }
                }
            }

            catch(Exception)
            {
                return;
            }
        }
    }
}
