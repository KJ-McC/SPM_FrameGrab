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
using System.Threading;

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
            //string camSN;
            try
            {
                while (true)
                {
                    //Removed camera serial number for simplifying interface
                    //Always display Current Serial Number
                    //StreamReader sr = new StreamReader("C:\\Gentex Corporation\\GAT\\CurrentCameraSN.txt");
                    //camSN = sr.ReadToEnd();
                    //sr.Close();
                    //textBox3.Text = camSN;

                    this.variableCompolet1.ReciveEvent(out variableName, out eventID, 0);
                    if (variableName == null || variableName == "" || variableName == "0")
                    {
                        return;  //return no variable name
                    }
                    else
                    {
                        //Read and Display Inputs from PLC

                        //WL SPOILER TRIGGER
                        textBox1.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[0]")).ToString();
                        //WS BUCKET TIGGER
                        textBox2.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[1]")).ToString();
                        //RESET PC SIGNAL
                        textBox3.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[2]")).ToString();

                        //Read and Display Outputs to PLC (not setup)
                        //textBox5.Text = (variableCompolet1.ReadVariable("FRAMEGRAB_INPUT[0]")).ToString();

                        //need to create file if not existing - Deleted after read in GATFramegrabber

                        if (textBox1.Text == ("1"))
                        {

                            //Pass the filepath and filename to the StreamWriter Constructor
                            StreamWriter sw = new StreamWriter("C:\\Gentex Corporation\\GAT\\Images\\FrameGrab_Out0.txt");
                            //Write a line of text
                            sw.WriteLine(variableCompolet1.ReadVariable("FRAMEGRAB_OUTPUT[0]"));
                            //Close the file
                            sw.Close();
                        }
                        else
                        {

                            if (textBox2.Text == "1")
                            {
                                // Running FrameRecorder App
                                System.Diagnostics.Process.Start("C:\\FrameRecorder\\App\\build\\exe.win-amd64-3.8\\FrameRecorder.exe");


                            }
                            else
                            {

                                if (textBox3.Text == "1")
                                {

                                    //Reset PC
                                    System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    }
                    Thread.Sleep(250); //Small delay to slow looping
                }
            }

            catch (Exception)
            {
                return;
            }
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
